using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using UniShare.Data;
using UniShare.Data.Dtos;

namespace UniShare.Controllers
{
    [ApiController]
    [Route("api/v1/items")]
    public class ItemsController : ControllerBase
    {
        private readonly UniShareDbContext _db;
        private const long MaxImageSize = 5 * 1024 * 1024; // 5MB
        public ItemsController(UniShareDbContext db)
        {
            _db = db;
        }

        // GET /api/v1/items
        [HttpGet]
        public async Task<IActionResult> GetItems([
            FromQuery] string? q,
            [FromQuery] string? category,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string? condition,
            [FromQuery] int? sellerId,
            [FromQuery] bool? freeOnly,
            [FromQuery] string? sort,
            [FromQuery] long? cursor,
            [FromQuery] int? limit)
        {
            var query = _db.Items.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(i => i.Title.Contains(q) || i.Description.Contains(q));
            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(i => i.Category == category);
            if (minPrice.HasValue)
                query = query.Where(i => i.Price >= minPrice);
            if (maxPrice.HasValue)
                query = query.Where(i => i.Price <= maxPrice);
            if (!string.IsNullOrWhiteSpace(condition))
                query = query.Where(i => i.Condition == condition);
            if (sellerId.HasValue)
                query = query.Where(i => i.SellerId == sellerId.Value);
            if (freeOnly == true)
                query = query.Where(i => i.Price == 0);
            if (cursor.HasValue)
                query = query.Where(i => i.PostedDate.Ticks < cursor);

            // Sorting
            query = sort switch
            {
                "price_asc" => query.OrderBy(i => i.Price),
                "price_desc" => query.OrderByDescending(i => i.Price),
                _ => query.OrderByDescending(i => i.PostedDate)
            };

            int take = limit ?? 20;
            var items = await query.Take(take + 1).ToListAsync();
            var resultItems = items.Take(take).ToList();
            long? nextCursor = items.Count > take ? resultItems.Last().PostedDate.Ticks : null;

            return Ok(new { items = resultItems, nextCursor });
        }

        // GET /api/v1/items/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetItem(int id)
        {
            var item = await _db.Items.Include(i => i.Images).FirstOrDefaultAsync(i => i.Id == id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        // POST /api/v1/items
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateItem([FromBody] ItemCreateDto dto)
        {
            // TODO: Require authentication
            // var userId = ...;
            var item = new Item
            {
                Title = dto.Title,
                Description = dto.Description,
                Category = dto.Category,
                Price = dto.Price,
                Condition = dto.Condition,
                SellerId = 0, // TODO: Set to authenticated user
                PostedDate = DateTime.UtcNow
            };
            _db.Items.Add(item);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetItem), new { id = item.Id }, item);
        }

        // PUT /api/v1/items/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateItem(int id, [FromBody] ItemUpdateDto dto)
        {
            var item = await _db.Items.FindAsync(id);
            if (item == null) return NotFound();
            // TODO: Check owner/admin
            if (dto.Title != null) item.Title = dto.Title;
            if (dto.Description != null) item.Description = dto.Description;
            if (dto.Category != null) item.Category = dto.Category;
            if (dto.Price.HasValue) item.Price = dto.Price.Value;
            if (dto.Condition != null) item.Condition = dto.Condition;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/v1/items/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var item = await _db.Items.FindAsync(id);
            if (item == null) return NotFound();
            // TODO: Check owner/admin
            _db.Items.Remove(item);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // POST /api/v1/items/{id}/favorite
        [HttpPost("{id}/favorite")]
        [Authorize]
        public IActionResult AddFavorite(int id)
        {
            // TODO: Implement favorite logic (in-memory or persistent)
            return NoContent();
        }

        // DELETE /api/v1/items/{id}/favorite
        [HttpDelete("{id}/favorite")]
        [Authorize]
        public IActionResult RemoveFavorite(int id)
        {
            // TODO: Implement favorite logic (in-memory or persistent)
            return NoContent();
        }

        // POST /api/v1/items/{id}/images
        [HttpPost("{id}/images")]
        [Authorize]
        public async Task<IActionResult> UploadImage(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");
            if (file.Length > MaxImageSize)
                return BadRequest("File too large. Max 5MB allowed.");
            if (!file.ContentType.StartsWith("image/"))
                return BadRequest("Invalid file type. Only images are allowed.");

            var item = await _db.Items.FindAsync(id);
            if (item == null) return NotFound();
            // TODO: Check owner/admin

            // TODO: Replace with actual storage logic
            var url = $"https://example.com/images/{Guid.NewGuid()}";

            var image = new ItemImage
            {
                ItemId = id,
                ImageUrl = url
            };
            _db.ItemImages.Add(image);
            await _db.SaveChangesAsync();
            return Ok(new { url });
        }

        // DELETE /api/v1/items/{id}/images/{imageId}
        [HttpDelete("{id}/images/{imageId}")]
        [Authorize]
        public async Task<IActionResult> DeleteImage(int id, int imageId)
        {
            var image = await _db.ItemImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ItemId == id);
            if (image == null) return NotFound();
            // TODO: Check owner/admin
            _db.ItemImages.Remove(image);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
