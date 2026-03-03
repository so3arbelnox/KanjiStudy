using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Svg.Skia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        private ViewModelBase _currentPage;

        public bool HomePageIsActive => CurrentPage == _homePage;
        public bool DeckPageIsActive => CurrentPage == _deckPage;

        private readonly HomePageViewModel _homePage = new();
        private readonly DeckPageViewModel _deckPage = new();

        public MainViewModel()
        {
            CurrentPage = _homePage;
        }

        [RelayCommand]
        private void SideMenuResize()
        {
            SideMenuExpaned = !SideMenuExpaned;
        }

        [RelayCommand]
        private void GoToHome()
        {
            CurrentPage = _homePage;
        }

        [RelayCommand]
        private void GoToDeck()
        {
            CurrentPage = _deckPage;
        }
    }
}
