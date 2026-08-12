using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using KanjiStudy;
using KanjiStudy.Data;
using KanjiStudy.Services;

namespace KanjiStudy.Android;

[Activity(
    Label = "KanjiStudy",
    Theme = "@style/MyTheme.NoActionBar",
    MainLauncher = true,
    ScreenOrientation = ScreenOrientation.UserLandscape,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        // Wired up before the shared App builds its DI container, so OrientationService picks
        // these up as soon as it's constructed - see Services/OrientationService.cs.
        OrientationService.DeviceClassResolver = GetDeviceClass;
        OrientationService.PlatformApply = ApplyOrientation;

        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .UseAndroid();
    }

    // Android's own convention for "tablet": smallest-width >= 600dp (the same threshold the
    // sw600dp resource qualifier uses).
    private DeviceClass GetDeviceClass()
    {
        var smallestWidthDp = Resources?.Configuration?.SmallestScreenWidthDp ?? 0;
        return smallestWidthDp >= 600 ? DeviceClass.Tablet : DeviceClass.Phone;
    }

    private void ApplyOrientation(AppOrientation orientation)
    {
        RequestedOrientation = orientation == AppOrientation.Portrait
            ? ScreenOrientation.UserPortrait
            : ScreenOrientation.UserLandscape;
    }
}
