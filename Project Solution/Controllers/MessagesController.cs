using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;
using UniShare.Data.Dtos;

namespace UniShare.Controllers
{
    [ApiController]
    [Route("api/v1/conversations/{conversationId}/messages")]
    public class MessagesController : ControllerBase
    {
        private readonly UniShareDbContext _db;
        public MessagesController(UniShareDbContext db)
        {
            _db = db;
        }

        // GET /api/v1/conversations/{id}/messages?cursor=&limit=
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMessages(int conversationId, [FromQuery] long? cursor, [FromQuery] int? limit)
        {
            // TODO: Get current userId from auth and check participant
            var currentUserId = 0;
            var isParticipant = await _db.ConversationParticipants.AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == currentUserId);
            if (!isParticipant) return Forbid();

            var query = _db.Messages.Where(m => m.ConversationId == conversationId);
            if (cursor.HasValue)
                query = query.Where(m => m.Timestamp.Ticks < cursor);
            query = query.OrderByDescending(m => m.Timestamp);
            int take = limit ?? 20;
            var messages = await query.Take(take + 1).ToListAsync();
            var result = messages.Take(take).ToList();
            long? nextCursor = messages.Count > take ? result.Last().Timestamp.Ticks : null;
            return Ok(new { items = result, nextCursor });
        }

        // POST /api/v1/conversations/{id}/messages
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PostMessage(int conversationId, [FromBody] MessageCreateDto dto)
        {
            // TODO: Get current userId from auth and check participant
            var currentUserId = 0;
            var isParticipant = await _db.ConversationParticipants.AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == currentUserId);
            if (!isParticipant) return Forbid();

            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = currentUserId,
                Content = dto.Content,
                Timestamp = DateTime.UtcNow
            };
            _db.Messages.Add(message);

            // Update conversation
            var convo = await _db.Conversations.FindAsync(conversationId);
            if (convo != null)
            {
                convo.LastMessage = dto.Content;
                convo.LastUpdated = message.Timestamp;
            }
            await _db.SaveChangesAsync();
            return Ok(message);
        }
    }
}
