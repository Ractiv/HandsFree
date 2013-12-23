using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.UI;
using Emgu.CV.Structure;

namespace HandsFree
{
    class GestureReader
    {
        //define these reusable constants and objects for better performance
        private const int   imageWidth = Main.ImageWidth, imageHeight = Main.ImageHeight,
                            imageWidthCropped = imageWidth / 2, imageHeightCropped = (int)(imageHeight / 1.5d),
                            maxLineAngle = 20;
    	private static Gray grayThreshold = new Gray(254), white = new Gray(255);

    	private static BlobDetector blobDetectorPalm = new BlobDetector(), blobDetectorFingers = new BlobDetector();    //for clustering palm and fingers

    	public static int Compute(ref Image<Gray, byte> imageSource)
    	{
    		Image<Gray, byte> imageProcessed = imageSource.Erode(1);    //erode image to prevent connected fingertips and other image defects

            //crop image for faster processing and noise cancellation, since right hand is always going to show up in the left of the image
            Image<Gray, byte> imageCropped = new Image<Gray, byte>(imageWidthCropped, imageHeightCropped);
            for (int i = 0; i < imageWidthCropped; i++)
                for (int j = 0; j < imageHeightCropped; j++)
                    imageCropped.Data[j, i, 0] = imageProcessed.Data[j, i, 0];

            //locating palm by eroding fingers (smoothing, equalizing, then thresholding is better than conventional erosion) and dilating the final palm shape
    		Image<Gray, byte> imagePalm = imageCropped.Clone();
            imagePalm._SmoothGaussian(9);
            imagePalm._EqualizeHist();
            imagePalm._ThresholdBinary(new Gray(254), white);
            imagePalm = imagePalm.Dilate(5);
            
    		blobDetectorPalm.Compute(ref imagePalm, 255);    //marking biggest blob (connected pixels cluster) as the palm blob

            //crop the palm blob to exact height using custom heuristics
            int medianWidth = blobDetectorPalm.BlobMaxArea.ComputeMedianWidth();
            int palmCropY = (int)(blobDetectorPalm.BlobMaxArea.YMin + medianWidth * 1.5d);

            List<Point> palmPoints = new List<Point>();
            foreach (Point pt in blobDetectorPalm.BlobMaxArea.Data)
                if (pt.Y < palmCropY)
                {
                    palmPoints.Add(pt);
                    imageCropped.Data[pt.Y, pt.X, 0] = 191;
                }

            //finding the border pixels of palm blob by checking if the pixel is bordering a white pixel
            List<Point> palmBlobBorder = new List<Point>();
            foreach (Point pt in palmPoints)
            {
                int xMin = pt.X - 1, xMax = pt.X + 1,    //8 points surrounding pt
                    yMin = pt.Y - 1, yMax = pt.Y + 1;
                checkBounds(ref xMin, ref xMax, imageWidthCropped);    //check if values are out of bounds of imageCropped
                checkBounds(ref yMin, ref yMax, imageHeightCropped);

                bool kill = false;
                for (int i = xMin; i <= xMax; i++)    //survey 8 points surrounding pt
                {
                    for (int j = yMin; j <= yMax; j++)
                        if (imageCropped.Data[j, i, 0] == 255)    //detect pixels that border white pixels
                        {
                            palmBlobBorder.Add(pt);
                            kill = true;
                            break;
                        }

                    if (kill) break;
                }
            }

            foreach (Point pt in palmBlobBorder) imageCropped.Data[pt.Y, pt.X, 0] = 255;   //setting the color of palm border pixels to white to avoid impeding the progress of bresenham line algorithm 

            double minLineLength = 0d;    //minimum length in order to be marked as line that travels to a fingertip
            List<LineSegment2D> lines = new List<LineSegment2D>();

            //path finding algorithm, find all straight lines that originate from palm boarder and travel to hand shape border
            foreach (Point pt in palmBlobBorder)
                for (int i = 340; i >= 200; i--)    //radiate lines between angles 200 and 340 (upwards between 20 and 160)
                {
                    Point ptResult;
                    double angle = i * Math.PI / 180d;
                    Point ptCircle = getCircumferencePoint(pt, 160, angle);    //end point of the line (opposing the origin end)
                    int x = pt.X, y = pt.Y, x2 = ptCircle.X, y2 = ptCircle.Y;

                    if (bresenham(ref x, ref y, ref x2, ref y2, ref imageCropped, out ptResult))    //radiate lines between orign and end points
                    {
                        LineSegment2D line = new LineSegment2D(ptResult, pt);
                        lines.Add(line);
                        minLineLength += line.Length;    //add current line length to minLineLength since the latter is average length times a coefficient
                    }
                }

            //filter fingerlines to remove ones that do not travel to fingertips, then draw fingerlines that are left onto an image and run blob detection to find finger blobs
            minLineLength = minLineLength / lines.Count * 2.5d;
            Image<Gray, byte> imageFingers = new Image<Gray, byte>(imageWidthCropped, imageHeightCropped);    //new image where all lines that travel to fingertips will be drawn

            foreach (LineSegment2D line in lines)
                if (line.Length > minLineLength || line.P1.X == 0)
                        imageFingers.Draw(line, new Gray(255), 1);    //draw finger lines that are longer than minLineLength, or if fingerline borders the left edge of the image in case the finger isn't fully within view

            imageFingers._SmoothGaussian(3);
            imageFingers._ThresholdBinary(new Gray(254), white);    //smooth drawn fingerlines into finger blobs
            blobDetectorFingers.Compute(ref imageFingers, 255);
            int fingersCount = 0;

            if (blobDetectorFingers.Blobs.Count > 1)
            {
                //heuristics for eliminating false blobs, specifically needed when a fist is shown
                foreach (Blob blob in blobDetectorFingers.Blobs)
                    if (blob.ComputeMaxWidth() >= 2 && blob.Length / (blob.ComputeMedianWidth() + 1) >= 3)
                    {
                        int verificationCount = 0;
                        foreach (Blob blobCurrent in blobDetectorFingers.Blobs)
                            if (blob != blobCurrent && blob.YMin < blobCurrent.YMax)
                                verificationCount++;

                        if (verificationCount > 0) fingersCount++;
                    }
                
            }
            else if (blobDetectorFingers.Blobs.Count == 1)
                if (blobDetectorFingers.BlobMaxArea.Length / (blobDetectorFingers.BlobMaxArea.ComputeMedianWidth() + 1) >= 3)
                    fingersCount = 1;

            if (fingersCount > 3) fingersCount = 3;   //currently limiting to 3 gestures, will be expanded in the future
            OmniViewer.Show(ref imageFingers, "imageFingers");
            return fingersCount;
    	}

