using Avalonia.Controls;
using System;
using System.Diagnostics;

namespace KanjiStudy
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Home_Button_Clicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Trace.WriteLine("Test");
        }
    }
}