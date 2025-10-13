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
    private readonly IMapper _mapper;
    private readonly IValidator<UpdateMeRequest> _updateMeValidator;

    public UserService(
        IUserRepository userRepository,
        IMapper mapper,
        IValidator<UpdateMeRequest> updateMeValidator)
    {
        _userRepository = userRepository;
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
}
