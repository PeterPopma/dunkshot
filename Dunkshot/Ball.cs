using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dunkshot
{
    class Ball
    {
        float x;
        float y;
        float xOrigin;
        float yOrigin;

        public float X { get => x; set => x = value; }
        public float Y { get => y; set => y = value; }
        public float XOrigin { get => xOrigin; set => xOrigin = value; }
        public float YOrigin { get => yOrigin; set => yOrigin = value; }

        public Ball(float x, float y)
        {
            this.x = x;
            this.y = y;
            this.XOrigin = x;
            this.YOrigin = y;
        }
    }
}
