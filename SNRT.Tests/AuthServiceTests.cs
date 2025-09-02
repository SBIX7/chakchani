using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SNRT.Application.Abstractions;
using SNRT.Infrastructure.Auth;
using SNRT.Infrastructure.Persistence;

namespace SNRT.Tests;

public class AuthServiceTests
{
	private static AppDbContext CreateInMemoryDb()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		var db = new AppDbContext(options);
		return db;
	}

	[Fact]
	public async Task Signup_Creates_User_And_Hashes_Password()
	{
		var db = CreateInMemoryDb();
		var sut = new AuthService(db);
		var id = await sut.SignupAsync(new SignupRequest("John", "Doe", "john@example.com", "P@ssw0rd", "P@ssw0rd"), CancellationToken.None);
		id.Should().NotBeEmpty();
		var user = await db.Users.FirstAsync(u => u.Id == id);
		user.Email.Should().Be("john@example.com");
		user.PasswordHash.Should().NotBeNullOrEmpty();
		user.PasswordHash.Should().NotBe("P@ssw0rd");
	}

	[Fact]
	public async Task Login_Returns_UserId_For_Valid_Credentials()
	{
		var db = CreateInMemoryDb();
		var sut = new AuthService(db);
		var id = await sut.SignupAsync(new SignupRequest("Jane", "Doe", "jane@example.com", "Secret!1", "Secret!1"), CancellationToken.None);
		var loggedIn = await sut.LoginAsync(new LoginRequest("jane@example.com", "Secret!1"), CancellationToken.None);
		loggedIn.Should().Be(id);
		var user = await db.Users.FirstAsync(u => u.Id == id);
		user.IsOnline.Should().BeTrue();
		user.LastLoginAt.Should().NotBeNull();
		( await db.LoginLogs.CountAsync(l => l.UserId == id && l.IsOnline) ).Should().Be(1);
	}

	[Fact]
	public async Task Login_Returns_Null_For_Invalid_Credentials()
	{
		var db = CreateInMemoryDb();
		var sut = new AuthService(db);
		var id = await sut.SignupAsync(new SignupRequest("Jane", "Doe", "jane2@example.com", "Secret!1", "Secret!1"), CancellationToken.None);
		var loggedIn = await sut.LoginAsync(new LoginRequest("jane2@example.com", "Wrong"), CancellationToken.None);
		loggedIn.Should().BeNull();
		var user = await db.Users.FirstAsync(u => u.Id == id);
		user.IsOnline.Should().BeFalse();
		( await db.LoginLogs.CountAsync(l => l.UserId == id) ).Should().Be(0);
	}
} 