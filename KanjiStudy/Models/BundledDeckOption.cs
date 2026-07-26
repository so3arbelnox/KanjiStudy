namespace KanjiStudy.Models
{
    public class BundledDeckOption
    {
        public string Title { get; }
        public string FilePath { get; }

        public BundledDeckOption(string title, string filePath)
        {
            Title = title;
            FilePath = filePath;
        }
    }
}
