using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Wordsmith.Helpers;
internal class StatisticsTracker
{
    private Dictionary<string, int> _word_usage_count = [];

    internal void TallyWord( string line, Word word)
    {
        try
        {
            if ( word.WordLength == 0 || word.WordEndIndex - word.WordIndex < 1 )
            {
                Wordsmith.PluginLog.Verbose( $"Word has a length of zero. ([{word.StartIndex}..{word.EndIndex}] {line[word.StartIndex..word.EndIndex]}\")" );
                return;
            }

            string w = line[word.WordIndex..word.WordEndIndex];
            if ( !this._word_usage_count.ContainsKey( w.ToLower() ) )
                this._word_usage_count[w.ToLower()] = 1;
            else
                this._word_usage_count[w.ToLower()]++;
        }
        catch (Exception)
        {
            Wordsmith.PluginLog.Error( $"Failed to Tally Word \"[{word.StartIndex}..{word.EndIndex}]{line[word.StartIndex..word.EndIndex]}\"" );
            return;
        }
    }

    internal void TallyWords( string line, IEnumerable<Word> words)
    {
        foreach ( Word word in words )
            TallyWord( line, word );
    }

    internal void AddChunk( TextChunk textChunk )
    {
        // Get all of the words in the chunk.
        List<Word> words = textChunk.Text.Words();
        TallyWords( textChunk.Text, words );
    }

    internal List<KeyValuePair<string, int>> ListWordsByCount(bool descending)
    {
        List<KeyValuePair<string, int>> results = [];
        results.AddRange( [..this._word_usage_count] );

        return descending
            ? [.. results.OrderByDescending( f => f.Value )]
            : [.. results.OrderBy( f => f.Value )];
    }

    internal List<KeyValuePair<string,int>> ListWordsByWord(bool descending)
    {
        List<KeyValuePair<string, int>> results = [.. this._word_usage_count.ToArray()];

        return descending
            ? [..results.OrderByDescending( f => f.Key )]
            : [..results.OrderBy( f => f.Key )];
    }

    internal void Clear() => this._word_usage_count.Clear();
}
