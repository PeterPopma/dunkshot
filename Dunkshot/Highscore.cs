using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dunkshot
{
    class Highscore
    {
        DateTime date;
        string name;
        int score;
        Texture2D photo;

        public Highscore()
        {

        }

        public Highscore(DateTime date_, int score_, Texture2D texture_)
        {
            date = date_;
            Score = score_;
            photo = texture_;
        }

        public DateTime Date
        {
            get
            {
                return date;
            }

            set
            {
                date = value;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }

        public Texture2D Photo
        {
            get
            {
                return photo;
            }

            set
            {
                photo = value;
            }
        }

        public int Score { get => score; set => score = value; }
    }
}
