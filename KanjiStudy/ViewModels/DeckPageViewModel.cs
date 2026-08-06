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
        [NotifyPropertyChangedFor(nameof(IsDuplicateFront))]
        private string _editFront = "";

        [ObservableProperty]
        private string _editBack = "";

        // Optional - blank means "no card number" (persisted as id 0), same as a bare 2-field
        // "front|back" line. Card numbers are what Study's id-range filter operates on.
        [ObservableProperty]
        private string _editId = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasEditError))]
        private string? _editError;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasAddedMessage))]
        private string? _addedMessage;

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
        public bool HasAddedMessage => !string.IsNullOrEmpty(AddedMessage);

        public bool IsSelectDeckStage => Stage == DeckStage.SelectDeck;
        public bool IsCardListStage => Stage == DeckStage.CardList;
        public bool IsEditCardStage => Stage == DeckStage.EditCard;

        public bool CanDeleteCard => !IsNewCard;
        public string EditScreenTitle => IsNewCard ? "New Card" : "Edit Card";

        // Warns (without blocking save) when another card in the deck already has this front text -
        // legitimate decks can have intentional duplicates (e.g. homographs), so this is advisory only.
        public bool IsDuplicateFront => Deck is not null
            && !string.IsNullOrWhiteSpace(EditFront)
            && Deck.Cards.Any(card => card != _editingCard
                && string.Equals(card.Front.Trim(), EditFront.Trim(), StringComparison.OrdinalIgnoreCase));

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
            EditId = "";
            EditError = null;
            AddedMessage = null;
            Stage = DeckStage.EditCard;
        }

        public void EditCard(Card card)
        {
            _editingCard = card;
            IsNewCard = false;
            EditFront = card.Front;
            EditBack = card.Back;
            EditId = card.Id == 0 ? "" : card.Id.ToString();
            EditError = null;
            AddedMessage = null;
            Stage = DeckStage.EditCard;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            _editingCard = null;
            EditError = null;
            AddedMessage = null;
            Stage = DeckStage.CardList;
        }

        // Dismiss the "card added" confirmation as soon as the user starts typing the next
        // entry, rather than leaving it up until the following save.
        partial void OnEditFrontChanged(string value) => AddedMessage = null;
        partial void OnEditBackChanged(string value) => AddedMessage = null;

        [RelayCommand]
        private void SaveCard()
        {
            if (Deck is null)
            {
                return;
            }

            var front = EditFront.Trim();
            var back = EditBack.Trim();
            var idText = EditId.Trim();

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

            var id = 0;

            if (!string.IsNullOrEmpty(idText) && !int.TryParse(idText, out id))
            {
                EditError = "Card # must be a whole number.";
                return;
            }

            var cards = new List<Card>(Deck.Cards);
            var savedCard = new Card(id, front, back);
            var wasNewCard = _editingCard is null;

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

            if (!Persist(updatedDeck))
            {
                // The in-memory Deck was already updated above - only the file write failed.
                // Stay on the edit screen with this card so the user sees the failure and can
                // retry, instead of silently losing the change.
                _editingCard = savedCard;
                return;
            }

            if (wasNewCard)
            {
                // Keep the add-card screen open for rapid entry of several cards in a row instead
                // of bouncing back to the list after every single one.
                _editingCard = null;
                EditFront = "";
                EditBack = "";
                EditId = "";
                AddedMessage = $"Added \"{front}\".";
            }
            else
            {
                _editingCard = null;
                IsNewCard = false;
                Stage = DeckStage.CardList;
            }
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

            if (Persist(updatedDeck))
            {
                _editingCard = null;
                Stage = DeckStage.CardList;
            }
        }

        private bool Persist(Deck deck)
        {
            try
            {
                DeckWriter.SaveToFile(deck);
                EditError = null;
                return true;
            }
            catch (Exception ex)
            {
                // A failed save must never look silent - surface it and let the caller decide
                // how to leave the edit screen state.
                EditError = $"Saved in the app, but couldn't write the file: {ex.Message}";
                return false;
            }
        }

        private static bool ContainsInvalidChars(string s) => s.Contains('|') || s.Contains('\n') || s.Contains('\r');
    }
}
