using Microsoft.EntityFrameworkCore;
using SNRT.Domain.Users;

namespace SNRT.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

	public DbSet<User> Users => Set<User>();
	public DbSet<LoginLog> LoginLogs => Set<LoginLog>();
	public DbSet<TitleItem> TitleItems => Set<TitleItem>();
	public DbSet<UserTitleOrder> UserTitleOrders => Set<UserTitleOrder>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<User>(entity =>
		{
			entity.HasIndex(u => u.Email).IsUnique();
			entity.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
			entity.Property(u => u.LastName).HasMaxLength(100).IsRequired();
			entity.Property(u => u.Email).HasMaxLength(200).IsRequired();
			entity.Property(u => u.PasswordHash).IsRequired();
		});

		modelBuilder.Entity<LoginLog>(entity =>
		{
			entity.HasOne(l => l.User)
				.WithMany(u => u.LoginLogs)
				.HasForeignKey(l => l.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<TitleItem>(entity =>
		{
			entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
		});

		modelBuilder.Entity<UserTitleOrder>(entity =>
		{
			entity.HasKey(x => new { x.UserId, x.TitleItemId });
			entity.HasOne(x => x.User)
				.WithMany(u => u.TitleOrders)
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Cascade);
			entity.HasOne(x => x.TitleItem)
				.WithMany()
				.HasForeignKey(x => x.TitleItemId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		// Seed 11 default titles with deterministic IDs (matching initial migration)
		modelBuilder.Entity<TitleItem>().HasData(new[]
		{
			new TitleItem { Id = Guid.Parse("5c7bb5cd-1fe1-4758-b37d-8d17f81dca41"), Title = "Title 1" },
			new TitleItem { Id = Guid.Parse("a408e591-d18c-457a-aa69-77bc20e7ed58"), Title = "Title 2" },
			new TitleItem { Id = Guid.Parse("3149a6a8-7996-4521-aef8-d245d96b5e69"), Title = "Title 3" },
			new TitleItem { Id = Guid.Parse("60139737-d9db-4459-8282-59b2ab2b9b14"), Title = "Title 4" },
			new TitleItem { Id = Guid.Parse("589bd436-7cf6-4207-90b2-090fba805646"), Title = "Title 5" },
			new TitleItem { Id = Guid.Parse("0cdcb930-144e-459b-83c5-e35c2b5f1c62"), Title = "Title 6" },
			new TitleItem { Id = Guid.Parse("7a3ab926-3976-4492-ad72-3b82e16c26a7"), Title = "Title 7" },
			new TitleItem { Id = Guid.Parse("df1bb89d-7031-4ba4-9ccd-1368c4599f40"), Title = "Title 8" },
			new TitleItem { Id = Guid.Parse("24a22e2b-6a09-42f2-a5d3-5e9ceb929de7"), Title = "Title 9" },
			new TitleItem { Id = Guid.Parse("871da67f-76a4-4ef6-b7db-3ae9fd384676"), Title = "Title 10" },
			new TitleItem { Id = Guid.Parse("62200d0b-c1cb-46af-a472-5d25ecc17faf"), Title = "Title 11" },
		});
	}
} 