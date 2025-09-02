using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using SNRT.Infrastructure.Di;
using SNRT.Infrastructure.Persistence;
using SNRT.Desktop.Views;

namespace SNRT.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
	private IHost? _host;

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		// Global exception handling
		DispatcherUnhandledException += (s, ex) =>
		{
			Log.Error(ex.Exception, "DispatcherUnhandledException");
			ex.Handled = true;
			MessageBox.Show(ex.Exception.Message, "Unexpected error");
		};
		AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
		{
			Log.Error(ex.ExceptionObject as Exception, "UnhandledException");
		};
		TaskScheduler.UnobservedTaskException += (s, ex) =>
		{
			Log.Error(ex.Exception, "UnobservedTaskException");
			ex.SetObserved();
		};

		var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SNRT");
		Directory.CreateDirectory(dataDir);
		var dbPath = Path.Combine(dataDir, "snrt.db");
		var logPath = Path.Combine(dataDir, "logs", "snrt-.log");
		Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
			.Enrich.FromLogContext()
			.WriteTo.Console()
			.WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
			.CreateLogger();

		_host = Host.CreateDefaultBuilder()
			.UseSerilog()
			.ConfigureServices(services =>
			{
				services.AddInfrastructure(dbPath);
				services.AddSingleton<LoginWindow>();
				services.AddSingleton<MainWindow>();
			})
			.Build();

		using (var scope = _host.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			db.Database.Migrate();
		}

		Log.Information("SNRT app started");
		_host.Start();
		var login = _host.Services.GetRequiredService<LoginWindow>();
		login.Show();
	}

	protected override void OnExit(ExitEventArgs e)
	{
		Log.Information("SNRT app exiting");
		_host?.Dispose();
		Log.CloseAndFlush();
		base.OnExit(e);
	}
}

