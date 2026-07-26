using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using KanjiStudy;

namespace KanjiStudy.Android;

[Activity(
    Label = "KanjiStudy",
    Theme = "@style/MyTheme.NoActionBar",
    MainLauncher = true,
    ScreenOrientation = ScreenOrientation.SensorLandscape,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .UseAndroid();
    }
}
