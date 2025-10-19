namespace UniShareProject.Repository.Sql;

/// <summary>
/// Contains all SQL queries for Conversation repository operations
/// </summary>
public static class ConversationQueries
{
    #region Insert Operations
    
    /// <summary>
    /// Inserts a new conversation and returns the generated ID
    /// </summary>
    public const string Insert = @"
        INSERT INTO dbo.Conversations (ItemID, LastMessage, LastUpdated, CreatedAt)
        VALUES (@ItemID, @LastMessage, @LastUpdated, @CreatedAt);
        SELECT CAST(SCOPE_IDENTITY() as int)";

    /// <summary>
    /// Inserts a conversation participant
    /// </summary>
    public const string InsertParticipant = @"
        INSERT INTO dbo.ConversationParticipants (ConversationID, UserID)
        VALUES (@ConversationId, @UserId)";

    #endregion

    #region Select Operations

    /// <summary>
    /// Gets a conversation by ID with participant information
    /// </summary>
    public const string GetById = @"
        SELECT c.ConversationID, c.ItemID, c.LastMessage, c.LastUpdated, c.CreatedAt,
               cp1.UserID as BuyerId, cp2.UserID as SellerId
        FROM dbo.Conversations c
        INNER JOIN dbo.ConversationParticipants cp1 ON c.ConversationID = cp1.ConversationID
        INNER JOIN dbo.ConversationParticipants cp2 ON c.ConversationID = cp2.ConversationID
        WHERE c.ConversationID = @Id AND cp1.UserID != cp2.UserID";

    /// <summary>
    /// Gets conversations by user ID
    /// </summary>
    public const string GetByUserId = @"
        SELECT DISTINCT c.ConversationID, c.ItemID, c.LastMessage, c.LastUpdated, c.CreatedAt
        FROM dbo.Conversations c
        INNER JOIN dbo.ConversationParticipants cp ON c.ConversationID = cp.ConversationID
        WHERE cp.UserID = @UserId
        ORDER BY c.LastUpdated DESC";

    /// <summary>
    /// Gets conversations by user ID with other participant info and unread count
    /// </summary>
    public const string GetConversationListByUserId = @"
        SELECT 
            c.ConversationID,
            c.ItemID,
            c.LastMessage,
            c.LastUpdated,
            otherUser.UserID as OtherUserId,
            otherUser.FirstName + ' ' + otherUser.LastName as OtherUserName,
            ISNULL(unread.UnreadCount, 0) as UnreadCount
        FROM dbo.Conversations c
        INNER JOIN dbo.ConversationParticipants cp ON c.ConversationID = cp.ConversationID
        INNER JOIN dbo.ConversationParticipants otherCp ON c.ConversationID = otherCp.ConversationID
        INNER JOIN dbo.Users otherUser ON otherCp.UserID = otherUser.UserID
        LEFT JOIN (
            SELECT m.ConversationID, COUNT(*) as UnreadCount
            FROM dbo.Messages m
            LEFT JOIN dbo.MessageReads mr ON m.MessageID = mr.MessageID AND mr.UserID = @UserId
            WHERE mr.MessageID IS NULL AND m.SenderID != @UserId
            GROUP BY m.ConversationID
        ) unread ON c.ConversationID = unread.ConversationID
        WHERE cp.UserID = @UserId AND otherCp.UserID != @UserId
        ORDER BY c.LastUpdated DESC";

    /// <summary>
    /// Check if user is participant in conversation
    /// </summary>
    public const string CheckUserParticipation = @"
        SELECT COUNT(1)
        FROM dbo.ConversationParticipants
        WHERE ConversationID = @ConversationId AND UserID = @UserId";

    /// <summary>
    /// Check if conversation exists between users for an item
    /// </summary>
    public const string FindExistingConversation = @"
        SELECT TOP 1 c.ConversationID
        FROM dbo.Conversations c
        INNER JOIN dbo.ConversationParticipants cp1 ON c.ConversationID = cp1.ConversationID
        INNER JOIN dbo.ConversationParticipants cp2 ON c.ConversationID = cp2.ConversationID
        WHERE c.ItemID = @ItemId 
        AND cp1.UserID = @UserId1 
        AND cp2.UserID = @UserId2
        AND cp1.UserID != cp2.UserID";

    #endregion

    #region Update Operations

    /// <summary>
    /// Updates conversation information
    /// </summary>
    public const string Update = @"
        UPDATE dbo.Conversations 
        SET LastMessage = @LastMessage, LastUpdated = @LastUpdated
        WHERE ConversationID = @ConversationID";

    /// <summary>
    /// Deletes a conversation
    /// </summary>
    public const string Delete = @"
        DELETE FROM dbo.Conversations WHERE ConversationID = @Id";

    #endregion
}