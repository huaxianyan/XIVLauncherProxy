using Avalonia.Controls;
using Avalonia.Interactivity;
using XIVLauncherProxy.Domain.Authentication;

namespace XIVLauncherProxy.Desktop.Views;

public partial class OtpImportWindow : Window
{
    public OtpImportWindow() : this([]) { }

    public OtpImportWindow(IReadOnlyList<OtpImportEntry> entries)
    {
        InitializeComponent();
        EntryList.ItemsSource = entries;
        EntryList.SelectedIndex = entries.Count > 0 ? 0 : -1;
    }

    private void ImportClick(object? sender, RoutedEventArgs e) =>
        Close(EntryList.SelectedItem as OtpImportEntry);

    private void CancelClick(object? sender, RoutedEventArgs e) => Close(null);
}
