using FluentValidation;
using System.ComponentModel;

namespace UniShareProject.services.DTOs;

/// <summary>
/// Request to start a new conversation with a user
/// </summary>
public record StartConversationRequest(
    /// <summary>
    /// Optional item ID if the conversation is about a specific item
    /// </summary>
    [Description("Optional item ID if the conversation is about a specific item")]
    int? ItemId,
    
    /// <summary>
    /// ID of the user to start a conversation with
    /// </summary>
    [Description("ID of the user to start a conversation with")]
    int OtherUserId
);

/// <summary>
/// Request to send a message in an existing conversation
/// </summary>
public record SendMessageRequest(
    /// <summary>
    /// ID of the conversation to send the message to
    /// </summary>
    [Description("ID of the conversation to send the message to")]
    int ConversationId,
    
    /// <summary>
    /// Content of the message to send
    /// </summary>
    [Description("Content of the message to send")]
    string Content
);

/// <summary>
/// Message data transfer object
/// </summary>
public record MessageDto(
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    [Description("Unique identifier for the message")]
    int MessageId,
    
    /// <summary>
    /// ID of the conversation this message belongs to
    /// </summary>
    [Description("ID of the conversation this message belongs to")]
    int ConversationId,
    
    /// <summary>
    /// ID of the user who sent the message
    /// </summary>
    [Description("ID of the user who sent the message")]
    int SenderId,
    
    /// <summary>
    /// Content of the message
    /// </summary>
    [Description("Content of the message")]
    string Content,
    
    /// <summary>
    /// Timestamp when the message was sent
    /// </summary>
    [Description("Timestamp when the message was sent")]
    DateTime Timestamp
);

/// <summary>
/// Conversation list item for displaying in conversation overview
/// </summary>
public record ConversationListItem(
    /// <summary>
    /// Unique identifier for the conversation
    /// </summary>
    [Description("Unique identifier for the conversation")]
    int ConversationId,
    
    /// <summary>
    /// Optional item ID if the conversation is about a specific item
    /// </summary>
    [Description("Optional item ID if the conversation is about a specific item")]
    int? ItemId,
    
    /// <summary>
    /// Preview of the last message in the conversation
    /// </summary>
    [Description("Preview of the last message in the conversation")]
    string? LastMessage,
    
    /// <summary>
    /// Timestamp of the last activity in the conversation
    /// </summary>
    [Description("Timestamp of the last activity in the conversation")]
    DateTime LastUpdated,
    
    /// <summary>
    /// ID of the other user in the conversation
    /// </summary>
    [Description("ID of the other user in the conversation")]
    int OtherUserId,
    
    /// <summary>
    /// Name of the other user in the conversation
    /// </summary>
    [Description("Name of the other user in the conversation")]
    string OtherUserName,
    
    /// <summary>
    /// Number of unread messages in the conversation
    /// </summary>
    [Description("Number of unread messages in the conversation")]
    int UnreadCount
);

/// <summary>
/// Validator for SendMessageRequest
/// </summary>
public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Message content is required.")
            .Length(1, 4000)
            .WithMessage("Message content must be between 1 and 4000 characters.");

        RuleFor(x => x.ConversationId)
            .GreaterThan(0)
            .WithMessage("Conversation ID must be a positive integer.");
    }
}