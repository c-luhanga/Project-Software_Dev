namespace UniShareProject.Repository.Sql;

/// <summary>
/// Contains all SQL queries for Item repository operations
/// </summary>
public static class ItemQueries
{
    #region Insert Operations
    
    /// <summary>
    /// Inserts a new item and returns the generated ID
    /// </summary>
    public const string Insert = @"
        INSERT INTO dbo.Items (Title, Description, CategoryID, Price, ConditionID, StatusID, SellerID, PostedDate)
        VALUES (@Title, @Description, @CategoryID, @Price, @ConditionID, @StatusID, @SellerID, SYSUTCDATETIME());
        SELECT CAST(SCOPE_IDENTITY() as int)";

    /// <summary>
    /// Legacy insert query that uses provided PostedDate
    /// </summary>
    public const string InsertLegacy = @"
        INSERT INTO dbo.Items (Title, Description, CategoryID, Price, ConditionID, StatusID, SellerID, PostedDate)
        VALUES (@Title, @Description, @CategoryID, @Price, @ConditionID, @StatusID, @SellerID, @PostedDate);
        SELECT CAST(SCOPE_IDENTITY() as int)";

    #endregion

    #region Select Operations

    /// <summary>
    /// Selects an item by its ID
    /// </summary>
    public const string GetById = @"
        SELECT * FROM dbo.Items WHERE ItemID = @Id";

    /// <summary>
    /// Gets seller ID and status for an item by ID
    /// </summary>
    public const string GetSellerAndStatus = @"
        SELECT SellerID, StatusID FROM dbo.Items WHERE ItemID = @Id";

    /// <summary>
    /// Selects all available items (StatusID = 1)
    /// </summary>
    public const string GetAllAvailable = @"
        SELECT * FROM dbo.Items WHERE StatusID = 1";

    /// <summary>
    /// Selects items by seller ID
    /// </summary>
    public const string GetByUserId = @"
        SELECT * FROM dbo.Items WHERE SellerID = @UserId";

    /// <summary>
    /// Search query with category join for legacy search
    /// </summary>
    public const string SearchLegacy = @"
        SELECT i.*, c.Name as Category 
        FROM dbo.Items i
        LEFT JOIN dbo.Categories c ON i.CategoryID = c.CategoryID
        WHERE i.StatusID = 1 AND (i.Title LIKE @Query OR i.Description LIKE @Query OR c.Name LIKE @Query)";

    #endregion

    #region Update Operations

    /// <summary>
    /// Updates the status of an item and returns affected row count
    /// </summary>
    public const string UpdateStatus = @"
        UPDATE dbo.Items 
        SET StatusID = @StatusId 
        WHERE ItemID = @Id;
        SELECT @@ROWCOUNT";

    /// <summary>
    /// Updates item fields
    /// </summary>
    public const string Update = @"
        UPDATE dbo.Items 
        SET Title = @Title, Description = @Description, CategoryID = @CategoryID, Price = @Price, 
            ConditionID = @ConditionID, StatusID = @StatusID
        WHERE ItemID = @ItemID";

    /// <summary>
    /// Soft delete by setting StatusID to 0
    /// </summary>
    public const string SoftDelete = @"
        UPDATE dbo.Items SET StatusID = 0 WHERE ItemID = @Id";

    #endregion

    #region Search with Pagination

    /// <summary>
    /// Template for paginated search queries. Use string.Format with WHERE clause.
    /// {0} will be replaced with the dynamic WHERE clause
    /// </summary>
    public const string SearchPaginated = @"
        -- Get total count
        SELECT COUNT(1) FROM dbo.Items i {0};
        
        -- Get paged results
        SELECT i.*, c.Name as Category 
        FROM dbo.Items i
        LEFT JOIN dbo.Categories c ON i.CategoryID = c.CategoryID
        {0}
        ORDER BY i.PostedDate DESC
        OFFSET @Offset ROWS 
        FETCH NEXT @PageSize ROWS ONLY";

    #endregion
}