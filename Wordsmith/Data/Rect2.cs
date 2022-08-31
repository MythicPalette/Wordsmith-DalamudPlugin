using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wordsmith.Data
{
    internal class Rect2
    {
        internal Vector2 Position;
        internal Vector2 Size;
        public Rect2(float x, float y, float w, float h)
        {
            this.Position = new(x, y);
            this.Size = new( w, h );
        }
        public Rect2( Vector2 position, Vector2 size )
        {
            this.Position = position;
            this.Size = size;
        }

        internal bool Contains( Vector2 v ) => v.X > this.Position.X && v.X < this.Position.X + this.Size.X && v.Y > this.Position.Y && v.Y < this.Position.Y + this.Size.Y;

    }
}
