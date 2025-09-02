namespace SNRT.Application.Abstractions;

public record SignupRequest(string FirstName, string LastName, string Email, string Password, string ConfirmPassword);
public record LoginRequest(string Email, string Password);

public interface IAuthService
{
	Task<Guid> SignupAsync(SignupRequest request, CancellationToken cancellationToken);
	Task<Guid?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
	Task LogoutAsync(Guid userId, CancellationToken cancellationToken);
} 