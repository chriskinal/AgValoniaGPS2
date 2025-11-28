using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AgValoniaGPS.iOS;

public partial class iOSMainView : UserControl
{
    public iOSMainView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
