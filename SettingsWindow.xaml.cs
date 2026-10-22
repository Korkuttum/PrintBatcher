using System.Windows;
using PrintBatcher.Models;

namespace PrintBatcher;

public partial class SettingsWindow : Window
{
    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        DataContext = settings;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
