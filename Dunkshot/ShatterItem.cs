using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dunkshot
{
    class ShatterItem
    {
        Rectangle rect;
        Color color;

        public ShatterItem(Rectangle rect_, Color color_)
        {
            Rect = rect_;
            Color = color_;
        }

        public Color Color { get => color; set => color = value; }
        public Rectangle Rect { get => rect; set => rect = value; }
    }
}
