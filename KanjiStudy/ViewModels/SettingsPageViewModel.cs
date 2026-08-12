using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using KanjiStudy.Data;
using KanjiStudy.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace KanjiStudy.ViewModels
{
    public partial class SettingsPageViewModel : PageViewModel
    {
        private readonly OrientationService _orientationService;

        [ObservableProperty]
        private List<string> _locationPaths;

        // The page is normally two content columns side by side; that's too narrow to be usable
        // on a phone (a column's already-narrow width gets eaten further by whichever control in
        // it doesn't wrap, e.g. a button), so in portrait it collapses to a single stacked column
        // instead - the right column's Width goes to 0 and its content moves into a third row
        // below the left column. Avalonia's Grid.ColumnDefinitions/RowDefinitions collections
        // themselves aren't bindable, but an individual ColumnDefinition's Width is, so the Grid
        // always declares the same 2 columns / 3 rows and this just changes their effective
        // shape. Computed once - like the rest of this app's orientation-driven layout, this only
        // ever changes via a full rebuild (see App.axaml.cs), not live.
        private bool IsPortrait => _orientationService.IsPortrait;

        public GridLength RightColumnWidth => IsPortrait ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        public int HeaderColumnSpan => IsPortrait ? 1 : 2;
        public int RightColumn => IsPortrait ? 0 : 1;
        public int RightRow => IsPortrait ? 2 : 1;

        // Three mutually-exclusive radio options over OrientationService.OrientationOverride
        // (null = automatic). Setting one to true applies the change immediately - see
        // OrientationService.SetOrientationOverride - which rebuilds the app's root view, so
        // there's no separate "Apply" step.
        public bool UseAutoOrientation
        {
            get => _orientationService.OrientationOverride is null;
            set { if (value) SetOrientationOverride(null); }
        }

        public bool UseLandscapeOrientation
        {
            get => _orientationService.OrientationOverride == AppOrientation.Landscape;
            set { if (value) SetOrientationOverride(AppOrientation.Landscape); }
        }

        public bool UsePortraitOrientation
        {
            get => _orientationService.OrientationOverride == AppOrientation.Portrait;
            set { if (value) SetOrientationOverride(AppOrientation.Portrait); }
        }

        public SettingsPageViewModel() : this(new OrientationService())
        {
        }

        public SettingsPageViewModel(OrientationService orientationService)
        {
            _orientationService = orientationService;
            PageName = Data.ApplicationPageNames.Settings;

            // TODO: Remove
            LocationPaths =
            [
                @"C:\Users\so3ar\Downloads\Deck1.deck",
                @"C:\Users\so3ar\Deck2.deck",
                @"C:\Users\so3ar\KanjiApp\Deck3.deck"
            ];
        }

        public string Test { get; set; } = "Settings";

        private void SetOrientationOverride(AppOrientation? orientation)
        {
            _orientationService.SetOrientationOverride(orientation);

            OnPropertyChanged(nameof(UseAutoOrientation));
            OnPropertyChanged(nameof(UseLandscapeOrientation));
            OnPropertyChanged(nameof(UsePortraitOrientation));
        }
    }
}
