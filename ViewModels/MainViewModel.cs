using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Svg.Skia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KanjiStudy.Data;
using KanjiStudy.Factories;
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

        private const string buttonActiveClass = "active";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SideMenuWidth))]
        //[NotifyPropertyChangedFor(nameof(SideMenuImage))]
        //[NotifyPropertyChangedFor(nameof(SideMenuImageWidth))]
        private bool _sideMenuExpaned = true;

        public int SideMenuWidth => SideMenuExpaned ? 180 : 90;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HomePageIsActive))]
        [NotifyPropertyChangedFor(nameof(DeckPageIsActive))]
        [NotifyPropertyChangedFor(nameof(SettingsPageIsActive))]
        private PageViewModel _currentPage;

        public bool HomePageIsActive => CurrentPage.PageName == ApplicationPageNames.Home;
        public bool DeckPageIsActive => CurrentPage.PageName == ApplicationPageNames.Deck;
        public bool SettingsPageIsActive => CurrentPage.PageName == ApplicationPageNames.Deck;

        /// <summary>
        /// Design time only constructor
        /// </summary>
        public MainViewModel()
        {
            CurrentPage = new SettingsPageViewModel();
        }

        public MainViewModel(PageFactory pageFactory)
        {
            _pageFactory = pageFactory;
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
        private void GoToSettings()
        {
            CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.Settings);
        }
    }
}
