
namespace Wordsmith.Data
{
    public class WordSearchResult
    {
        /// <summary>
        /// Static value that holds the next available ID
        /// </summary>
        protected static long _nextid = 0;

        /// <summary>
        /// Holds the ID of this instance.
        /// </summary>
        public readonly long ID;

        /// <summary>
        /// The original string used for the search.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Returns true if an exception happened while searching for the word.
        /// </summary>
        public bool SearchError => Exception != null;

        /// <summary>
        /// Is null unless an exception occured while searching in which case this will hold that value.
        /// </summary>
        public Exception? Exception;

        /// <summary>
        /// A list of all WordEntries that hold the word variant data.
        /// </summary>
        protected List<WordEntry> _entries = new List<WordEntry>();

        /// <summary>
        /// An array of all word variant entries.
        /// </summary>
        public WordEntry[] Entries { get => _entries.ToArray(); }

        /// <summary>
        /// Adds a single entry to the collection.
        /// </summary>
        /// <param name="entry">The entry to be added.</param>
        public void AddEntry(WordEntry entry) => _entries.Add(entry);

        /// <summary>
        /// Adds a range of entries to the collection.
        /// </summary>
        /// <param name="entries">The IEnumerable of WordEntry to add.</param>
        public void AddEntries(IEnumerable<WordEntry> entries) => _entries.AddRange(entries);

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="query">The string that was searched.</param>
        public WordSearchResult(string query)
        {
            Query = query;
            ID = ++_nextid;
        }
    }
}
