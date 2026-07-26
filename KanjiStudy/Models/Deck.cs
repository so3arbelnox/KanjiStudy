using System.Collections.Generic;

namespace KanjiStudy.Models
{
    public class Deck
    {
        public string Title { get; }
        public string FilePath { get; }
        public IReadOnlyList<Card> Cards { get; }

        public Deck(string title, string filePath, IReadOnlyList<Card> cards)
        {
            Title = title;
            FilePath = filePath;
            Cards = cards;
        }
    }
}
