using AutoMapper;
using Microsoft.Extensions.Logging;
using UniShareProject.Repository.Data.Interfaces;
using UniShareProject.Repository.Models;
using UniShareProject.Repository.Repositories;
using UniShareProject.services.DTOs;
using UniShareProject.services.Interfaces;

namespace UniShareProject.services.Implementations;

/// <summary>
/// Service for handling messaging operations
/// </summary>
public class MessagingService : IMessagingService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<MessagingService> _logger;

    public MessagingService(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IItemRepository itemRepository,
        IUserRepository userRepository,
        IUnitOfWorkFactory unitOfWorkFactory,
        IMapper mapper,
        ILogger<MessagingService> logger)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _itemRepository = itemRepository;
        _userRepository = userRepository;
        _unitOfWorkFactory = unitOfWorkFactory;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<int> StartConversationAsync(int currentUserId, StartConversationRequest request, CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        try
        {
            // Validate that the other user exists
            var otherUserExists = await _userRepository.UserExistsAsync(request.OtherUserId, ct);
            if (!otherUserExists)
            {
                throw new ArgumentException("The specified user does not exist.", nameof(request.OtherUserId));
            }

            // Validate that the current user is not trying to message themselves
            if (currentUserId == request.OtherUserId)
            {
                throw new ArgumentException("Cannot start a conversation with yourself.");
            }

            // If an item is specified, validate it exists and determine seller/buyer roles
            int? itemId = request.ItemId;
            int sellerId, buyerId;

            if (itemId.HasValue)
            {
                var itemInfo = await _itemRepository.GetSellerAndStatusAsync(itemId.Value, ct);
                if (itemInfo == null)
                {
                    throw new ArgumentException("The specified item does not exist.", nameof(request.ItemId));
                }

                sellerId = itemInfo.Value.SellerId;
                
                // Determine buyer based on who's not the seller
                if (currentUserId == sellerId)
                {
                    buyerId = request.OtherUserId;
                }
                else if (request.OtherUserId == sellerId)
                {
                    buyerId = currentUserId;
                }
                else
                {
                    throw new ArgumentException("Neither user is the seller of this item.");
                }

                // Check if conversation already exists for this item between these users
                var existingConversationId = await _conversationRepository.FindExistingConversationAsync(
                    itemId.Value, sellerId, buyerId, unitOfWork);
                
                if (existingConversationId.HasValue)
                {
                    return existingConversationId.Value;
                }
            }
            else
            {
                // For general conversations, assign arbitrarily
                sellerId = currentUserId;
                buyerId = request.OtherUserId;
            }

            // Create new conversation
            var conversation = new Conversation
            {
                ItemID = itemId,
                BuyerId = buyerId,
                SellerId = sellerId,
                LastMessage = null,
                LastUpdated = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            var conversationId = await _conversationRepository.CreateAsync(conversation, unitOfWork);
            await unitOfWork.CommitAsync();

            _logger.LogInformation("Created conversation {ConversationId} between users {UserId1} and {UserId2}", 
                conversationId, currentUserId, request.OtherUserId);

            return conversationId;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<MessageDto> SendMessageAsync(int currentUserId, SendMessageRequest request, CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        try
        {
            // Verify user is a participant in the conversation
            var isParticipant = await _conversationRepository.IsUserParticipantAsync(
                request.ConversationId, currentUserId, unitOfWork);
            
            if (!isParticipant)
            {
                throw new UnauthorizedAccessException("You are not a participant in this conversation.");
            }

            // Create and send the message
            var message = new Message
            {
                ConversationID = request.ConversationId,
                SenderID = currentUserId,
                Content = request.Content,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            var messageId = await _messageRepository.CreateAsync(message, unitOfWork);
            message.MessageID = messageId;
            
            await unitOfWork.CommitAsync();

            _logger.LogInformation("User {UserId} sent message {MessageId} in conversation {ConversationId}", 
                currentUserId, messageId, request.ConversationId);

            return _mapper.Map<MessageDto>(message);
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<ConversationListItem>> GetUserConversationsAsync(int userId, CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        var conversationData = await _conversationRepository.GetConversationListByUserIdAsync(userId, unitOfWork);
        return _mapper.Map<IEnumerable<ConversationListItem>>(conversationData);
    }

    public async Task<IEnumerable<MessageDto>> GetConversationMessagesAsync(int conversationId, int userId, CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        // Verify user is a participant in the conversation
        var isParticipant = await _conversationRepository.IsUserParticipantAsync(conversationId, userId, unitOfWork);
        if (!isParticipant)
        {
            throw new UnauthorizedAccessException("You are not a participant in this conversation.");
        }

        var messages = await _messageRepository.GetByConversationIdAsync(conversationId, unitOfWork);
        return _mapper.Map<IEnumerable<MessageDto>>(messages);
    }

    public async Task<bool> MarkConversationAsReadAsync(int conversationId, int userId, CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        try
        {
            // Verify user is a participant in the conversation
            var isParticipant = await _conversationRepository.IsUserParticipantAsync(conversationId, userId, unitOfWork);
            if (!isParticipant)
            {
                return false;
            }

            // Get all unread messages in the conversation that were not sent by this user
            var messages = await _messageRepository.GetByConversationIdAsync(conversationId, unitOfWork);
            var unreadMessages = messages.Where(m => m.SenderID != userId && !m.IsRead).ToList();

            // Mark each message as read
            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                await _messageRepository.UpdateAsync(message, unitOfWork);
            }

            await unitOfWork.CommitAsync();

            _logger.LogInformation("User {UserId} marked {Count} messages as read in conversation {ConversationId}", 
                userId, unreadMessages.Count, conversationId);

            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }
}