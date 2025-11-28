using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AgValoniaGPS.iOS;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            // iOS uses single view lifetime - minimal test view for now
            singleViewPlatform.MainView = new iOSMainView();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
