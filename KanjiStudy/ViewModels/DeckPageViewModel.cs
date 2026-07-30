using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KanjiStudy.Models;
using KanjiStudy.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KanjiStudy.ViewModels
{
    public enum DeckStage
    {
        SelectDeck,
        CardList,
        EditCard
    }

    public partial class DeckPageViewModel : PageViewModel
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasDeck))]
        [NotifyPropertyChangedFor(nameof(DeckTitle))]
        [NotifyPropertyChangedFor(nameof(DeckCardCountText))]
        private Deck? _deck;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasLoadError))]
        private string? _loadError;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSelectDeckStage))]
        [NotifyPropertyChangedFor(nameof(IsCardListStage))]
        [NotifyPropertyChangedFor(nameof(IsEditCardStage))]
        private DeckStage _stage = DeckStage.SelectDeck;

        // Edit form state
        [ObservableProperty]
        private string _editFront = "";

        [ObservableProperty]
        private string _editBack = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasEditError))]
        private string? _editError;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanDeleteCard))]
        [NotifyPropertyChangedFor(nameof(EditScreenTitle))]
        private bool _isNewCard;

        private Card? _editingCard;

        public List<BundledDeckOption> BundledDecks { get; }
        public bool HasBundledDecks => BundledDecks.Count > 0;

        public bool HasDeck => Deck is not null;
        public string DeckTitle => Deck?.Title ?? "";
        public string DeckCardCountText => Deck is null ? "" : $"{Deck.Cards.Count} cards";

        public bool HasLoadError => !string.IsNullOrEmpty(LoadError);
        public bool HasEditError => !string.IsNullOrEmpty(EditError);

        public bool IsSelectDeckStage => Stage == DeckStage.SelectDeck;
        public bool IsCardListStage => Stage == DeckStage.CardList;
        public bool IsEditCardStage => Stage == DeckStage.EditCard;

        public bool CanDeleteCard => !IsNewCard;
        public string EditScreenTitle => IsNewCard ? "New Card" : "Edit Card";

        // Android is locked to landscape, which leaves very little vertical room, so the
        // select-deck and edit-card forms need to run more compact there to avoid scrolling.
        public bool IsCompactLayout => OperatingSystem.IsAndroid();

        public DeckPageViewModel()
        {
            PageName = Data.ApplicationPageNames.Deck;
            BundledDecks = LoadBundledDeckOptions();
            TryAutoLoadLastDeck();
        }

        private static List<BundledDeckOption> LoadBundledDeckOptions()
        {
            if (!Directory.Exists(BundledDecksInstaller.DecksDirectory))
            {
                return new List<BundledDeckOption>();
            }

            return Directory.GetFiles(BundledDecksInstaller.DecksDirectory, "*.txt")
                .Select(path => new BundledDeckOption(DeckLoader.PeekTitle(path), path))
                .OrderBy(option => option.Title)
                .ToList();
        }

        private void TryAutoLoadLastDeck()
        {
            var lastDeckPath = AppSettingsStore.Load().LastDeckPath;

            if (!string.IsNullOrEmpty(lastDeckPath) && File.Exists(lastDeckPath))
            {
                LoadDeckFromFile(lastDeckPath);
            }
        }

        public void LoadDeckFromFile(string filePath)
        {
            try
            {
                var deck = DeckLoader.LoadFromFile(filePath);

                // Unlike Study, an empty deck is fine here - this page's job includes populating
                // a deck's first cards, so it should land on the card list, not an error.
                Deck = deck;
                LoadError = null;
                Stage = DeckStage.CardList;

                AppSettingsStore.Save(new AppSettings { LastDeckPath = filePath });
            }
            catch (Exception ex)
            {
                LoadError = $"Failed to load deck: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ChangeDeck()
        {
            Deck = null;
            LoadError = null;
            Stage = DeckStage.SelectDeck;
        }

        [RelayCommand]
        private void AddCard()
        {
            _editingCard = null;
            IsNewCard = true;
            EditFront = "";
            EditBack = "";
            EditError = null;
            Stage = DeckStage.EditCard;
        }

        public void EditCard(Card card)
        {
            _editingCard = card;
            IsNewCard = false;
            EditFront = card.Front;
            EditBack = card.Back;
            EditError = null;
            Stage = DeckStage.EditCard;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            _editingCard = null;
            EditError = null;
            Stage = DeckStage.CardList;
        }

        [RelayCommand]
        private void SaveCard()
        {
            if (Deck is null)
            {
                return;
            }

            var front = EditFront.Trim();
            var back = EditBack.Trim();

            if (string.IsNullOrEmpty(front) || string.IsNullOrEmpty(back))
            {
                EditError = "Front and back are both required.";
                return;
            }

            if (ContainsInvalidChars(front) || ContainsInvalidChars(back))
            {
                EditError = "Front and back can't contain \"|\" or line breaks.";
                return;
            }

            var cards = new List<Card>(Deck.Cards);
            var savedCard = new Card(_editingCard?.Id ?? 0, front, back);

            if (_editingCard is null)
            {
                cards.Add(savedCard);
            }
            else
            {
                var index = cards.IndexOf(_editingCard);

                if (index >= 0)
                {
                    cards[index] = savedCard;
                }
                else
                {
                    cards.Add(savedCard);
                }
            }

            var updatedDeck = new Deck(Deck.Title, Deck.FilePath, cards);
            Deck = updatedDeck;
            _editingCard = savedCard;
            IsNewCard = false;

            Persist(updatedDeck);
        }

        [RelayCommand]
        private void DeleteCard()
        {
            if (Deck is null || _editingCard is null)
            {
                return;
            }

            var cards = new List<Card>(Deck.Cards);
            cards.Remove(_editingCard);

            var updatedDeck = new Deck(Deck.Title, Deck.FilePath, cards);
            Deck = updatedDeck;

            Persist(updatedDeck);
        }

        private void Persist(Deck deck)
        {
            try
            {
                DeckWriter.SaveToFile(deck);
                EditError = null;
                _editingCard = null;
                Stage = DeckStage.CardList;
            }
            catch (Exception ex)
            {
                // The in-memory Deck was already updated above - only the file write failed.
                // Stay on the edit screen so the user sees the failure and can retry, instead of
                // silently losing the change the way AppSettingsStore swallows save failures.
                EditError = $"Saved in the app, but couldn't write the file: {ex.Message}";
            }
        }

        private static bool ContainsInvalidChars(string s) => s.Contains('|') || s.Contains('\n') || s.Contains('\r');
    }
}
