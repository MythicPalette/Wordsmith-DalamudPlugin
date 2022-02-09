using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wordsmith.Helpers
{
    public static class StringExtensions
    {
        public static string CaplitalizeFirst(this string s)
        {
            if (s == null)
                return s;

            if (s.Length == 1)
                return char.ToUpper(s[0]).ToString();
            else if (s.Length > 1)
                return char.ToUpper(s[0]).ToString() + s.Substring(1);
            return s;
        }
    }
}
