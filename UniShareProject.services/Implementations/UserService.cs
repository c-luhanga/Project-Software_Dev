using AutoMapper;
using FluentValidation;
using UniShareProject.Repository.Repositories;
using UniShareProject.services.DTOs;
using UniShareProject.services.Interfaces;

namespace UniShareProject.services.Implementations;

/// <summary>
/// Service implementation for user-related operations
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<UpdateMeRequest> _updateMeValidator;

    public UserService(
        IUserRepository userRepository,
        IItemRepository itemRepository,
        IMapper mapper,
        IValidator<UpdateMeRequest> updateMeValidator)
    {
        _userRepository = userRepository;
        _itemRepository = itemRepository;
        _mapper = mapper;
        _updateMeValidator = updateMeValidator;
    }

    public async Task<UserDto?> GetMeAsync(int userId, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return null;

        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> UpdateMeAsync(int userId, UpdateMeRequest req, CancellationToken ct)
    {
        // Validate request
        var validationResult = await _updateMeValidator.ValidateAsync(req, ct);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new ValidationException($"Validation failed: {errors}");
        }

        // Update profile
        var rowsAffected = await _userRepository.UpdateProfileAsync(
            userId,
            req.Phone,
            req.House,
            req.ProfileImageUrl,
            ct);

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"User with ID {userId} not found or could not be updated");
        }

        // Re-fetch updated user
        var updatedUser = await _userRepository.GetByIdAsync(userId, ct);
        if (updatedUser is null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found after update");
        }

        return _mapper.Map<UserDto>(updatedUser);
    }

    public async Task<bool> BanUserAsync(int userId, int adminId, CancellationToken ct)
    {
        // Check if user exists
        var userExists = await _userRepository.UserExistsAsync(userId, ct);
        if (!userExists)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        // Ban the user
        var rowsAffected = await _userRepository.BanUserAsync(userId, ct);
        return rowsAffected > 0;
    }

    public async Task<bool> UnbanUserAsync(int userId, int adminId, CancellationToken ct)
    {
        // Check if user exists
        var userExists = await _userRepository.UserExistsAsync(userId, ct);
        if (!userExists)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        // Unban the user
        var rowsAffected = await _userRepository.UnbanUserAsync(userId, ct);
        return rowsAffected > 0;
    }

    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken ct)
    {
        // Gather all statistics concurrently for better performance
        var totalUsersTask = _userRepository.GetTotalUsersAsync(ct);
        var bannedUsersTask = _userRepository.GetBannedUsersCountAsync(ct);
        var adminUsersTask = _userRepository.GetAdminUsersCountAsync(ct);
        var totalItemsTask = _itemRepository.GetTotalItemsAsync(ct);
        var activeItemsTask = _itemRepository.GetActiveItemsCountAsync(ct);
        var pendingItemsTask = _itemRepository.GetPendingItemsCountAsync(ct);
        var soldItemsTask = _itemRepository.GetSoldItemsCountAsync(ct);
        var withdrawnItemsTask = _itemRepository.GetWithdrawnItemsCountAsync(ct);

        // Wait for all tasks to complete
        await Task.WhenAll(
            totalUsersTask,
            bannedUsersTask,
            adminUsersTask,
            totalItemsTask,
            activeItemsTask,
            pendingItemsTask,
            soldItemsTask,
            withdrawnItemsTask
        );

        return new AdminDashboardDto
        {
            TotalUsers = totalUsersTask.Result,
            BannedUsers = bannedUsersTask.Result,
            AdminUsers = adminUsersTask.Result,
            TotalItems = totalItemsTask.Result,
            ActiveItems = activeItemsTask.Result,
            PendingItems = pendingItemsTask.Result,
            SoldItems = soldItemsTask.Result,
            WithdrawnItems = withdrawnItemsTask.Result,
            LastUpdated = DateTime.UtcNow,
            Status = "Operational"
        };
    }

    public async Task<AdminUsersListDto> GetUsersAsync(int page, int pageSize, string? searchTerm, bool includeAdmins, bool includeBanned, CancellationToken ct)
    {
        // Get users with pagination
        var (users, totalCount) = await _userRepository.GetUsersAsync(page, pageSize, searchTerm, includeAdmins, includeBanned, ct);

        // Map users to DTOs
        var userDtos = users.Select(user => new AdminUserDto
        {
            Id = user.UserID,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            House = user.House,
            IsBanned = user.IsBanned,
            IsAdmin = user.IsAdmin,
            RegistrationDate = user.CreatedAt.ToString("O"), // ISO 8601 format
            LastLoginDate = user.LastSeen?.ToString("O"), // ISO 8601 format
            ProfileImageUrl = user.ProfileImageURL
        }).ToList();

        // Calculate pagination info
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        var hasNextPage = page < totalPages;
        var hasPreviousPage = page > 1;

        return new AdminUsersListDto
        {
            Users = userDtos,
            CurrentPage = page,
            PageSize = pageSize,
            TotalUsers = totalCount,
            TotalPages = totalPages,
            HasNextPage = hasNextPage,
            HasPreviousPage = hasPreviousPage
        };
    }
}
