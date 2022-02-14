using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wordsmith.Data
{
    public class WordEntry
    {
        private static long _nextid;
        public readonly long ID;

        public string Word { get; set; } = "";

        protected string _type = "";
        public string Type
        { 
            get => _type;
            set
            {
                if (value.Length == 1)
                    _type = char.ToUpper(value[0]).ToString();
                else
                    _type = char.ToUpper(value[0]).ToString() + value.Substring(1);                
            }
        }
        public string Definition { get; set; } = "";
        public WordEntry()
        {
            ID = ++_nextid;
        }
    }
}
