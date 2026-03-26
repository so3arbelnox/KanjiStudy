using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace KanjiStudy.ViewModels
{
    public partial class SettingsPageViewModel : PageViewModel
    {
        [ObservableProperty]
        private List<string> _locationPaths;

        public SettingsPageViewModel()
        {
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
    }
}
