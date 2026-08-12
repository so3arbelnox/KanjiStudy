using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KanjiStudy.Data;
using KanjiStudy.Models;
using KanjiStudy.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace KanjiStudy.ViewModels
{
    public enum StudyStage
    {
        SelectDeck,
        Options,
        Studying,
        Complete
    }

    public enum CardFace
    {
        Front,
        Back
    }

    public partial class StudyPageViewModel : PageViewModel
    {
        private static readonly Regex JapaneseCharsRegex = new(@"[぀-ヿ一-鿿＀-￯]+", RegexOptions.Compiled);

        private readonly OrientationService _orientationService;
        private readonly List<Card> _missedCards = new();
        private List<Card> _currentRound = new();
        private int _currentIndex;

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
        [NotifyPropertyChangedFor(nameof(IsOptionsStage))]
        [NotifyPropertyChangedFor(nameof(IsStudyingStage))]
        [NotifyPropertyChangedFor(nameof(IsCompleteStage))]
        private StudyStage _stage = StudyStage.SelectDeck;

        // Options
        [ObservableProperty]
        private bool _limitAmount;

        [ObservableProperty]
        private int _amount = 10;

        [ObservableProperty]
        private bool _useRange;

        [ObservableProperty]
        private int _rangeStart = 1;

        [ObservableProperty]
        private int _rangeEnd = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CardFontSize))]
        private bool _reverseCard;

        [ObservableProperty]
        private bool _hideHiragana;

        // Studying state
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CardFontSize))]
        private CardFace _currentFace = CardFace.Front;

        [ObservableProperty]
        private string _cardText = "";

        [ObservableProperty]
        private string? _cardHint;

        [ObservableProperty]
        private bool _showCardId;

        [ObservableProperty]
        private string _cardIdText = "";

        [ObservableProperty]
        private string _progressText = "";

        [ObservableProperty]
        private bool _canMarkAgain;

        [ObservableProperty]
        private string _actionButtonText = "Reveal";

        [ObservableProperty]
        private bool _showRoundSummary;

        [ObservableProperty]
        private string _roundSummaryText = "";

        public List<BundledDeckOption> BundledDecks { get; }
        public bool HasBundledDecks => BundledDecks.Count > 0;

        public bool HasDeck => Deck is not null;
        public string DeckTitle => Deck?.Title ?? "";
        public string DeckCardCountText => Deck is null ? "" : $"{Deck.Cards.Count} cards";

        public bool HasLoadError => !string.IsNullOrEmpty(LoadError);

        public bool IsSelectDeckStage => Stage == StudyStage.SelectDeck;
        public bool IsOptionsStage => Stage == StudyStage.Options;
        public bool IsStudyingStage => Stage == StudyStage.Studying;
        public bool IsCompleteStage => Stage == StudyStage.Complete;

        // The kanji side is a handful of characters and gets the big font; the reading/translation
        // side can run long, so it gets a smaller one. A phone's screen is much smaller than the
        // desktop window this was tuned for, so both sizes scale down there - and in portrait,
        // width (not height) is the constraint, so the front font backs off a bit further too.
        public double CardFontSize => (CurrentFace == CardFace.Front) ^ ReverseCard
            ? FrontCardFontSize
            : BackCardFontSize;

        private double FrontCardFontSize => IsPhone
            ? (_orientationService.IsPortrait ? 50 : 60)
            : (_orientationService.IsPortrait ? 70 : 90);

        private double BackCardFontSize => IsPhone
            ? (_orientationService.IsPortrait ? 20 : 22)
            : (_orientationService.IsPortrait ? 26 : 30);

        // A phone's screen leaves very little room, so the select-deck and options screens need
        // to run noticeably more compact there to avoid scrolling.
        public bool IsCompactLayout => IsPhone;
        public double OptionsTitleFontSize => IsPhone ? 16 : 20;
        public double OptionsSpacing => IsPhone ? 4 : 8;

        private bool IsPhone => _orientationService.DeviceClass == DeviceClass.Phone;

        public StudyPageViewModel() : this(new OrientationService())
        {
        }

        public StudyPageViewModel(OrientationService orientationService)
        {
            _orientationService = orientationService;
            PageName = Data.ApplicationPageNames.Study;
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

                if (deck.Cards.Count == 0)
                {
                    LoadError = "No cards were found in that file.";
                    return;
                }

                Deck = deck;
                LoadError = null;
                RangeStart = deck.Cards.Min(c => c.Id);
                RangeEnd = deck.Cards.Max(c => c.Id);
                Amount = Math.Min(10, deck.Cards.Count);
                Stage = StudyStage.Options;

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
            Stage = StudyStage.SelectDeck;
        }

        [RelayCommand]
        private void StartStudying()
        {
            if (Deck is null)
            {
                return;
            }

            IEnumerable<Card> pool = Deck.Cards;

            if (UseRange)
            {
                pool = pool.Where(c => c.Id >= RangeStart && c.Id <= RangeEnd);
            }

            var cards = pool.ToList();
            Shuffle(cards);

            if (LimitAmount && Amount < cards.Count)
            {
                cards = cards.Take(Amount).ToList();
            }

            if (cards.Count == 0)
            {
                LoadError = "No cards match those options.";
                return;
            }

            LoadError = null;
            _missedCards.Clear();
            _currentRound = cards;
            _currentIndex = 0;
            ShowRoundSummary = false;
            Stage = StudyStage.Studying;

            ShowCardFront();
        }

        [RelayCommand]
        private void RevealOrNext()
        {
            if (CurrentFace == CardFace.Front)
            {
                ShowCardBack();
            }
            else
            {
                Advance();
            }
        }

        [RelayCommand]
        private void MarkAgain()
        {
            _missedCards.Add(_currentRound[_currentIndex]);
            Advance();
        }

        [RelayCommand]
        private void ContinueToNextRound()
        {
            _currentRound = new List<Card>(_missedCards);
            _missedCards.Clear();
            Shuffle(_currentRound);
            _currentIndex = 0;
            ShowRoundSummary = false;

            ShowCardFront();
        }

        [RelayCommand]
        private void RestartStudying()
        {
            Stage = StudyStage.Options;
        }

        private void Advance()
        {
            _currentIndex++;

            if (_currentIndex >= _currentRound.Count)
            {
                var total = _currentRound.Count;
                var missed = _missedCards.Count;
                var correct = total - missed;
                var score = total == 0 ? 100 : (int)Math.Round(correct * 100.0 / total);

                if (missed > 0)
                {
                    RoundSummaryText = $"Correct: {correct}   Missed: {missed}   Score: {score}%";
                    ShowRoundSummary = true;
                }
                else
                {
                    Stage = StudyStage.Complete;
                }

                return;
            }

            ShowCardFront();
        }

        private void ShowCardFront()
        {
            var card = _currentRound[_currentIndex];

            CurrentFace = CardFace.Front;
            ActionButtonText = "Reveal";
            CanMarkAgain = false;
            ShowCardId = false;
            CardHint = null;
            CardText = ReverseCard ? (HideHiragana ? StripJapanese(card.Back) : card.Back) : card.Front;
            ProgressText = $"{_currentIndex + 1} / {_currentRound.Count}";
        }

        private void ShowCardBack()
        {
            var card = _currentRound[_currentIndex];

            CurrentFace = CardFace.Back;
            ActionButtonText = "OK";
            CanMarkAgain = true;
            ShowCardId = true;
            CardIdText = $"Card: {card.Id}";

            if (ReverseCard)
            {
                CardText = card.Front;
                CardHint = ExtractJapanese(card.Back);
            }
            else
            {
                CardText = card.Back;
                CardHint = null;
            }
        }

        private static string StripJapanese(string text) => JapaneseCharsRegex.Replace(text, "").Trim();

        private static string ExtractJapanese(string text) => JapaneseCharsRegex.Match(text).Value;

        private static void Shuffle(List<Card> cards) => Random.Shared.Shuffle(CollectionsMarshal.AsSpan(cards));
    }
}
