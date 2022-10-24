using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wordsmith.Data
{
    internal class WebManifest
    {
        internal bool IsLoaded { get; set; } = false;
        public string[] Dictionaries = Array.Empty<string>();
    }
}
