using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SNRT.Application.Abstractions;
using SNRT.Domain.Users;
using SNRT.Infrastructure.Persistence;

namespace SNRT.Infrastructure.Auth;

public class AuthService : IAuthService
{
	private readonly IDbContextFactory<AppDbContext> _dbFactory;

	public AuthService(IDbContextFactory<AppDbContext> dbFactory)
	{
		_dbFactory = dbFactory;
	}

	public async Task<Guid> SignupAsync(SignupRequest request, CancellationToken cancellationToken)
	{
		if (request.Password != request.ConfirmPassword)
		{
			Log.Warning("Signup password mismatch for {Email}", request.Email);
			throw new ArgumentException("Passwords do not match");
		}
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
		var exists = await db.Users.AnyAsync(u => u.Email == request.Email, cancellationToken);
		if (exists)
		{
			Log.Warning("Signup attempt with existing email {Email}", request.Email);
			throw new InvalidOperationException("Email already registered");
		}

		var user = new User
		{
			FirstName = request.FirstName.Trim(),
			LastName = request.LastName.Trim(),
			Email = request.Email.Trim().ToLowerInvariant(),
			PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
			Role = UserRole.Employee,
			IsOnline = false
		};
		db.Users.Add(user);
		await db.SaveChangesAsync(cancellationToken);
		Log.Information("User signed up {Email} with Id {UserId}", user.Email, user.Id);
		return user.Id;
	}

	public async Task<Guid?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
		var email = request.Email.Trim().ToLowerInvariant();
		var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
		if (user == null)
		{
			Log.Warning("Login failed for non-existent user {Email}", email);
			return null;
		}

		var ok = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
		if (!ok)
		{
			Log.Warning("Login failed (bad password) for {Email}", email);
			return null;
		}

		user.IsOnline = true;
		user.LastLoginAt = DateTimeOffset.UtcNow;
		db.LoginLogs.Add(new LoginLog { UserId = user.Id, Timestamp = user.LastLoginAt.Value, IsOnline = true });
		await db.SaveChangesAsync(cancellationToken);
		Log.Information("User logged in {Email} ({UserId})", user.Email, user.Id);
		return user.Id;
	}

	public async Task LogoutAsync(Guid userId, CancellationToken cancellationToken)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
		var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
		if (user == null)
		{
			Log.Warning("Logout for unknown user {UserId}", userId);
			return;
		}
		user.IsOnline = false;
		db.LoginLogs.Add(new LoginLog { UserId = user.Id, Timestamp = DateTimeOffset.UtcNow, IsOnline = false });
		await db.SaveChangesAsync(cancellationToken);
		Log.Information("User logged out {Email} ({UserId})", user.Email, user.Id);
	}
} 