using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using KanjiStudy.Data;
using KanjiStudy.Factories;
using KanjiStudy.Services;
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
            BundledDecksInstaller.EnsureInstalled();

            var buttonFontSize = 20.0;

            if (OperatingSystem.IsAndroid())
            {
                buttonFontSize = 14.0;
                Resources["ButtonFontSize"] = buttonFontSize;
            }

            // The portrait bottom nav bar's buttons are icon-only and meant to be a bit more
            // prominent/tappable than a regular Button - see MainView.axaml.
            Resources["PortraitNavButtonFontSize"] = buttonFontSize * 1.5;

            BuildAndShowRootView();

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Builds a fresh DI container and root view (MainWindow's content on desktop, or
        /// MainView directly on Android). Called at startup, and again whenever
        /// OrientationService.RestartRequested fires, so every view model - not just the ones
        /// that read orientation directly - starts clean with the new layout rather than trying
        /// to re-flow live.
        /// </summary>
        private void BuildAndShowRootView()
        {
            var collection = new ServiceCollection();
            collection.AddSingleton<OrientationService>();
            collection.AddSingleton<MainViewModel>();
            collection.AddTransient<HomePageViewModel>();
            collection.AddTransient<DeckPageViewModel>();
            collection.AddTransient<StudyPageViewModel>();
            collection.AddTransient<SettingsPageViewModel>();

            collection.AddSingleton<Func<ApplicationPageNames, PageViewModel>>(x => name => name switch
            {
                ApplicationPageNames.Home => x.GetRequiredService<HomePageViewModel>(),
                ApplicationPageNames.Deck => x.GetRequiredService<DeckPageViewModel>(),
                ApplicationPageNames.Study => x.GetRequiredService<StudyPageViewModel>(),
                ApplicationPageNames.Settings => x.GetRequiredService<SettingsPageViewModel>(),
                _ => throw new InvalidOperationException()
            });

            collection.AddSingleton<PageFactory>();

            var services = collection.BuildServiceProvider();

            var orientationService = services.GetRequiredService<OrientationService>();
            orientationService.RestartRequested += (_, _) => Dispatcher.UIThread.Post(BuildAndShowRootView);

            // Re-applied on every (re)build, both at cold start and after a restart, so this is
            // the single place that pushes the current orientation out to the platform surface.
            OrientationService.PlatformApply?.Invoke(orientationService.Orientation);

            var mainViewModel = services.GetRequiredService<MainViewModel>();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var (width, height) = WindowSizeFor(orientationService.Orientation);

                if (desktop.MainWindow is MainWindow existingWindow)
                {
                    // Resize and swap content on the existing Window rather than replacing
                    // desktop.MainWindow outright - reassigning it mid-run risks tripping the
                    // default ShutdownMode.OnMainWindowClose behavior.
                    existingWindow.Content = new MainView { DataContext = mainViewModel };
                    existingWindow.Width = width;
                    existingWindow.Height = height;
                }
                else
                {
                    desktop.MainWindow = new MainWindow
                    {
                        Width = width,
                        Height = height,
                        DataContext = mainViewModel
                    };
                }
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            {
                singleView.MainView = new MainView
                {
                    DataContext = mainViewModel
                };
            }
        }

        private static (double Width, double Height) WindowSizeFor(AppOrientation orientation) =>
            orientation == AppOrientation.Portrait ? (800, 1400) : (1400, 800);
    }
}