using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SNRT.Infrastructure.Persistence;

namespace SNRT.Desktop.Views;

public partial class AdminDashboardWindow : Window
{
	private readonly IServiceProvider _sp;

	public AdminDashboardWindow(IServiceProvider sp)
	{
		_sp = sp;
		InitializeComponent();
		_ = LoadUsersAsync();
		SetDefaultDates();
		_ = LoadLogsAsync();
	}

	private void SetDefaultDates()
	{
		ToDate.SelectedDate = DateTime.UtcNow.Date;
		FromDate.SelectedDate = DateTime.UtcNow.Date.AddDays(-7);
	}

	private async Task LoadUsersAsync()
	{
		using var scope = _sp.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var users = await db.Users.OrderBy(u => u.Email).ToListAsync();
		UserFilter.ItemsSource = users;
	}

	private async Task LoadLogsAsync()
	{
		using var scope = _sp.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var query = db.LoginLogs.Include(l => l.User).AsQueryable();

		if (UserFilter.SelectedValue is Guid userId)
		{
			query = query.Where(l => l.UserId == userId);
		}
		if (FromDate.SelectedDate is DateTime from)
		{
			var f = DateTime.SpecifyKind(from, DateTimeKind.Utc);
			query = query.Where(l => l.Timestamp >= f);
		}
		if (ToDate.SelectedDate is DateTime to)
		{
			var t = DateTime.SpecifyKind(to.AddDays(1), DateTimeKind.Utc);
			query = query.Where(l => l.Timestamp < t);
		}

		var data = await query.OrderByDescending(l => l.Timestamp)
			.Select(l => new { UserEmail = l.User!.Email, l.Timestamp, Status = l.IsOnline ? "Online" : "Offline", l.Note })
			.ToListAsync();
		LogsGrid.ItemsSource = data;
	}

	private async void OnApplyFilters(object sender, RoutedEventArgs e)
	{
		await LoadLogsAsync();
	}

	private void OnExportCsv(object? sender, RoutedEventArgs e)
	{
		var dlg = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = $"login-logs-{DateTime.UtcNow:yyyyMMdd}.csv" };
		if (dlg.ShowDialog() == true)
		{
			using var sw = new StreamWriter(dlg.FileName);
			sw.WriteLine("User,Timestamp,Status,Note");
			if (LogsGrid.ItemsSource is IEnumerable<object> rows)
			{
				foreach (var row in rows)
				{
					var props = row.GetType().GetProperties();
					string? user = props.First(p => p.Name == "UserEmail").GetValue(row)?.ToString();
					string? timestamp = props.First(p => p.Name == "Timestamp").GetValue(row)?.ToString();
					string? status = props.First(p => p.Name == "Status").GetValue(row)?.ToString();
					string? note = props.First(p => p.Name == "Note").GetValue(row)?.ToString();
					sw.WriteLine($"{Escape(user)},{Escape(timestamp)},{Escape(status)},{Escape(note)}");
				}
			}
		}
		static string Escape(string? s) => s is null ? string.Empty : "\"" + s.Replace("\"", "\"\"") + "\"";
	}
} 