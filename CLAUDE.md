# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

KanjiStudy is a cross-platform flashcard app (Avalonia UI, .NET 10, MVVM) for studying Japanese vocabulary/kanji decks. Three projects share one solution:

- `KanjiStudy/` — the shared UI + core logic (views, view models, models, services). This is where almost all work happens.
- `KanjiStudy.Desktop/` — thin desktop host (Windows/Linux/macOS), just wires up `AppBuilder` and starts the classic desktop lifetime.
- `KanjiStudy.Android/` — thin Android host, wires up `AvaloniaMainActivity`.

## Commands

```bash
# Build everything
dotnet build KanjiStudy.sln

# Run the desktop app
dotnet run --project KanjiStudy.Desktop

# Build/run without a full rebuild (faster iteration)
dotnet build KanjiStudy.Desktop && dotnet run --project KanjiStudy.Desktop --no-build
```

There is no test project in this repo currently — don't assume `dotnet test` has anything to run. Building the Android project requires the .NET Android workload/SDK; it isn't needed for UI/logic changes, which can be verified via the Desktop target.

## Architecture

### Navigation: page enum + factory + naming convention, no router

Pages are values of `ApplicationPageNames` (`Data/ApplicationPageNames.cs`). `MainViewModel` holds a single `CurrentPage` (a `PageViewModel`) and swaps it via `GoTo*()` relay commands, which call `PageFactory.GetPageViewModel(name)`.

`PageFactory` (`Factories/PageFactory.cs`) just wraps a `Func<ApplicationPageNames, PageViewModel>` that is built in `App.axaml.cs` as a switch expression over the DI container — this is where each page's view model is resolved. The actual `View` for a given `ViewModel` is never wired explicitly: `ViewLocator.cs` finds it at runtime by string-replacing `ViewModel` → `View` in the CLR full type name (e.g. `KanjiStudy.ViewModels.DeckPageViewModel` → `KanjiStudy.Views.DeckPageView`) and instantiating it via reflection. `MainView`/`MainWindow` bind `CurrentPage` through this locator to render whichever page is active.

**To add a new page**, all of these need to stay in sync:
1. New value in `Data/ApplicationPageNames.cs`.
2. New `XyzPageViewModel : PageViewModel` in `ViewModels/`, setting `PageName` in its constructor.
3. New `XyzPageView.axaml`(`.cs`) in `Views/`, named to match the view model exactly (`Xyz` prefix, `View`/`ViewModel` suffix), same relative namespace.
4. Register the view model in the DI `ServiceCollection` and add its case to the `Func<ApplicationPageNames, PageViewModel>` switch, both in `App.axaml.cs`.
5. If it should be reachable from the side menu, add a `GoTo*` command + `*IsActive` property to `MainViewModel`.

### View models

Built on CommunityToolkit.Mvvm: `ViewModelBase` is `ObservableObject`; properties use `[ObservableProperty]` (with `[NotifyPropertyChangedFor(...)]` for derived/computed properties) and commands use `[RelayCommand]`, all on `partial class` view models so the source generator can fill them in. `PageViewModel` (base for every page) just adds the observable `PageName`.

### Deck loading and format

Decks are plain pipe-delimited text files (see `Services/DeckLoader.cs`): an optional `Title=...` first line, an optional `----` separator, then one card per line as `id|front|back` or `front|back` (no id). `DeckLoader.LoadFromFile` parses a full `Deck`/`Card` (`Models/`); `DeckLoader.PeekTitle` reads just the title line cheaply for listing decks without parsing every card.

Sample decks live in `KanjiStudy/Decks/*.txt` and are embedded as `AvaloniaResource`s (`avares://KanjiStudy/Decks/...`). On startup, `Services/BundledDecksInstaller.EnsureInstalled()` (called from `App.axaml.cs`) copies any bundled decks that aren't already present into a writable per-user directory (`LocalApplicationData/KanjiStudy/Decks`) so they behave like any other file the user can browse to. `StudyPageViewModel` lists files from that directory as `BundledDecks`.

### Settings persistence

`Services/AppSettingsStore` is a static best-effort JSON store at `ApplicationData/KanjiStudy/settings.json` (currently just `LastDeckPath`). Both load and save swallow exceptions deliberately — a missing/corrupt settings file must never block startup, and a failed save must never interrupt studying. `StudyPageViewModel` uses it to auto-reload the last deck on construction and to remember the deck whenever one is loaded.

### Study flow (`ViewModels/StudyPageViewModel.cs`)

This is the core of the app and the most stateful view model. It's driven by two enums:
- `StudyStage`: `SelectDeck → Options → Studying → Complete`, plus a "round summary" interstitial shown between rounds when cards were missed.
- `CardFace`: `Front`/`Back`, toggled by reveal.

Studying works in rounds: `StartStudying` builds a shuffled pool (optionally filtered by id range, optionally capped to a count) as `_currentRound`. `RevealOrNext` flips the card, then advances. `MarkAgain` queues the current card into `_missedCards` and advances. When a round ends, if anything was missed, `ContinueToNextRound` restarts a new round scoped to just the missed cards (repeat until a round has zero misses, then `Stage` becomes `Complete`).

`ReverseCard` mode swaps which side is quizzed; `HideHiragana`/`JapaneseCharsRegex` (a compiled regex over Japanese Unicode ranges) is used to strip or extract the Japanese portion of a card's back text depending on direction, so the hint shown on reveal is only the language-relevant part.

### Platform differences

There's no platform abstraction layer — `OperatingSystem.IsAndroid()` is checked inline wherever Android needs different layout/sizing (e.g. `MainViewModel.SideMenuPadding`, `StudyPageViewModel.CardFontSize`, and a global `ButtonFontSize` resource override set in `App.axaml.cs`). Follow this pattern rather than introducing a new abstraction for platform-specific tweaks.
