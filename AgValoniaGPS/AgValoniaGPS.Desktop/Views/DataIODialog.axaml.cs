using Avalonia.Controls;
using Avalonia.Interactivity;
using AgValoniaGPS.ViewModels;

namespace AgValoniaGPS.Desktop.Views;

public partial class DataIODialog : Window
{
    private MainViewModel? ViewModel => DataContext as MainViewModel;

    public DataIODialog()
    {
        InitializeComponent();
    }

    private async void BtnNtripConnect_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.ConnectToNtripAsync();
        }
    }

    private async void BtnNtripDisconnect_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.DisconnectFromNtripAsync();
        }
    }

    private void BtnClose_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
