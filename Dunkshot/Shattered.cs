using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dunkshot
{

    class Shattered
    {
        static public List<ShatteredPart> CreateShatteredParts(GraphicsDevice GraphicsDevice, Texture2D textureOriginalImage, Texture2D textureShattered)
        {
            int width = textureShattered.Width;
            int height = textureShattered.Height;
            Color[] dataMask = new Color[width * height];
            textureShattered.GetData<Color>(dataMask, 0, dataMask.Length);
            Color[] dataOriginal = new Color[textureOriginalImage.Width * textureOriginalImage.Height];
            textureOriginalImage.GetData<Color>(dataOriginal, 0, dataOriginal.Length);
            List<ShatteredPart> shatteredParts = new List<ShatteredPart>();
            List<ShatterItem> shatteredItems = new List<ShatterItem>();

            // walk through shatter image
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = dataMask[x + y * width];
                    // find color of pixel in list of shattered items
                    ShatterItem item = shatteredItems.Find(o => o.Color.Equals(pixel));
                    // if new color, add Rect to list
                    if (item == null)
                    {
                        shatteredItems.Add(new ShatterItem(new Rectangle(x, y, 1, 1), pixel));
                    }
                    else
                    {
                        if (x < item.Rect.X)
                        {
                            item.Rect = new Rectangle(x, item.Rect.Y, item.Rect.Width+(item.Rect.X-x), item.Rect.Height);
                        }
                        if (y < item.Rect.Top)
                        {
                            item.Rect = new Rectangle(item.Rect.X, y, item.Rect.Width, item.Rect.Height+(item.Rect.Top-y));
                        }
                        if (x >= item.Rect.Right)
                        {
                            item.Rect = new Rectangle(item.Rect.X, item.Rect.Y, x - item.Rect.X + 1, item.Rect.Height);
                        }
                        if (y >= item.Rect.Bottom)
                        {
                            item.Rect = new Rectangle(item.Rect.X, item.Rect.Y, item.Rect.Width, y - item.Rect.Y + 1);
                        }
                    }
                }
            }
            // for each item: copy data from image where data from shatter image is same color and set offset x and y to values of rect
            foreach(ShatterItem item in shatteredItems)
            {
                ShatteredPart shatteredPart = new ShatteredPart();
                Color[] dataShatteredPart = new Color[item.Rect.Width*item.Rect.Height];
                for (int yy = 0; yy < item.Rect.Height; yy++)
                {
                    for (int xx = 0; xx < item.Rect.Width; xx++)
                    {
                        Color maskPixel = dataMask[(yy+item.Rect.Top)* textureShattered.Width + xx+item.Rect.Left];
                        if(maskPixel.Equals(item.Color))
                        {
                            dataShatteredPart[xx + yy * item.Rect.Width] = dataOriginal[(yy + item.Rect.Top) * textureOriginalImage.Width + xx + item.Rect.Left];
                        }
                        else
                        {
                            dataShatteredPart[xx + yy * item.Rect.Width] = new Color(0, 0, 0, 0);
                        }
                    }
                }
                Texture2D texture = new Texture2D(GraphicsDevice, item.Rect.Width, item.Rect.Height);
                texture.SetData(dataShatteredPart);
                shatteredPart.Texture = texture;
                shatteredPart.XOffset = item.Rect.X;
                shatteredPart.YOffset = item.Rect.Y;
                shatteredParts.Add(shatteredPart);
            }

            return shatteredParts;
        }
    }
}
