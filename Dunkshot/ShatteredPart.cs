using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dunkshot
{ 

    class ShatteredPart
    {
        Texture2D texture;
        int xOffset;
        int yOffset;
        float x;
        float y;
        float xSpeed;
        float ySpeed;
        float angle;
        float rotationSpeed;

        public ShatteredPart()
        {
        }

        public Texture2D Texture { get => texture; set => texture = value; }
        public int XOffset { get => xOffset; set => xOffset = value; }
        public float X { get => x; set => x = value; }
        public float Y { get => y; set => y = value; }
        public float XSpeed { get => xSpeed; set => xSpeed = value; }
        public float YSpeed { get => ySpeed; set => ySpeed = value; }
        public int YOffset { get => yOffset; set => yOffset = value; }
        public float Angle { get => angle; set => angle = value; }
        public float RotationSpeed { get => rotationSpeed; set => rotationSpeed = value; }
    }
}