        //check if coordinate values are out of bounds, and set values to be within bounds (two birds one function)
        private static bool checkBounds(ref int valMin, ref int valMax, int upperBound)
        {
            if (valMin < 0)
            {
                valMin = 0;
                return false;
            }
            else if (valMax >= upperBound)
            {
                valMax = upperBound - 1;
                return false;
            }

            return true;
        }

        //get end point of a line given the origin of the line, angle between the line and the horizontal line drawn across origin, and length of the line
        private static Point getCircumferencePoint(Point origin, int radius, double angle)
        {
            Point result = new Point();
            result.X = (int)(origin.X + radius * Math.Cos(angle));
            result.Y = (int)(origin.Y + radius * Math.Sin(angle));
            return result;
        }

        //Bresenham's line algorithm
        private static bool bresenham(ref int x, ref int y, ref int x2, ref int y2, ref Image<Gray, byte> imageSource, out Point ptResult)
        {
            ptResult = new Point();

            int w = x2 - x;
            int h = y2 - y;
            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
            if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
            if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
            if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
            int longest = Math.Abs(w);
            int shortest = Math.Abs(h);

            if (!(longest > shortest))
            {
                longest = Math.Abs(h);
                shortest = Math.Abs(w);
                if (h<0) dy2 = -1; else if (h>0) dy2 = 1;
                dx2 = 0;            
            }

            int numerator = longest >> 1;
            for (int i = 0; i <= longest; i++)
            {
                if (checkBounds(ref x, ref x, imageWidthCropped) && checkBounds(ref y, ref y, imageHeightCropped))
                {
                    //only keep drawing if the current pixel the line is drawing across is a white one or has the value of 64
                    if (imageSource.Data[y, x, 0] == 255 || imageSource.Data[y, x, 0] == 64)
                        imageSource.Data[y, x, 0] = 64;
                    else
                    {
                        //otherwise stop and return the position of the last point the line travels across
                        ptResult.X = x;
                        ptResult.Y = y;
                        return true;
                    }
                }
                else
                    return false;

                numerator += shortest;
                if (!(numerator<longest))
                {
                    numerator -= longest;
                    x += dx1;
                    y += dy1;
                }
                else
                {
                    x += dx2;
                    y += dy2;
                }
            }

            return false;
        }

        private static double getDistance(Point pt1, Point pt2)
        {
            return Math.Sqrt(Math.Pow(pt2.X - pt1.X, 2) + Math.Pow(pt2.Y - pt1.Y, 2));
        }
    }
}
