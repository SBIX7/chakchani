using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SNRT.Application.Abstractions;

namespace SNRT.Desktop.Views;

public partial class LoginWindow : Window
{
	private readonly IServiceProvider _sp;
	private readonly IAuthService _auth;

	public LoginWindow(IServiceProvider sp, IAuthService auth)
	{
		_sp = sp;
		_auth = auth;
		InitializeComponent();
	}

	private async void OnLoginClick(object sender, RoutedEventArgs e)
	{
		ErrorText.Text = string.Empty;
		var email = EmailBox.Text.Trim();
		var password = PasswordBox.Password;
		var id = await _auth.LoginAsync(new LoginRequest(email, password), CancellationToken.None);
		if (id is null)
		{
			ErrorText.Text = "Invalid credentials";
			return;
		}
		var main = _sp.GetRequiredService<MainWindow>();
		main.SetCurrentUser(id.Value);
		main.Show();
		Close();
	}

	private void OnSignupClick(object sender, RoutedEventArgs e)
	{
		var signup = new SignupWindow(_sp, _auth);
		signup.Show();
		Close();
	}
} 