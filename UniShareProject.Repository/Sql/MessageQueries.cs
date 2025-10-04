namespace UniShareProject.Repository.Sql;

/// <summary>
/// Contains all SQL queries for Message repository operations
/// </summary>
public static class MessageQueries
{
    #region Insert Operations
    
    /// <summary>
    /// Inserts a new message and returns the generated ID
    /// </summary>
    public const string Insert = @"
        INSERT INTO dbo.Messages (ConversationID, SenderID, Content, Timestamp)
        VALUES (@ConversationID, @SenderID, @Content, @Timestamp);
        SELECT CAST(SCOPE_IDENTITY() as int)";

    /// <summary>
    /// Inserts a message read record
    /// </summary>
    public const string InsertRead = @"
        INSERT INTO dbo.MessageReads (MessageID, UserID, ReadAt)
        VALUES (@MessageID, @UserID, @ReadAt)";

    #endregion

    #region Select Operations

    /// <summary>
    /// Gets a message by ID with read status
    /// </summary>
    public const string GetById = @"
        SELECT m.MessageID, m.ConversationID, m.SenderID, m.Content, m.Timestamp,
               CASE WHEN mr.MessageID IS NOT NULL THEN 1 ELSE 0 END as IsRead
        FROM dbo.Messages m
        LEFT JOIN dbo.MessageReads mr ON m.MessageID = mr.MessageID
        WHERE m.MessageID = @Id";

    /// <summary>
    /// Gets messages by conversation ID with read status
    /// </summary>
    public const string GetByConversationId = @"
        SELECT m.MessageID, m.ConversationID, m.SenderID, m.Content, m.Timestamp,
               CASE WHEN mr.MessageID IS NOT NULL THEN 1 ELSE 0 END as IsRead
        FROM dbo.Messages m
        LEFT JOIN dbo.MessageReads mr ON m.MessageID = mr.MessageID
        WHERE m.ConversationID = @ConversationId 
        ORDER BY m.Timestamp";

    #endregion

    #region Update Operations

    /// <summary>
    /// Updates message content
    /// </summary>
    public const string Update = @"
        UPDATE dbo.Messages 
        SET Content = @Content
        WHERE MessageID = @MessageID";

    /// <summary>
    /// Updates conversation's last message and timestamp
    /// </summary>
    public const string UpdateConversationLastMessage = @"
        UPDATE dbo.Conversations 
        SET LastMessage = @Content, LastUpdated = @Timestamp 
        WHERE ConversationID = @ConversationID";

    /// <summary>
    /// Deletes a message
    /// </summary>
    public const string Delete = @"
        DELETE FROM dbo.Messages WHERE MessageID = @Id";

    #endregion
}