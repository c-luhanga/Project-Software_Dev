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