using System.Threading;
using System.Threading.Tasks;
using UniShareProject.Repository.Models;
using UniShareProject.Repository.Repositories;

namespace UniShareProject.Repository.Abstractions;

public interface IConversationRepository
{
    /// <summary>
    /// Ensure a conversation exists between two users for an optional item. Returns the conversation ID.
    /// </summary>
    Task<int> EnsureConversationAsync(int? itemId, int userA, int userB, CancellationToken ct);

    /// <summary>
    /// Check if a user is a participant in a conversation.
    /// </summary>
    Task<bool> UserIsParticipantAsync(int conversationId, int userId, CancellationToken ct);

    /// <summary>
    /// List conversations for a user with paging.
    /// </summary>
    Task<PagedResult<ConversationListData>> ListForUserAsync(int userId, PageSpec page, CancellationToken ct);
}

public interface IMessageRepository
{
    /// <summary>
    /// Insert a new message and return its ID.
    /// </summary>
    Task<int> InsertAsync(Message msg, CancellationToken ct);

    /// <summary>
    /// Get paged messages for a conversation.
    /// </summary>
    Task<PagedResult<Message>> GetByConversationAsync(int conversationId, PageSpec page, CancellationToken ct);
}
