using Avalonia.Controls;
using KanjiStudy.ViewModels;
using System;
using System.Diagnostics;

namespace KanjiStudy.Views
{
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
        }

        private void Home_Button_Clicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Trace.WriteLine("Test");
        }

        private void Image_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (e.ClickCount != 2)
            {
                return;
            }

            (DataContext as MainViewModel)?.SideMenuResizeCommand?.Execute(null);
        }
    }
}