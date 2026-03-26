using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using KanjiStudy.Data;
using KanjiStudy.Factories;
using KanjiStudy.ViewModels;
using KanjiStudy.Views;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace KanjiStudy
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var collection = new ServiceCollection();
            collection.AddSingleton<MainViewModel>();
            collection.AddTransient<HomePageViewModel>();
            collection.AddTransient<DeckPageViewModel>();
            collection.AddTransient<SettingsPageViewModel>();

            collection.AddSingleton<Func<ApplicationPageNames, PageViewModel>>(x => name => name switch
            {
                ApplicationPageNames.Home => x.GetRequiredService<HomePageViewModel>(),
                ApplicationPageNames.Deck => x.GetRequiredService<DeckPageViewModel>(),
                ApplicationPageNames.Settings => x.GetRequiredService<SettingsPageViewModel>(),
                _ => throw new InvalidOperationException()
            });

            collection.AddSingleton<PageFactory>();
            
            var services = collection.BuildServiceProvider();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainView
                {
                    DataContext = services.GetRequiredService<MainViewModel>()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}