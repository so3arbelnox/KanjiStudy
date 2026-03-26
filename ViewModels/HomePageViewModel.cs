using System;
using System.Collections.Generic;
using System.Text;

namespace KanjiStudy.ViewModels
{
    public partial class HomePageViewModel : PageViewModel
    {
        public HomePageViewModel()
        {
            PageName = Data.ApplicationPageNames.Home;
        }

        public string Test { get; set; } = "Home";
    }
}
