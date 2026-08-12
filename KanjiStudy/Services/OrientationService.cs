using CommunityToolkit.Mvvm.ComponentModel;
using KanjiStudy.Data;
using System;

namespace KanjiStudy.Services
{
    /// <summary>
    /// Picks and persists the app's layout orientation.
    ///
    /// The default is device-based (desktop/tablet -> landscape, phone -> portrait), computed
    /// once from <see cref="DeviceClassResolver"/>. The user can override that default from the
    /// Settings page; the override (or lack of one) is persisted via AppSettingsStore.
    ///
    /// This is a DI singleton (registered in App.axaml.cs) rather than something read inline via
    /// OperatingSystem.* the way most platform differences in this app are handled, because it
    /// needs two things only the host project can provide: the real device class (Android's
    /// smallest-screen-width can't be read from shared code) and a way to apply a change to the
    /// actual platform surface (Activity.RequestedOrientation on Android). Each host assigns
    /// <see cref="DeviceClassResolver"/> and <see cref="PlatformApply"/> to itself at startup;
    /// desktop doesn't need to do either, since the defaults already suit it.
    ///
    /// Changing the orientation rebuilds the app's root view (see App.axaml.cs) rather than
    /// trying to re-flow every bound layout live, so view models only ever need to read the
    /// orientation once, at construction.
    /// </summary>
    public partial class OrientationService : ObservableObject
    {
        /// <summary>
        /// Assigned by the current host to report its real device class. Desktop leaves this
        /// null, which resolves to <see cref="DeviceClass.Desktop"/>.
        /// </summary>
        public static Func<DeviceClass>? DeviceClassResolver { get; set; }

        /// <summary>
        /// Assigned by the current host so a runtime orientation change reaches the actual
        /// platform surface (e.g. Android's RequestedOrientation). Left null where nothing
        /// platform-specific needs to happen.
        /// </summary>
        public static Action<AppOrientation>? PlatformApply { get; set; }

        /// <summary>
        /// Raised after a change has been persisted and applied to the platform, asking the host
        /// to rebuild the root view so every view model picks up the new orientation fresh.
        /// </summary>
        public event EventHandler? RestartRequested;

        public DeviceClass DeviceClass { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Orientation))]
        [NotifyPropertyChangedFor(nameof(IsPortrait))]
        private AppOrientation? _orientationOverride;

        public AppOrientation Orientation => OrientationOverride ?? DefaultOrientationForDevice;

        public bool IsPortrait => Orientation == AppOrientation.Portrait;

        private AppOrientation DefaultOrientationForDevice =>
            DeviceClass == DeviceClass.Phone ? AppOrientation.Portrait : AppOrientation.Landscape;

        public OrientationService()
        {
            DeviceClass = (DeviceClassResolver ?? (() => Data.DeviceClass.Desktop))();
            _orientationOverride = AppSettingsStore.Load().OrientationOverride;
        }

        /// <summary>
        /// Sets an explicit override, or null to go back to the device-based default.
        /// </summary>
        public void SetOrientationOverride(AppOrientation? orientation)
        {
            if (OrientationOverride == orientation)
            {
                return;
            }

            OrientationOverride = orientation;

            var settings = AppSettingsStore.Load();
            settings.OrientationOverride = orientation;
            AppSettingsStore.Save(settings);

            RestartRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
