using Avalonia.Platform;
using System;
using System.IO;

namespace KanjiStudy.Services
{
    /// <summary>
    /// Copies the sample decks embedded in the app (Desktop and Android alike) into a writable,
    /// per-user directory so they show up as real files when browsing for a deck to study.
    /// </summary>
    public static class BundledDecksInstaller
    {
        private static readonly Uri DecksResourceFolder = new("avares://KanjiStudy/Decks/");

        public static string DecksDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KanjiStudy",
            "Decks");

        public static void EnsureInstalled()
        {
            try
            {
                Directory.CreateDirectory(DecksDirectory);

                foreach (var resourceUri in AssetLoader.GetAssets(DecksResourceFolder, null))
                {
                    var destinationPath = Path.Combine(DecksDirectory, Path.GetFileName(resourceUri.AbsolutePath));

                    if (File.Exists(destinationPath))
                    {
                        continue;
                    }

                    using var resourceStream = AssetLoader.Open(resourceUri);
                    using var destinationStream = File.Create(destinationPath);
                    resourceStream.CopyTo(destinationStream);
                }
            }
            catch
            {
                // Best-effort: if the bundled decks can't be installed, the user can still browse for their own.
            }
        }
    }
}
