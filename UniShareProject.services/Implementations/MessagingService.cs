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
    private readonly UniShareProject.Repository.Abstractions.IConversationRepository _conversationRepository;
    private readonly UniShareProject.Repository.Abstractions.IMessageRepository _messageRepository;
    private readonly UniShareProject.Repository.Repositories.IMessageRepository _legacyMessageRepository;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<MessagingService> _logger;
    private readonly IRealTimeNotificationService? _realTimeNotificationService;

    public MessagingService(
        UniShareProject.Repository.Abstractions.IConversationRepository conversationRepository,
        UniShareProject.Repository.Abstractions.IMessageRepository messageRepository,
        UniShareProject.Repository.Repositories.IMessageRepository legacyMessageRepository,
        IUnitOfWorkFactory unitOfWorkFactory,
        IMapper mapper,
        ILogger<MessagingService> logger,
        IRealTimeNotificationService? realTimeNotificationService = null)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _legacyMessageRepository = legacyMessageRepository;
        _unitOfWorkFactory = unitOfWorkFactory;
        _mapper = mapper;
        _logger = logger;
        _realTimeNotificationService = realTimeNotificationService;
    }

    public async Task<int> StartConversationAsync(StartConversationRequest req, int starterUserId, CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        try
        {
            // Use the new abstractions interface to ensure conversation exists
            var conversationId = await _conversationRepository.EnsureConversationAsync(req.ItemId, starterUserId, req.OtherUserId, ct);
            
            await unitOfWork.CommitAsync();
            
            _logger.LogInformation("Started/found conversation {ConversationId} between users {StarterUserId} and {OtherUserId} for item {ItemId}", 
                conversationId, starterUserId, req.OtherUserId, req.ItemId);
            
            return conversationId;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<MessageDto> SendAsync(SendMessageRequest req, int senderId, CancellationToken ct)
    {
        await using var unitOfWork = _unitOfWorkFactory.Create();
        
        try
        {
            // Check if user is participant in the conversation
            var isParticipant = await _conversationRepository.UserIsParticipantAsync(req.ConversationId, senderId, ct);
            if (!isParticipant)
            {
                throw new UnauthorizedAccessException("User is not a participant in this conversation.");
            }

            // Create message object
            var message = new Message
            {
                ConversationID = req.ConversationId,
                SenderID = senderId,
                Content = req.Content,
                Timestamp = DateTime.UtcNow
            };

            // Insert message using the new abstractions interface
            var messageId = await _messageRepository.InsertAsync(message, ct);
            
            await unitOfWork.CommitAsync();
            
            // Re-read the inserted message to get all fields populated using legacy repository
            await using var readUnitOfWork = _unitOfWorkFactory.Create();
            var insertedMessage = await _legacyMessageRepository.GetByIdAsync(messageId, readUnitOfWork);
            
            if (insertedMessage == null)
            {
                throw new InvalidOperationException($"Failed to retrieve inserted message with ID {messageId}");
            }
            
            _logger.LogInformation("User {SenderId} sent message {MessageId} in conversation {ConversationId}", 
                senderId, messageId, req.ConversationId);
            
            var messageDto = _mapper.Map<MessageDto>(insertedMessage);
            
            // Send real-time notification if available
            if (_realTimeNotificationService != null)
            {
                try
                {
                    await _realTimeNotificationService.NotifyNewMessageAsync(req.ConversationId, messageDto, ct);
                    _logger.LogDebug("Real-time notification sent for message {MessageId}", messageId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send real-time notification for message {MessageId}", messageId);
                    // Don't fail the message send if notification fails
                }
            }
            
            return messageDto;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<PagedResult<MessageDto>> GetConversationAsync(int conversationId, PageSpec page, int userId, CancellationToken ct)
    {
        // Check if user is participant in the conversation
        var isParticipant = await _conversationRepository.UserIsParticipantAsync(conversationId, userId, ct);
        if (!isParticipant)
        {
            throw new UnauthorizedAccessException("User is not a participant in this conversation.");
        }

        // Fetch paged messages from repository
        var pagedMessages = await _messageRepository.GetByConversationAsync(conversationId, page, ct);
        
        // Map to PagedResult<MessageDto>
        var messageDtos = _mapper.Map<IEnumerable<MessageDto>>(pagedMessages.Items);
        
        return new PagedResult<MessageDto>
        {
            Items = messageDtos,
            Total = pagedMessages.Total,
            Page = pagedMessages.Page,
            PageSize = pagedMessages.PageSize
        };
    }

    public async Task<PagedResult<ConversationListItem>> ListForUserAsync(int userId, PageSpec page, CancellationToken ct)
    {
        // Call repository to get paged conversation list
        var pagedConversations = await _conversationRepository.ListForUserAsync(userId, page, ct);
        
        // Map to PagedResult<ConversationListItem>
        var conversationListItems = _mapper.Map<IEnumerable<ConversationListItem>>(pagedConversations.Items);
        
        return new PagedResult<ConversationListItem>
        {
            Items = conversationListItems,
            Total = pagedConversations.Total,
            Page = pagedConversations.Page,
            PageSize = pagedConversations.PageSize
        };
    }
}