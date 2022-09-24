using System.Text.RegularExpressions;

namespace Wordsmith.Helpers
{
    internal class Console
    {
        internal static bool ProcessCommand(string s)
        {
            Match m = Regex.Match(s, @"(?<=^dev(?:\s+))(?<option>\S+)(?:=)(?<value>\S+)$");
            if ( !m.Success )
                return false;

            // Enable debug
            switch (m.Groups["option"].Value.ToLower())
            {
                case "dbg":
                    Wordsmith.Configuration.EnableDebug = m.Groups["value"].Value == "on";
                    Wordsmith.Configuration.Save();
                    break;

                case "nquery":
                    Wordsmith.Configuration.NumericQuery = m.Groups["value"].Value;
                    Wordsmith.Configuration.Save();
                    break;

                case "dquery":
                    Wordsmith.Configuration.DateQuery = m.Groups["value"].Value;
                    Wordsmith.Configuration.Save();
                    break;

                case "wquery":
                    Wordsmith.Configuration.WordQuery = m.Groups["value"].Value;
                    Wordsmith.Configuration.Save();
                    break;

                case "tquery":
                    Wordsmith.Configuration.TimeQuery = m.Groups["value"].Value;
                    Wordsmith.Configuration.Save();
                    break;

                case "config":
                    if ( m.Groups["value"].Value.ToLower() == "reset" )
                        Wordsmith.Configuration.ResetToDefault();
                    break;
            }
            return true;
        }
    }
}
