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
        int gameTimeMilliSeconds;
        Texture2D photo;

        public Highscore()
        {

        }

        public Highscore(DateTime date_, int time_, Texture2D texture_)
        {
            date = date_;
            gameTimeMilliSeconds = time_;
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

        public int GameTimeMilliSeconds
        {
            get
            {
                return gameTimeMilliSeconds;
            }

            set
            {
                gameTimeMilliSeconds = value;
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
    }
}
