using CommunityToolkit.Mvvm.ComponentModel;
using KanjiStudy.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace KanjiStudy.ViewModels
{
    public partial class PageViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ApplicationPageNames _pageName;
    }
}
