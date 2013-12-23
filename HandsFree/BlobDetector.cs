using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace HandsFree
{
    class BlobDetector
    {
        public List<Blob> Blobs = new List<Blob>();
        public Blob BlobMaxArea = null;

        private Image<Gray, byte> image = null;
        private Blob blob = null;
        private int x, y, x0, y0;
        private Point pt;
        private byte gray;

        public void Compute(ref Image<Gray, byte> imageSource, byte grayToDetect)
        {
            gray = grayToDetect;
            image = imageSource.Clone();

            Blobs = new List<Blob>();
            BlobMaxArea = new Blob();

            for (int j = imageSource.Height - 1; j >= 0; j--)
                for (int i = imageSource.Width - 1; i >= 0; i--)
                {
                    if (image.Data[j, i, 0] == gray)
                    {
                        blob = new Blob();
                        addToBlob(i, j);

                        for (int k = 0; k < blob.Count; k++)
                        {
                            pt = blob.Data[k];

                            if (pt.X > 0 && pt.X < imageSource.Width - 1 && pt.Y > 0 && pt.Y < imageSource.Height - 1)
                            {
                                x = pt.X - 1;
                                y = pt.Y - 1;

                                if (image.Data[y, x, 0] == gray)
                                    addToBlob(x, y);

                                x = pt.X;
                                y = pt.Y - 1;

                                if (image.Data[y, x, 0] == gray)
                                    addToBlob(x, y);

                                x = pt.X + 1;
                                y = pt.Y - 1;

                                if (image.Data[y, x, 0] == gray)
                                    addToBlob(x, y);

                                x = pt.X - 1;
                                y = pt.Y;

                                if (image.Data[y, x, 0] == gray)
                                    addToBlob(x, y);

                                x = pt.X + 1;
                                y = pt.Y;

                                if (image.Data[y, x, 0] == gray)
                                    addToBlob(x, y);

                                x = pt.X - 1;
                                y = pt.Y + 1;

                                if (image.Data[y, x, 0] == gray)
                                    addToBlob(x, y);

                                x = pt.X;
                                y = pt.Y + 1;

                                if (image.Data[y, x, 0] == gray)
                                    addToBlob(x, y);

                                x = pt.X + 1;
                                y = pt.Y + 1;

                                if (image.Data[y, x, 0] == gray)
                                    addToBlob(x, y);
                            }
                        }

                        blob.Compute();
                        Blobs.Add(blob);

                        if (blob.Area > BlobMaxArea.Area)
                            BlobMaxArea = blob;
                    }
                }
        }

        public void Compute(ref Image<Gray, byte> imageSource, int i, int j)
        {
            gray = imageSource.Data[j, i, 0];
            x0 = i;
            y0 = j;
            image = imageSource;
            blob = new Blob();
            addToBlob(i, j);

            for (int k = 0; k < blob.Count; k++)
            {
                pt = blob.Data[k];

                if (pt.X > 0 && pt.X < imageSource.Width - 1 && pt.Y > 0 && pt.Y < imageSource.Height - 1)
                {
                    x = pt.X - 1;
                    y = pt.Y - 1;

                    if (image.Data[y, x, 0] == gray)
                        addToBlob(x, y);

                    x = pt.X;
                    y = pt.Y - 1;

                    if (image.Data[y, x, 0] == gray)
                        addToBlob(x, y);

                    x = pt.X + 1;
                    y = pt.Y - 1;

                    if (image.Data[y, x, 0] == gray)
                        addToBlob(x, y);

                    x = pt.X - 1;
                    y = pt.Y;

                    if (image.Data[y, x, 0] == gray)
                        addToBlob(x, y);

                    x = pt.X + 1;
                    y = pt.Y;

                    if (image.Data[y, x, 0] == gray)
                        addToBlob(x, y);

                    x = pt.X - 1;
                    y = pt.Y + 1;

                    if (image.Data[y, x, 0] == gray)
                        addToBlob(x, y);

                    x = pt.X;
                    y = pt.Y + 1;

                    if (image.Data[y, x, 0] == gray)
                        addToBlob(x, y);

                    x = pt.X + 1;
                    y = pt.Y + 1;

                    if (image.Data[y, x, 0] == gray)
                        addToBlob(x, y);
                }
            }

            blob.Compute();
            Blobs.Add(blob);
        }

        public void Clear()
        {
            Blobs = new List<Blob>();
        }

        private void addToBlob(int x, int y)
        {
            blob.Add(x, y);
            image.Data[y, x, 0] = 63;
        }
    }
}
