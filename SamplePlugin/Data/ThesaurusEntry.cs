using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wordsmith.Data
{
    public class ThesaurusEntry : WordEntry
    {
        // Synonyms
        protected List<string> _syn = new List<string>();
        public string[] Synonyms { get => _syn.ToArray(); }
        public void AddSynonym(string word) => _syn.Add(word);
        public void AddSynonyms(IEnumerable<string> words) => _syn.AddRange(words);
        public string SynonymString => string.Join(", ", Synonyms ?? new string[] { });

        // Related words
        protected List<string> _rel = new List<string>();
        public string[] Related { get => _rel.ToArray(); }
        public void AddRelatedWord(string word) => _rel.Add(word);
        public void AddRelatedWords(IEnumerable<string> words) => _rel.AddRange(words);
        public string RelatedString => string.Join(", ", Related ?? new string[] { });

        // Near antonyms
        protected List<string> _near = new List<string>();
        public string[] NearAntonyms { get => _near.ToArray(); }
        public void AddNearAntonym(string word) => _near.Add(word);
        public void AddNearAntonyms(IEnumerable<string> words) => _near.AddRange(words);
        public string NearAntonymString => string.Join(", ", NearAntonyms ?? new string[] { });

        // Antonyms
        protected List<string> _ant = new List<string>();
        public string[] Antonyms { get => _ant.ToArray(); }
        public void AddAntonym(string word) => _ant.Add(word);
        public void AddAntonyms(IEnumerable<string> words) => _ant.AddRange(words);
        public string AntonymString => string.Join(", ", Antonyms ?? new string[] { });
    }
}
