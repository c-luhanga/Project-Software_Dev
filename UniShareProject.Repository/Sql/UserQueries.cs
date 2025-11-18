namespace UniShareProject.Repository.Sql;

/// <summary>
/// Contains all SQL queries for User repository operations
/// </summary>
public static class UserQueries
{
    #region Insert Operations
    
    /// <summary>
    /// Inserts a new user and returns the generated ID
    /// </summary>
    public const string Insert = @"
        INSERT INTO dbo.Users 
        (FirebaseUID, FirstName, LastName, Email, PasswordHash, Phone, House, IsBanned, IsAdmin, IsDeleted, CreatedAt, LastSeen, ProfileImageURL)
        VALUES 
        (@FirebaseUID, @FirstName, @LastName, @Email, @PasswordHash, @Phone, @House, @IsBanned, @IsAdmin, @IsDeleted, @CreatedAt, @LastSeen, @ProfileImageURL);
        SELECT CAST(SCOPE_IDENTITY() as int)";

    #endregion

    #region Select Operations

    /// <summary>
    /// Checks if an email exists in the database
    /// </summary>
    public const string EmailExists = @"
        SELECT COUNT(1) FROM dbo.Users WHERE Email = @Email";

    /// <summary>
    /// Gets a user by email (active users only)
    /// </summary>
    public const string GetByEmail = @"
        SELECT * FROM dbo.Users WHERE Email = @Email AND IsDeleted = 0";

    /// <summary>
    /// Gets a user by ID (active users only)
    /// </summary>
    public const string GetById = @"
        SELECT * FROM dbo.Users WHERE UserID = @Id AND IsDeleted = 0";

    /// <summary>
    /// Gets a user by username/email (for backward compatibility)
    /// </summary>
    public const string GetByUsername = @"
        SELECT * FROM dbo.Users WHERE Email = @Username AND IsDeleted = 0";

    /// <summary>
    /// Gets all active users
    /// </summary>
    public const string GetAll = @"
        SELECT * FROM dbo.Users WHERE IsDeleted = 0";

    /// <summary>
    /// Checks if a user exists by ID (for admin operations)
    /// </summary>
    public const string UserExists = @"
        SELECT COUNT(1) FROM dbo.Users WHERE UserID = @Id AND IsDeleted = 0";

    /// <summary>
    /// Gets total count of users
    /// </summary>
    public const string GetTotalUsers = @"
        SELECT COUNT(1) FROM dbo.Users WHERE IsDeleted = 0";

    /// <summary>
    /// Gets count of banned users
    /// </summary>
    public const string GetBannedUsersCount = @"
        SELECT COUNT(1) FROM dbo.Users WHERE IsDeleted = 0 AND IsBanned = 1";

    /// <summary>
    /// Gets count of admin users
    /// </summary>
    public const string GetAdminUsersCount = @"
        SELECT COUNT(1) FROM dbo.Users WHERE IsDeleted = 0 AND IsAdmin = 1";

    /// <summary>
    /// Gets paginated users with filtering for admin management
    /// </summary>
    public const string GetUsersWithPagination = @"
        SELECT * FROM dbo.Users 
        WHERE IsDeleted = 0
        AND (@IncludeAdmins = 1 OR IsAdmin = 0)
        AND (@IncludeBanned = 1 OR IsBanned = 0)
        AND (
            @SearchTerm = '' OR 
            FirstName LIKE '%' + @SearchTerm + '%' OR 
            LastName LIKE '%' + @SearchTerm + '%' OR 
            Email LIKE '%' + @SearchTerm + '%'
        )
        ORDER BY UserID
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY";

    /// <summary>
    /// Gets total count of users with filtering for admin management
    /// </summary>
    public const string GetUsersCount = @"
        SELECT COUNT(1) FROM dbo.Users 
        WHERE IsDeleted = 0
        AND (@IncludeAdmins = 1 OR IsAdmin = 0)
        AND (@IncludeBanned = 1 OR IsBanned = 0)
        AND (
            @SearchTerm = '' OR 
            FirstName LIKE '%' + @SearchTerm + '%' OR 
            LastName LIKE '%' + @SearchTerm + '%' OR 
            Email LIKE '%' + @SearchTerm + '%'
        )";

    #endregion

    #region Update Operations

    /// <summary>
    /// Updates user information
    /// </summary>
    public const string Update = @"
        UPDATE dbo.Users 
        SET FirstName = @FirstName, LastName = @LastName, Email = @Email, 
            Phone = @Phone, House = @House, LastSeen = @LastSeen, ProfileImageURL = @ProfileImageURL
        WHERE UserID = @UserID AND IsDeleted = 0";

    /// <summary>
    /// Updates user profile information (Phone, House, ProfileImageURL) and LastSeen timestamp
    /// </summary>
    public const string UpdateProfile = @"
        UPDATE dbo.Users
        SET Phone=@phone, House=@house, ProfileImageURL=@profileImageUrl, LastSeen=SYSUTCDATETIME()
        WHERE UserID=@userId AND IsDeleted=0;
        SELECT @@ROWCOUNT;";

    /// <summary>
    /// Bans a user by setting IsBanned to 1
    /// </summary>
    public const string BanUser = @"
        UPDATE dbo.Users 
        SET IsBanned = 1, LastSeen = SYSUTCDATETIME() 
        WHERE UserID = @Id AND IsDeleted = 0;
        SELECT @@ROWCOUNT;";

    /// <summary>
    /// Unbans a user by setting IsBanned to 0
    /// </summary>
    public const string UnbanUser = @"
        UPDATE dbo.Users 
        SET IsBanned = 0, LastSeen = SYSUTCDATETIME() 
        WHERE UserID = @Id AND IsDeleted = 0;
        SELECT @@ROWCOUNT;";

    /// <summary>
    /// Soft delete by setting IsDeleted to 1
    /// </summary>
    public const string SoftDelete = @"
        UPDATE dbo.Users SET IsDeleted = 1 WHERE UserID = @Id";

    #endregion
}