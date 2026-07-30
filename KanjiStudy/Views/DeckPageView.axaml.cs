using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using KanjiStudy.Models;
using KanjiStudy.Services;
using KanjiStudy.ViewModels;
using System;
using System.IO;
using System.Linq;

namespace KanjiStudy.Views;

public partial class DeckPageView : UserControl
{
    public DeckPageView()
    {
        InitializeComponent();
    }

    private async void BrowseForDeck_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DeckPageViewModel viewModel)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel is null)
        {
            return;
        }

        IStorageFolder? suggestedStartLocation = null;

        try
        {
            if (Directory.Exists(BundledDecksInstaller.DecksDirectory))
            {
                suggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(BundledDecksInstaller.DecksDirectory);
            }
        }
        catch (Exception)
        {
            // Some platforms (e.g. Android, for app-private paths) can't resolve a raw path to a
            // storage folder. Not fatal - the picker just opens without a suggested starting point.
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose a deck file",
            AllowMultiple = false,
            SuggestedStartLocation = suggestedStartLocation,
            FileTypeFilter = [new FilePickerFileType("Deck files") { Patterns = ["*.txt", "*.deck"] }]
        });

        var file = files.FirstOrDefault();

        if (file is not null)
        {
            viewModel.LoadDeckFromFile(file.Path.LocalPath);
        }
    }

    private void BundledDeck_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string filePath } && DataContext is DeckPageViewModel viewModel)
        {
            viewModel.LoadDeckFromFile(filePath);
        }
    }

    private void CardList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: Card card } listBox && DataContext is DeckPageViewModel viewModel)
        {
            viewModel.EditCard(card);
            listBox.SelectedItem = null;
        }
    }
}
