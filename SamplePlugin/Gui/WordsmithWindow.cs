using System;
using Dalamud.Interface.Windowing;

namespace Wordsmith.Gui
{
    public abstract class WordsmithWindow : Window
    {
        protected bool _disposed = false;
        public virtual bool Disposed => _disposed;

        public WordsmithWindow(string name) : base(name) { }

        public virtual void Dispose() => _disposed = true;
    }
}
