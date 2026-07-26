using KanjiStudy.Data;
using KanjiStudy.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace KanjiStudy.Factories
{
    public class PageFactory(Func<ApplicationPageNames, PageViewModel> pageFactory)
    {
        public PageViewModel GetPageViewModel(ApplicationPageNames pageName) => pageFactory.Invoke(pageName);
    }
}
