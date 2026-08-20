using Avalonia.Controls;
using Avalonia.Interactivity;
using XIVLauncherProxy.Desktop.Localization;

namespace XIVLauncherProxy.Desktop.Views;

public partial class ConfirmationWindow : Window
{
    public ConfirmationWindow() : this(string.Empty) { }

    public ConfirmationWindow(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
        Title = ResourceTextLocalizer.Instance["AppTitle"];
    }

    private void OkClick(object? sender, RoutedEventArgs e) => Close(true);
    private void CancelClick(object? sender, RoutedEventArgs e) => Close(false);
}
