using System.Windows;
using SNRT.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace SNRT.Desktop.Views;

public partial class SignupWindow : Window
{
	private readonly IServiceProvider _sp;
	private readonly IAuthService _auth;

	public SignupWindow(IServiceProvider sp, IAuthService auth)
	{
		_sp = sp;
		_auth = auth;
		InitializeComponent();
	}

	private async void OnCreateClick(object sender, RoutedEventArgs e)
	{
		ErrorText.Text = string.Empty;
		try
		{
			var req = new SignupRequest(
				FirstNameBox.Text.Trim(),
				LastNameBox.Text.Trim(),
				EmailBox.Text.Trim(),
				PasswordBox.Password,
				ConfirmBox.Password
			);
			await _auth.SignupAsync(req, CancellationToken.None);
			Log.Information("Signup succeeded for {Email}", req.Email);
			var login = _sp.GetRequiredService<LoginWindow>();
			login.Show();
			Close();
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Signup failed for {Email}", EmailBox.Text.Trim());
			ErrorText.Text = ex.Message;
		}
	}

	private void OnBackClick(object sender, RoutedEventArgs e)
	{
		Log.Information("Signup cancelled, returning to login");
		var login = _sp.GetRequiredService<LoginWindow>();
		login.Show();
		Close();
	}
} 