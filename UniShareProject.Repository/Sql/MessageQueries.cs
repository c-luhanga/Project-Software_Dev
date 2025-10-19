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
    /// Inserts a new message with SYSUTCDATETIME() and returns the generated ID
    /// </summary>
    public const string InsertWithSysDate = @"
        INSERT INTO dbo.Messages (ConversationID, SenderID, Content, Timestamp)
        VALUES (@ConversationID, @SenderID, @Content, SYSUTCDATETIME());
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

    /// <summary>
    /// Gets count of messages for a conversation
    /// </summary>
    public const string CountByConversation = @"
        SELECT COUNT(*)
        FROM dbo.Messages
        WHERE ConversationID = @ConversationId";

    /// <summary>
    /// Gets paged messages for a conversation with read status
    /// </summary>
    public const string GetByConversationPaged = @"
        SELECT 
            m.MessageID,
            m.ConversationID,
            m.SenderID,
            m.Content,
            m.Timestamp,
            CASE WHEN mr.MessageID IS NOT NULL THEN 1 ELSE 0 END as IsRead
        FROM dbo.Messages m
        LEFT JOIN dbo.MessageReads mr ON m.MessageID = mr.MessageID
        WHERE m.ConversationID = @ConversationId
        ORDER BY m.Timestamp ASC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

    /// <summary>
    /// Gets count and paged messages for a conversation in a single query using QueryMultiple
    /// </summary>
    public const string CountAndPageByConversation = @"
        -- Count query
        SELECT COUNT(*)
        FROM dbo.Messages
        WHERE ConversationID = @ConversationId;

        -- Paged results query
        SELECT 
            m.MessageID,
            m.ConversationID,
            m.SenderID,
            m.Content,
            m.Timestamp,
            CASE WHEN mr.MessageID IS NOT NULL THEN 1 ELSE 0 END as IsRead
        FROM dbo.Messages m
        LEFT JOIN dbo.MessageReads mr ON m.MessageID = mr.MessageID
        WHERE m.ConversationID = @ConversationId
        ORDER BY m.Timestamp ASC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

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
    /// Updates conversation's last message and timestamp with SYSUTCDATETIME()
    /// </summary>
    public const string UpdateConversationLastMessageWithSysDate = @"
        UPDATE dbo.Conversations 
        SET LastMessage = @Content, LastUpdated = SYSUTCDATETIME()
        WHERE ConversationID = @ConversationID";

    /// <summary>
    /// Deletes a message
    /// </summary>
    public const string Delete = @"
        DELETE FROM dbo.Messages WHERE MessageID = @Id";

    #endregion
}