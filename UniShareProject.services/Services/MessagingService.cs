using UniShareProject.Repository.Data;
using UniShareProject.Repository.Models;
using UniShareProject.Repository.Repositories;

namespace UniShareProject.services.Services;

public class MessagingService : IMessagingService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public MessagingService(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IUnitOfWorkFactory unitOfWorkFactory)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<Conversation?> GetConversationByIdAsync(int id)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        return await _conversationRepository.GetByIdAsync(id, unitOfWork);
    }

    public async Task<IEnumerable<Conversation>> GetUserConversationsAsync(int userId)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        return await _conversationRepository.GetByUserIdAsync(userId, unitOfWork);
    }

    public async Task<Conversation> CreateConversationAsync(int itemId, int buyerId, int sellerId)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        var conversation = new Conversation
        {
            ItemId = itemId,
            BuyerId = buyerId,
            SellerId = sellerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };
        try
        {
            conversation.Id = await _conversationRepository.CreateAsync(conversation, unitOfWork);
            await unitOfWork.CommitAsync();
            return conversation;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<Message>> GetConversationMessagesAsync(int conversationId)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        return await _messageRepository.GetByConversationIdAsync(conversationId, unitOfWork);
    }

    public async Task<Message> SendMessageAsync(int conversationId, int senderId, string content)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };
        try
        {
            message.Id = await _messageRepository.CreateAsync(message, unitOfWork);
            await unitOfWork.CommitAsync();
            return message;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> MarkMessageAsReadAsync(int messageId)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        try
        {
            var message = await _messageRepository.GetByIdAsync(messageId, unitOfWork);
            if (message == null)
                return false;
            message.IsRead = true;
            var success = await _messageRepository.UpdateAsync(message, unitOfWork);
            await unitOfWork.CommitAsync();
            return success;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }
}