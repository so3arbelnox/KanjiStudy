using System;

namespace KanjiStudy.Models
{
    public class Card
    {
        public int Id { get; }
        public string Front { get; }
        public string Back { get; }

        public Card(int id, string front, string back)
        {
            Id = id;
            Front = front;
            Back = back;
        }
    }
}
