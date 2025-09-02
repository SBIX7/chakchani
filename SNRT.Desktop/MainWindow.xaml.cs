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
	private List<CombinedItem> _combinedItems = new();

	public MainWindow(IServiceProvider sp, IAuthService auth)
	{
		_sp = sp;
		_auth = auth;
		InitializeComponent();
		CombinedList.PreviewMouseLeftButtonDown += CombinedList_PreviewMouseLeftButtonDown;
		CombinedList.Drop += CombinedList_Drop;
		CombinedList.AllowDrop = true;
	}

	public void SetCurrentUser(Guid id)
	{
		_currentUserId = id;
		_ = LoadCombinedAsync();
		_ = UpdateAdminButtonAsync();
	}

	private async Task LoadCombinedAsync()
	{
		using var scope = _sp.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		var rssItems = new List<CombinedItem>();
		var sources = new[]
		{
			("Hespress", "https://www.hespress.com/feed"),
			("Media24", "https://www.medias24.com/feed/")
		};
		foreach (var (name, url) in sources)
		{
			try
			{
				var feed = await FeedReader.ReadAsync(url);
				foreach (var i in feed.Items.Take(100))
				{
					var titleText = i.Title ?? string.Empty;
					var key = $"rss|{name}|{titleText}";
					rssItems.Add(new CombinedItem
					{
						Source = name,
						Title = titleText,
						Published = i.PublishingDateString,
						Url = i.Link,
						ItemKey = key
					});
				}
				Log.Information("RSS source {Source} refreshed with {Count} items", name, rssItems.Count);
			}
			catch (Exception ex)
			{
				Log.Warning(ex, "RSS source {Source} failed", name);
			}
		}

		var combined = rssItems
			.GroupBy(x => x.ItemKey)
			.Select(g => g.First())
			.ToList();

		var existingOrders = await db.UserDisplayOrders
			.Where(o => o.UserId == _currentUserId)
			.ToDictionaryAsync(o => o.ItemKey, o => o.SortOrder);

		int maxOrder = existingOrders.Count == 0 ? -1 : existingOrders.Values.Max();
		foreach (var item in combined)
		{
			if (!existingOrders.ContainsKey(item.ItemKey))
			{
				existingOrders[item.ItemKey] = ++maxOrder;
				db.UserDisplayOrders.Add(new UserDisplayOrder
				{
					UserId = _currentUserId,
					ItemKey = item.ItemKey,
					SortOrder = existingOrders[item.ItemKey]
				});
			}
		}
		await db.SaveChangesAsync();

		_combinedItems = combined
			.OrderBy(i => existingOrders[i.ItemKey])
			.ToList();
		CombinedList.ItemsSource = _combinedItems;
	}

	private Point _dragStartPoint;
	private void CombinedList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		_dragStartPoint = e.GetPosition(null);
	}

	private async void CombinedList_Drop(object sender, DragEventArgs e)
	{
		if (CombinedList.ItemsSource is not IEnumerable<CombinedItem> items) return;
		var pos = e.GetPosition(CombinedList);
		var targetItem = GetItemAtPosition(pos);
		var sourceItem = e.Data.GetData(typeof(CombinedItem)) as CombinedItem ?? CombinedList.SelectedItem as CombinedItem;
		if (sourceItem == null || targetItem == null || ReferenceEquals(sourceItem, targetItem)) return;

		var list = items.ToList();
		var from = list.IndexOf(sourceItem);
		var to = list.IndexOf(targetItem);
		if (from < 0 || to < 0) return;
		list.RemoveAt(from);
		list.Insert(to, sourceItem);
		CombinedList.ItemsSource = list.ToList();

		using var scope = _sp.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		for (int i = 0; i < list.Count; i++)
		{
			var key = list[i].ItemKey;
			var order = await db.UserDisplayOrders.FirstOrDefaultAsync(o => o.UserId == _currentUserId && o.ItemKey == key);
			if (order is null)
			{
				order = new UserDisplayOrder { UserId = _currentUserId, ItemKey = key, SortOrder = i };
				db.UserDisplayOrders.Add(order);
			}
			else
			{
				order.SortOrder = i;
			}
		}
		await db.SaveChangesAsync();
		Log.Information("Combined titles reordered for user {UserId}. From {From} to {To}", _currentUserId, from, to);
		_combinedItems = list;
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (e.LeftButton == MouseButtonState.Pressed && CombinedList.SelectedItem != null)
		{
			var diff = _dragStartPoint - e.GetPosition(null);
			if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
			{
				DragDrop.DoDragDrop(CombinedList, CombinedList.SelectedItem, DragDropEffects.Move);
			}
		}
	}

	private CombinedItem? GetItemAtPosition(Point position)
	{
		var element = CombinedList.InputHitTest(position) as DependencyObject;
		while (element != null && element is not ListViewItem)
		{
			element = VisualTreeHelper.GetParent(element);
		}
		return (element as ListViewItem)?.Content as CombinedItem;
	}

	private async void OnLogoutClick(object sender, RoutedEventArgs e)
	{
		await _auth.LogoutAsync(_currentUserId, CancellationToken.None);
		var login = _sp.GetRequiredService<Views.LoginWindow>();
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

public class CombinedItem
{
	public string Source { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public string? Published { get; set; }
	public string? Url { get; set; }
	public string ItemKey { get; set; } = string.Empty;
}