using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SNRT.Application.Abstractions;
using SNRT.Infrastructure.Auth;
using SNRT.Infrastructure.Persistence;

namespace SNRT.Tests;

public class AuthServiceTests
{
	private static IDbContextFactory<AppDbContext> CreateInMemoryFactory()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		return new TestDbContextFactory(options);
	}

	[Fact]
	public async Task Signup_Creates_User_And_Hashes_Password()
	{
		var factory = CreateInMemoryFactory();
		var sut = new AuthService(factory);
		var id = await sut.SignupAsync(new SignupRequest("John", "Doe", "john@example.com", "P@ssw0rd", "P@ssw0rd"), CancellationToken.None);
		id.Should().NotBeEmpty();
		await using var db = await factory.CreateDbContextAsync();
		var user = await db.Users.FirstAsync(u => u.Id == id);
		user.Email.Should().Be("john@example.com");
		user.PasswordHash.Should().NotBeNullOrEmpty();
		user.PasswordHash.Should().NotBe("P@ssw0rd");
	}

	[Fact]
	public async Task Login_Returns_UserId_For_Valid_Credentials()
	{
		var factory = CreateInMemoryFactory();
		var sut = new AuthService(factory);
		var id = await sut.SignupAsync(new SignupRequest("Jane", "Doe", "jane@example.com", "Secret!1", "Secret!1"), CancellationToken.None);
		var loggedIn = await sut.LoginAsync(new LoginRequest("jane@example.com", "Secret!1"), CancellationToken.None);
		loggedIn.Should().Be(id);
		await using var db = await factory.CreateDbContextAsync();
		var user = await db.Users.FirstAsync(u => u.Id == id);
		user.IsOnline.Should().BeTrue();
		user.LastLoginAt.Should().NotBeNull();
		( await db.LoginLogs.CountAsync(l => l.UserId == id && l.IsOnline) ).Should().Be(1);
	}

	[Fact]
	public async Task Login_Returns_Null_For_Invalid_Credentials()
	{
		var factory = CreateInMemoryFactory();
		var sut = new AuthService(factory);
		var id = await sut.SignupAsync(new SignupRequest("Jane", "Doe", "jane2@example.com", "Secret!1", "Secret!1"), CancellationToken.None);
		var loggedIn = await sut.LoginAsync(new LoginRequest("jane2@example.com", "Wrong"), CancellationToken.None);
		loggedIn.Should().BeNull();
		await using var db = await factory.CreateDbContextAsync();
		var user = await db.Users.FirstAsync(u => u.Id == id);
		user.IsOnline.Should().BeFalse();
		( await db.LoginLogs.CountAsync(l => l.UserId == id) ).Should().Be(0);
	}
} 

internal sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
{
	private readonly DbContextOptions<AppDbContext> _options;

	public TestDbContextFactory(DbContextOptions<AppDbContext> options)
	{
		_options = options;
	}

	public AppDbContext CreateDbContext()
	{
		return new AppDbContext(_options);
	}

	public ValueTask<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult(new AppDbContext(_options));
	}
}