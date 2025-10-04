using UniShareProject.services.DTOs;

namespace UniShareProject.services.Interfaces;

public interface IAuthService
{
    Task<int> RegisterAsync(RegisterRequest req, CancellationToken ct);
    Task<LoginResponse> LoginAsync(LoginRequest req, CancellationToken ct);
}