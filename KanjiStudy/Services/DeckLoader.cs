using System;
using System.Collections.Generic;
using System.IO;
using KanjiStudy.Models;

namespace KanjiStudy.Services
{
    /// <summary>
    /// Reads decks from the plain-text format used by the original KanjiStudy Windows app:
    /// an optional "Title=..." line, an optional "----" separator, then one card per line as
    /// either "id|front|back" or "front|back".
    /// </summary>
    public static class DeckLoader
    {
        public static Deck LoadFromFile(string filePath)
        {
            var title = Path.GetFileNameWithoutExtension(filePath);
            var cards = new List<Card>();

            foreach (var line in File.ReadLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.StartsWith("Title=", StringComparison.Ordinal))
                {
                    title = line.Substring("Title=".Length);
                    continue;
                }

                if (line.StartsWith("----", StringComparison.Ordinal))
                {
                    continue;
                }

                var fields = line.Split('|');

                if (fields.Length == 2)
                {
                    cards.Add(new Card(0, fields[0], fields[1]));
                }
                else if (fields.Length == 3 && int.TryParse(fields[0], out var id))
                {
                    cards.Add(new Card(id, fields[1], fields[2]));
                }
            }

            return new Deck(title, filePath, cards);
        }

        /// <summary>
        /// Reads just enough of the file to get its display title, without parsing every card.
        /// </summary>
        public static string PeekTitle(string filePath)
        {
            foreach (var line in File.ReadLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.StartsWith("Title=", StringComparison.Ordinal))
                {
                    return line.Substring("Title=".Length);
                }

                break;
            }

            return Path.GetFileNameWithoutExtension(filePath);
        }
    }
}
