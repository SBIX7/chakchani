using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CodeHollow.FeedReader;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SNRT.Application.Abstractions;
using SNRT.Domain.Users;
using SNRT.Infrastructure.Persistence;

namespace SNRT.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	private Guid _currentUserId;
	private readonly IServiceProvider _sp;
	private readonly IAuthService _auth;

	public MainWindow(IServiceProvider sp, IAuthService auth)
	{
		_sp = sp;
		_auth = auth;
		InitializeComponent();
		TitlesList.PreviewMouseLeftButtonDown += TitlesList_PreviewMouseLeftButtonDown;
		TitlesList.Drop += TitlesList_Drop;
		TitlesList.AllowDrop = true;
	}

	public void SetCurrentUser(Guid id)
	{
		_currentUserId = id;
		_ = LoadTitlesAsync();
		_ = LoadRssAsync();
		_ = UpdateAdminButtonAsync();
	}

	private async Task LoadTitlesAsync()
	{
		using var scope = _sp.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var titles = await db.TitleItems.OrderBy(t => t.Title).ToListAsync();
		var orders = await db.UserTitleOrders.Where(o => o.UserId == _currentUserId).ToListAsync();
		if (orders.Count == 0)
		{
			var i = 0;
			foreach (var t in titles)
			{
				db.UserTitleOrders.Add(new UserTitleOrder { UserId = _currentUserId, TitleItemId = t.Id, SortOrder = i++ });
			}
			await db.SaveChangesAsync();
			orders = await db.UserTitleOrders.Where(o => o.UserId == _currentUserId).ToListAsync();
		}
		var joined = titles.Join(orders, t => t.Id, o => o.TitleItemId, (t, o) => new { t.Id, t.Title, o.SortOrder })
			.OrderBy(x => x.SortOrder)
			.Select(x => x.Title)
			.ToList();
		TitlesList.ItemsSource = joined;
	}

	private Point _dragStartPoint;
	private void TitlesList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		_dragStartPoint = e.GetPosition(null);
	}

	private async void TitlesList_Drop(object sender, DragEventArgs e)
	{
		if (TitlesList.ItemsSource is not IEnumerable<string> items) return;
		var pos = e.GetPosition(TitlesList);
		var targetItem = GetItemAtPosition(pos);
		var sourceTitle = e.Data.GetData(typeof(string)) as string;
		if (sourceTitle == null || targetItem == null || Equals(sourceTitle, targetItem)) return;

		var list = items.ToList();
		var from = list.IndexOf(sourceTitle);
		var to = list.IndexOf(targetItem);
		if (from < 0 || to < 0) return;
		list.RemoveAt(from);
		list.Insert(to, sourceTitle);
		TitlesList.ItemsSource = list.ToList();

		// persist order
		using var scope = _sp.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var titleEntities = await db.TitleItems.ToListAsync();
		for (int i = 0; i < list.Count; i++)
		{
			var titleEntity = titleEntities.First(t => t.Title == list[i]);
			var order = await db.UserTitleOrders.FirstAsync(o => o.UserId == _currentUserId && o.TitleItemId == titleEntity.Id);
			order.SortOrder = i;
		}
		await db.SaveChangesAsync();
		Log.Information("Titles reordered for user {UserId}. From {From} to {To}", _currentUserId, from, to);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (e.LeftButton == MouseButtonState.Pressed && TitlesList.SelectedItem != null)
		{
			var diff = _dragStartPoint - e.GetPosition(null);
			if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
			{
				DragDrop.DoDragDrop(TitlesList, TitlesList.SelectedItem, DragDropEffects.Move);
			}
		}
	}

	private string? GetItemAtPosition(Point position)
	{
		var element = TitlesList.InputHitTest(position) as DependencyObject;
		while (element != null && element is not ListBoxItem)
		{
			element = VisualTreeHelper.GetParent(element);
		}
		return (element as ListBoxItem)?.Content as string;
	}

	private async Task LoadRssAsync()
	{
		Log.Information("Refreshing RSS feeds");
		var sources = new[]
		{
			("Hespress", "https://www.hespress.com/feed"),
			("Media24", "https://www.medias24.com/feed/")
		};
		var items = new List<dynamic>();
		foreach (var (name, url) in sources)
		{
			try
			{
				var feed = await FeedReader.ReadAsync(url);
				foreach (var i in feed.Items.Take(20))
				{
					items.Add(new { Source = name, Title = i.Title, Published = i.PublishingDateString });
				}
				Log.Information("RSS source {Source} refreshed with {Count} items", name, items.Count);
			}
			catch (Exception ex)
			{
				Log.Warning(ex, "RSS source {Source} failed", name);
			}
		}
		RssList.ItemsSource = items;
	}

	private async void OnLogoutClick(object sender, RoutedEventArgs e)
	{
		await _auth.LogoutAsync(_currentUserId, CancellationToken.None);
		var login = new Views.LoginWindow(_sp, _auth);
		login.Show();
		Close();
	}

	private async Task UpdateAdminButtonAsync()
	{
		using var scope = _sp.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var user = await db.Users.FirstAsync(u => u.Id == _currentUserId);
		var isAdmin = user.Role == UserRole.Admin;
		AdminBtn.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
	}

	private void OnAdminClick(object sender, RoutedEventArgs e)
	{
		var dash = new Views.AdminDashboardWindow(_sp);
		dash.Show();
	}
}