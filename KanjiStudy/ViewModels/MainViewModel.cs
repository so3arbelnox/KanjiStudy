using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Svg.Skia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KanjiStudy.Data;
using KanjiStudy.Factories;
using KanjiStudy.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace KanjiStudy.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        /*
            public IImage SideMenuImage => new Bitmap(AssetLoader.Open(new Uri($"avares://{nameof(KanjiStudy)}/Assets/Images/{(SideMenuExpaned ? "kanji_logo_transparent" : "kanji_logo_no_text_transparent")}.png")));

            public int SideMenuImageWidth => SideMenuExpaned ? 100 : 40;
        */

        private PageFactory _pageFactory;
        private OrientationService _orientationService;

        private const string buttonActiveClass = "active";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SideMenuWidth))]
        [NotifyPropertyChangedFor(nameof(ShowNavLabels))]
        [NotifyPropertyChangedFor(nameof(ShowExpandedLogo))]
        [NotifyPropertyChangedFor(nameof(ShowCollapsedLogo))]
        //[NotifyPropertyChangedFor(nameof(SideMenuImage))]
        //[NotifyPropertyChangedFor(nameof(SideMenuImageWidth))]
        private bool _sideMenuExpaned = true;

        public int SideMenuWidth => SideMenuExpaned ? 180 : 90;

        // Orientation only changes via a full app rebuild (see App.axaml.cs), so these are safe
        // to compute once here rather than needing live change notification.
        public bool IsPortrait => _orientationService.IsPortrait;

        // Landscape: a full-height rail down the left side. Portrait: a compact bar along the
        // bottom, which suits a narrow/tall window (or a phone) far better than a side rail would.
        public Dock NavDock => IsPortrait ? Dock.Bottom : Dock.Left;

        // Only one of these actually constrains the nav Border - Dock uses the other axis to fill
        // the available space - so the unused one is NaN ("unset"), same as Avalonia's own default.
        public double NavWidth => IsPortrait ? double.NaN : SideMenuWidth;
        public double NavHeight => IsPortrait ? 84 : double.NaN;

        // Portrait's edge padding comes from the nav Grid's own spacer columns (see MainView.axaml -
        // they're sized to match the gaps between buttons), so the Border itself only needs
        // top/bottom padding there.
        public Thickness SideMenuPadding => IsPortrait
            ? new Thickness(0, 6)
            : (OperatingSystem.IsAndroid() ? new Thickness(10) : new Thickness(20));

        // The bottom bar is too short for icon+label buttons, so labels never show there,
        // regardless of the expand/collapse toggle (which portrait doesn't expose anyway).
        public bool ShowNavLabels => SideMenuExpaned && !IsPortrait;
        public bool ShowExpandedLogo => SideMenuExpaned && !IsPortrait;
        public bool ShowCollapsedLogo => !SideMenuExpaned && !IsPortrait;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HomePageIsActive))]
        [NotifyPropertyChangedFor(nameof(DeckPageIsActive))]
        [NotifyPropertyChangedFor(nameof(StudyPageIsActive))]
        [NotifyPropertyChangedFor(nameof(SettingsPageIsActive))]
        private PageViewModel _currentPage;

        public bool HomePageIsActive => CurrentPage.PageName == ApplicationPageNames.Home;
        public bool DeckPageIsActive => CurrentPage.PageName == ApplicationPageNames.Deck;
        public bool StudyPageIsActive => CurrentPage.PageName == ApplicationPageNames.Study;
        public bool SettingsPageIsActive => CurrentPage.PageName == ApplicationPageNames.Settings;

        /// <summary>
        /// Design time only constructor
        /// </summary>
        public MainViewModel()
        {
            _orientationService = new OrientationService();
            CurrentPage = new SettingsPageViewModel();
        }

        public MainViewModel(PageFactory pageFactory, OrientationService orientationService)
        {
            _pageFactory = pageFactory;
            _orientationService = orientationService;
            SideMenuExpaned = !OperatingSystem.IsAndroid();
            GoToHome();
        }

        [RelayCommand]
        private void SideMenuResize()
        {
            SideMenuExpaned = !SideMenuExpaned;
        }

        [RelayCommand]
        private void GoToHome()
        {
            CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.Home);
        }

        [RelayCommand]
        private void GoToDeck()
        {
            CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.Deck);
        }

        [RelayCommand]
        private void GoToStudy()
        {
            CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.Study);
        }

        [RelayCommand]
        private void GoToSettings()
        {
            CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.Settings);
        }
    }
}
