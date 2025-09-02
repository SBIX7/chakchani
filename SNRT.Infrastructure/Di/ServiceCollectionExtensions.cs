using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SNRT.Application.Abstractions;
using SNRT.Infrastructure.Auth;
using SNRT.Infrastructure.Persistence;

namespace SNRT.Infrastructure.Di;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, string sqlitePath)
	{
		services.AddDbContext<AppDbContext>(options =>
		{
			options.UseSqlite($"Data Source={sqlitePath}");
		});
		services.AddScoped<IAuthService, AuthService>();
		return services;
	}
} 