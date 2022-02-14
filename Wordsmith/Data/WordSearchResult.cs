using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wordsmith.Data
{
    public class WordSearchResult
    {
        protected static long _nextid = 0;
        public readonly long ID;
        public string Query { get; set; }
        public bool SearchError => Exception != null;
        public Exception? Exception;

        protected List<WordEntry> _entries = new List<WordEntry>();
        public WordEntry[] Entries { get => _entries.ToArray(); }

        public void AddEntry(WordEntry entry) => _entries.Add(entry);
        public void AddEntries(IEnumerable<WordEntry> entries) => _entries.AddRange(entries);

        public WordSearchResult(string query)
        {
            Query = query;
            ID = ++_nextid;
        }
    }
}
