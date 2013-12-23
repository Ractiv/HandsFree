using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Util;
using Emgu.CV.Structure;

namespace HandsFree
{
    class Blob
    {
        public List<Point> Data = new List<Point>();

        public int    XMin = 9999, XMax = 0, YMin = 9999, YMax = 0;
        public int    Width, Height, Area, Count = 0;
        public double Length;
        public Point  P0 = new Point(), P1 = new Point();

        public void Add(int x, int y)
        {
            Data.Add(new Point(x, y));

            if (x < XMin) XMin = x;
            if (x > XMax) XMax = x;
            if (y < YMin) { YMin = y; P0.X = x; P0.Y = y; }
            if (y > YMax) { YMax = y; P1.X = x; P1.Y = y; }

            Count++;
        }

        public void Compute()
        {
            Width = XMax - XMin;
            Height = YMax - YMin;
            Area = Width * Height;
            Length = Math.Sqrt(Math.Pow(P0.X - P1.X, 2) + Math.Pow(P0.Y - P1.Y, 2));
        }

        public int ComputeMaxWidth()
        {
            Image<Gray, byte> imageMaxWidth = new Image<Gray, byte>(Width + 1, Height + 1);
            foreach (Point pt in Data)
                imageMaxWidth.Data[pt.Y - YMin, pt.X - XMin, 0] = 255;

            int maxWidth = 0;
            for (int j = Height; j >= 0; j--)
            {
                bool started = false;
                int startX = 0, endX = 0;
                for (int i = Width; i >= 0; i--)
                {
                    if (imageMaxWidth.Data[j, i, 0] == 255)
                        if (started)
                            endX = i;
                        else
                        {
                            started = true;
                            startX = i;
                            endX = i;
                        }
                }

                int currentWidth = Math.Abs(startX - endX);
                if (currentWidth > maxWidth)
                    maxWidth = currentWidth;
            }

            return maxWidth;
        }

        public int ComputeMedianWidth()
        {
            Image<Gray, byte> imageMediaWidth = new Image<Gray, byte>(Width + 1, Height + 1);
            foreach (Point pt in Data)
                imageMediaWidth.Data[pt.Y - YMin, pt.X - XMin, 0] = 255;

            List<int> widths = new List<int>();
            for (int j = Height; j >= 0; j--)
            {
                bool started = false;
                int startX = 0, endX = 0;
                for (int i = Width; i >= 0; i--)
                {
                    if (imageMediaWidth.Data[j, i, 0] == 255)
                    {
                        if (started)
                            endX = i;
                        else
                        {
                            started = true;
                            startX = i;
                            endX = i;
                        }
                    }
                }

                int currentWidth = Math.Abs(startX - endX);
                widths.Add(currentWidth);
            }

            if (widths.Count > 0)
            {
                widths.Sort();
                return widths[widths.Count / 2];
            }

            return 0;
        }
    }
}
