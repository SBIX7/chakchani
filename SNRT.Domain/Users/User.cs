namespace SNRT.Domain.Users;

public enum UserRole
{
	Employee = 0,
	Admin = 1
}

public class User
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string PasswordHash { get; set; } = string.Empty;
	public UserRole Role { get; set; } = UserRole.Employee;
	public DateTimeOffset? LastLoginAt { get; set; }
	public bool IsOnline { get; set; }

	public ICollection<LoginLog> LoginLogs { get; set; } = new List<LoginLog>();
	public ICollection<UserTitleOrder> TitleOrders { get; set; } = new List<UserTitleOrder>();
}

public class LoginLog
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid UserId { get; set; }
	public DateTimeOffset Timestamp { get; set; }
	public bool IsOnline { get; set; }
	public string? Note { get; set; }

	public User? User { get; set; }
}

public class TitleItem
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Title { get; set; } = string.Empty;
	public string? Description { get; set; }
}

public class UserTitleOrder
{
	public Guid UserId { get; set; }
	public Guid TitleItemId { get; set; }
	public int SortOrder { get; set; }

	public User? User { get; set; }
	public TitleItem? TitleItem { get; set; }
} 