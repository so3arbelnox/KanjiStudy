using System;
using System.Collections.Generic;
using System.Text;

namespace KanjiStudy.ViewModels
{
    public partial class DeckPageViewModel : PageViewModel
    {
        public DeckPageViewModel()
        {
            PageName = Data.ApplicationPageNames.Deck;
        }

        public string Test { get; set; } = "Decks";
    }
}
