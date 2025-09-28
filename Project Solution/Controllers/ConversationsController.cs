using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;

namespace UniShare.Controllers
{
    [ApiController]
    [Route("api/v1/conversations")]
    public class ConversationsController : ControllerBase
    {
        private readonly UniShareDbContext _db;
        public ConversationsController(UniShareDbContext db)
        {
            _db = db;
        }

        // GET /api/v1/conversations
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMyConversations()
        {
            // TODO: Get current userId from auth
            var currentUserId = 0;
            var conversations = await _db.ConversationParticipants
                .Where(cp => cp.UserId == currentUserId)
                .Select(cp => cp.Conversation)
                .Include(c => c.Participants)
                .ToListAsync();

            var result = conversations.Select(c =>
            {
                var other = c.Participants.FirstOrDefault(p => p.UserId != currentUserId)?.User;
                return new
                {
                    id = c.Id,
                    lastMessage = c.LastMessage,
                    lastUpdated = c.LastUpdated,
                    otherParticipant = other == null ? null : new
                    {
                        id = other.Id,
                        firstName = other.FirstName,
                        lastName = other.LastName
                    }
                };
            });
            return Ok(result);
        }

        public class ConversationCreateDto
        {
            public int ItemId { get; set; }
            public int ParticipantId { get; set; }
        }

        // POST /api/v1/conversations
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateOrFindConversation([FromBody] ConversationCreateDto dto)
        {
            // TODO: Get current userId from auth
            var currentUserId = 0;
            if (currentUserId == dto.ParticipantId)
                return BadRequest("Cannot create conversation with yourself.");

            // Find existing conversation for these participants
            var convo = await _db.Conversations
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c =>
                    c.Participants.Any(p => p.UserId == currentUserId) &&
                    c.Participants.Any(p => p.UserId == dto.ParticipantId)
                );
            if (convo == null)
            {
                convo = new Conversation
                {
                    LastMessage = null,
                    LastUpdated = DateTime.UtcNow,
                    Participants = new List<ConversationParticipant>
                    {
                        new ConversationParticipant { UserId = currentUserId },
                        new ConversationParticipant { UserId = dto.ParticipantId }
                    },
                    Messages = new List<Message>()
                };
                _db.Conversations.Add(convo);
                await _db.SaveChangesAsync();
            }
            return Ok(new { id = convo.Id });
        }
    }
}
