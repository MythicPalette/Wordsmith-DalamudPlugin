
namespace Wordsmith.Data
{
    public class WordEntry
    {
        private static long _nextid;
        public readonly long ID;

        /// <summary>
        /// The word as a string
        /// </summary>
        public string Word { get; set; } = "";

        /// <summary>
        /// Holds the type of the word (i.e. Noun)
        /// </summary>
        protected string _type = "";

        /// <summary>
        /// Gets or sets the type of the word (i.e. Noun)
        /// </summary>
        public string Type
        { 
            get => _type;
            set => _type = value.CaplitalizeFirst();
        }

        /// <summary>
        /// The definition of this variant of the word.
        /// One word can have multiple variants
        /// </summary>
        public string Definition { get; set; } = "";

        /// <summary>
        /// Default constructor that just assigns an ID.
        /// </summary>
        public WordEntry() { ID = ++_nextid; }
    }
}
