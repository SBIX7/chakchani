using System.Windows;
using SNRT.Application.Abstractions;

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
			var login = new LoginWindow(_sp, _auth);
			login.Show();
			Close();
		}
		catch (Exception ex)
		{
			ErrorText.Text = ex.Message;
		}
	}

	private void OnBackClick(object sender, RoutedEventArgs e)
	{
		var login = new LoginWindow(_sp, _auth);
		login.Show();
		Close();
	}
} 