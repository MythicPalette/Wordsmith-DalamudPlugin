using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wordsmith.Gui
{
    public abstract class Window
    {
        protected bool _visible;
        public bool Visible { get => _visible; set => _visible = value; }

        protected Plugin Plugin;

        public Window(Plugin plugin) { this.Plugin = plugin; }
        public void Draw()
        {
            if (Visible)
                DrawUI();
        }

        protected abstract void DrawUI();
    }
}
