using System.IO;
using KanjiStudy.Models;

namespace KanjiStudy.Services
{
    /// <summary>
    /// Writes a Deck back to the pipe-delimited text format DeckLoader reads. Always writes the
    /// 3-field "id|front|back" form for every card, regardless of whether the original file used
    /// 2-field lines - this round-trips cleanly either way.
    /// </summary>
    public static class DeckWriter
    {
        public static void SaveToFile(Deck deck)
        {
            var tempPath = deck.FilePath + ".tmp";

            using (var writer = new StreamWriter(tempPath, append: false))
            {
                writer.WriteLine($"Title={deck.Title}");
                writer.WriteLine("----");

                foreach (var card in deck.Cards)
                {
                    writer.WriteLine($"{card.Id}|{card.Front}|{card.Back}");
                }
            }

            File.Move(tempPath, deck.FilePath, overwrite: true);
        }
    }
}
