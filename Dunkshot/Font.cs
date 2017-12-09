using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dunkshot
{
    class Font
    {
        Rectangle[] fontRect;
        int[] fontYOffset;
        int Spacing = -17;
        int SpacingExtraNumbers = 14;
        Texture2D textureFont;
        int[] fontExtraYOffset = { 0,9,8,9,9,5,5 };       // This needs to be set manually, because we don't know the line heights
        enum DetectionPhase { FindObject, FindEmptyLine, VerticalScanning };
        DetectionPhase detectionPhase;
        int CharacterNumber;
        Color[] bitmapData;
        int FirstNonemptyLine;
        int LastNonemptyLine;
        int LineNumber;
        float wavePhase;

        private bool isBackgroundPixel(int index)
        {
            return (bitmapData[index].A == 0);
//            return (bitmapData[index].R == 0 && bitmapData[index].G == 0 && bitmapData[index].B == 0);
        }

        public void Adjust(int[] fontExtraYOffset_, int Spacing_, int SpacingExtraNumbers_)
        {
            fontExtraYOffset = fontExtraYOffset_;
            Spacing = Spacing_;
            SpacingExtraNumbers = SpacingExtraNumbers_;
        }

        // Capture all characters of the font texture to seperate rectangles that can be used to  display text
        //
        // Pre-conditions:
        //
        // - Must be complete ASCII set from ! to ~
        // - There must be an empty horizontal line between character lines
        // - There must be an empty vertical line between characters
        // - All characters must consist of one connecting piece, except :;= they must consist of 2 pieces
        // - Vertical offset is based on the highest character of a line, so you might need to adjust some characters a little bit using Adjust() method (before calling Initialize)
        //
        public void Initialize(Texture2D textureFont_)
        {
            fontRect = new Rectangle[255];
            fontYOffset = new int[255];
            textureFont = textureFont_;
            bitmapData = new Color[textureFont.Width * textureFont.Height];
            textureFont.GetData<Color>(bitmapData, 0, bitmapData.Length);
            int x = 0;
            int y = 0;
            CharacterNumber = 0;
            FirstNonemptyLine = -1;
            LastNonemptyLine = -1;
            LineNumber = 0;
            detectionPhase = DetectionPhase.FindObject;
            while (y < textureFont.Height)
            {
                if(detectionPhase.Equals(DetectionPhase.FindObject))
                {
                    if (!isBackgroundPixel(x + y * textureFont.Width))     // found object
                    {
                        FirstNonemptyLine = y;
                        detectionPhase = DetectionPhase.FindEmptyLine;
                    }
                    x++;
                    if (x >= textureFont.Width)
                    {
                        x = 0;
                        y++;
                    }
                }
                if (detectionPhase.Equals(DetectionPhase.FindEmptyLine))
                {
                    int xx;
                    int yy = FirstNonemptyLine;
                    {
                        bool isEmptyLine = false;
                        while (!isEmptyLine)
                        {
                            xx = 0;
                            yy++;
                            isEmptyLine = true;
                            while (isEmptyLine && xx <= textureFont.Width)
                            {
                                if (!isBackgroundPixel(xx + yy * textureFont.Width))
                                {
                                    isEmptyLine = false;
                                }
                                xx++;
                            }
                        }
                        LastNonemptyLine = yy;
                        detectionPhase = DetectionPhase.VerticalScanning;
                    }
                }

                if (detectionPhase.Equals(DetectionPhase.VerticalScanning))
                {
                    VerticalScanning();
                    y = LastNonemptyLine + 1;
                    x = 0;
                    LineNumber++;
                }
            }
        }

        private void VerticalScanning()
        {
            Rectangle rect;
            int x = 0;
            int y = FirstNonemptyLine;
            while (y < LastNonemptyLine || x < textureFont.Width)
            {
                y++;
                if(y > LastNonemptyLine)
                {
                    y = FirstNonemptyLine;
                    x++;
                }
                if (!isBackgroundPixel(x + y * textureFont.Width))     // found object
                {
                    rect = ExpandArea(bitmapData, new Rectangle(x - 1, y - 1, 3, 3));       // create rectangle around object
                    if (CharacterNumber == 25 || CharacterNumber == 26 || CharacterNumber == 28)     // find another object below this rectangle and add it (for characters ;:=)
                    {
                        int xx = rect.Left;     // NOTE: we start looking from the left of the first object to find the second. That's ok for now, because the character-parts vertically overlap each other
                        int yy = FirstNonemptyLine;
                        while (isBackgroundPixel(xx + yy * textureFont.Width) || rect.Contains(xx, yy))
                        {
                            yy++;
                            if (yy > LastNonemptyLine)
                            {
                                yy = FirstNonemptyLine;
                                xx++;
                            }
                        }
                        Rectangle rect2 = new Rectangle(xx, yy, 3, 3);
                        rect2 = ExpandArea(bitmapData, rect2);       // create rectangle around object

                        // now create a rectangle around both rectangles
                        if (rect2.X < rect.X)
                        {
                            rect.Width += (rect.X - rect2.X);
                            rect.X = rect2.X;
                        }
                        if (rect2.Y < rect.Y)
                        {
                            rect.Height += (rect.Y - rect2.Y);
                            rect.Y = rect2.Y;
                        }
                        if (rect2.Right > rect.Right)
                        {
                            rect.Width += (rect2.Right - rect.Right);
                        }
                        if (rect2.Bottom > rect.Bottom)
                        {
                            rect.Height += (rect2.Bottom - rect.Bottom);
                        }
                    }
                    fontRect[CharacterNumber] = rect;
                    fontYOffset[CharacterNumber] = fontExtraYOffset[LineNumber] + rect.Top - FirstNonemptyLine;
                    CharacterNumber++;
                    x += rect.Width;
                }
            }
            detectionPhase = DetectionPhase.FindObject;
        }

        private Rectangle ExpandArea(Color[] data, Rectangle rect)
        {
            if (!isFreeLine(data, rect, 0))     // check if the upper line is free. if not, expand rectangle 
            {
                rect.Y--;
                rect.Height++;
                rect = ExpandArea(data, rect);
            }
            if (!isFreeLine(data, rect, 1))     // check if the left line is free. if not, expand rectangle 
            {
                rect.X--;
                rect.Width++;
                rect = ExpandArea(data, rect);
            }
            if (!isFreeLine(data, rect, 2))      // check if the bottom line is free. if not, expand rectangle 
            {
                rect.Height++;
                rect = ExpandArea(data, rect);
            }
            if (!isFreeLine(data, rect, 3))     // check if the bottom line is free. if not, expand rectangle 
            {
                rect.Width++;
                rect = ExpandArea(data, rect);
            }

            return rect;
        }

        private bool isFreeLine(Color[] data, Rectangle rect, int direction)
        {
            if(direction==0)    // up 
            {
                for (int x = 0; x < rect.Width; x++)
                {
                    if (data[rect.X + x + rect.Y * textureFont.Width].A != 0)
                    {
                        return false;
                    }
                }
            }
            if (direction == 1)    // left
            {
                for (int y = 0; y < rect.Height; y++)
                {
                    if (data[rect.X + (rect.Y+y) * textureFont.Width].A != 0)
                    {
                        return false;
                    }
                }
            }
            if (direction == 2)    // down
            {
                for (int x = 0; x < rect.Width; x++)
                {
                    if (data[rect.X + x + (rect.Y + rect.Height-1) * textureFont.Width].A != 0)
                    {
                        return false;
                    }
                }
            }
            if (direction == 3)    // right
            {
                for (int y = 0; y < rect.Height; y++)
                {
                    if (data[rect.X + rect.Width-1 + (rect.Y + y) * textureFont.Width].A != 0)
                    {
                        return false;
                    }
                }
            }
            return true;

        }

        public void Print(SpriteBatch spriteBatch, String text, int x, int y, bool printWaving=false)
        {
            int offsetX = 0;
            if(printWaving)
            {
                wavePhase += 0.04f;
            }
            for (int k = 0; k < text.Length; k++)
            {
                if((int)text[k]==32)    // Space
                {
                    offsetX += 45;
                    continue;
                }
                Rectangle rect = fontRect[(int)text[k]-33];
                if (printWaving)
                {
                    spriteBatch.Draw(textureFont, new Rectangle(x + offsetX, (int)(30 * Math.Sin(wavePhase + 0.005*(x + offsetX))) + y + fontYOffset[(int)text[k] - 33], rect.Width, rect.Height), rect, Color.White);
                }
                else
                {
                    spriteBatch.Draw(textureFont, new Rectangle(x + offsetX, y + fontYOffset[(int)text[k] - 33], rect.Width, rect.Height), rect, Color.White);
                }
                offsetX += rect.Width + Spacing;
                if ((int)text[k] > 47 && (int)text[k] < 58)
                {
                    offsetX += SpacingExtraNumbers;
                }
            }
        }
    }
}
