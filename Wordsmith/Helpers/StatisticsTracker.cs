using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wordsmith.Helpers;
internal class StatisticsTracker
{
    private Dictionary<string, int> _word_usage_count = new();
    internal void TallyWords( string line, Word[] words)
    {
        foreach (Word word in words)
        {
            string w = line[word.WordIndex..word.WordLength];
            if ( !this._word_usage_count.ContainsKey( w.ToLower() ) )
                this._word_usage_count[w.ToLower()] = 1;
            else
                this._word_usage_count[w.ToLower()]++;
        }
    }
}
