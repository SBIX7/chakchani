using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SNRT.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
	public AppDbContext CreateDbContext(string[] args)
	{
		var builder = new DbContextOptionsBuilder<AppDbContext>();
		var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SNRT", "snrt.db");
		Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
		builder.UseSqlite($"Data Source={dbPath}");
		return new AppDbContext(builder.Options);
	}
} 