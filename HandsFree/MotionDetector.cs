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

namespace HandsFree {
    class MotionDetector {
        private const int                imageWidth = Main.ImageWidth, imageHeight = Main.ImageHeight,
                                         imageWidthCropped = imageWidth / 4,
                                         gestureCaptureDelay = 5;
        private static Gray              white = new Gray(255);
        private static Image<Gray, byte> imageBlank = new Image<Gray, byte>(imageWidth, imageHeight);

    	private static Image<Bgr, byte>  imageBackground = null,
                                         imageBeforeMotion = null, imageAfterMotion = null;

        private static Timer motionTimer = new Timer();
        private static int   motionTimerCount = 0;

        private static double xWeightedLeft = 0d, xWeightedRight = 0d;
        private static int    xWeightedCount = 0;

        private static bool gestureActive = false, gestureActiveOld = false;
        private static int gestureCaptureDelayCurrent = 0;
        private static int motionXMin, motionXMax;

        static MotionDetector() {
            motionTimer.Interval = 1;
            motionTimer.Tick += motionTimer_Tick;
            motionTimer.Start();
        }

        private static void motionTimer_Tick(object sender, EventArgs e) {
            motionTimerCount++;
        }

    	public static bool Compute(ref Image<Bgr, byte> imageSource, ref Image<Gray, byte> imageResult) {
            bool handDetected = false;
    		if (imageBackground != null) {

                byte baseThreshold = 0;
                Image<Gray, byte> imageProcessed = null;
    			getFrameDifference(ref imageSource, ref imageBackground, ref imageProcessed, ref baseThreshold);
                OmniViewer.Show(ref imageProcessed, "MotionDetector_imageProcessed0");
                List<int> xArray = new List<int>(), yArray = new List<int>();    //find center of motion by finding the median of X and Y positions of pixels brighter than base threshold
                List<Point> candidates = new List<Point>();
                for (int i = 0; i < imageWidth; i++)
                    for (int j = 0; j < imageHeight; j++)
                        if (imageProcessed.Data[j, i, 0] > baseThreshold) {

                            imageProcessed.Data[j, i, 0] = 255;
                            xArray.Add(i); yArray.Add(j);
                            candidates.Add(new Point(i, j));
                        }

                if (candidates.Count > 0) {
                    xArray.Sort(); yArray.Sort();
                    Point centerOfMotion = new Point(xArray[xArray.Count / 3], yArray[yArray.Count / 3]);
                    imageProcessed._ThresholdBinary(new Gray(baseThreshold), white);    //smooth image to cancel noise, and get count of pixels above threshold
                    Image<Gray, byte> imageSmoothed = imageProcessed.SmoothGaussian(9);
                    int pixCount = 0;
                    for (int i = 0; i < imageWidth; i++)
                        for (int j = 0; j < imageHeight; j++)
                            if (imageSmoothed.Data[j, i, 0] == 255) { imageSmoothed.Data[j, i, 0] = 255; pixCount++; }
                            else imageSmoothed.Data[j, i, 0] = 0;
                    
                    if (pixCount > 20 && centerOfMotion.X < imageWidth / 3d) gestureActive = true;    //heuristics to recognize motion as raising hand
                    else if (gestureCaptureDelayCurrent == gestureCaptureDelay) gestureActive = false;
                    else gestureCaptureDelayCurrent++;

                    if (gestureActive) {
                        if (!gestureActiveOld) {
                            imageBeforeMotion = imageBackground;
                            motionTimerCount = 0;
                            xWeightedLeft = xWeightedRight = 0d;
                            xWeightedCount = 0;
                            motionXMin = 9999;
                            motionXMax = 0;
                        }

                        for (int i = 0; i < imageWidth; i++)
                            for (int j = 0; j < imageHeight; j++)
                                if (imageProcessed.Data[j, i, 0] == 255)
                                    if (i < imageWidth / 2) { xWeightedLeft += 1d - (i / (imageWidth / 2d)); xWeightedCount++; }
                                    else { xWeightedRight += 1d - (imageWidth - i) / (imageWidth / 2d); xWeightedCount++; }

                        if (centerOfMotion.X < motionXMin) motionXMin = centerOfMotion.X;
                        if (centerOfMotion.X > motionXMax) motionXMax = centerOfMotion.X;
                        imageSmoothed.Draw(new CircleF(centerOfMotion, 5), new Gray(127), 2);
                    }
                    else if (!gestureActive && gestureActiveOld) {
                        double size = (xWeightedLeft / xWeightedCount) - (xWeightedRight / xWeightedCount);
                        int motionWidth = motionXMax - motionXMin;
                        if (motionTimerCount >= 1 && motionTimerCount < 50 && size > 0.3d && motionWidth < 15) {

                            imageAfterMotion = imageSource;
                            byte baseThreshold2 = 0;    //obtain preliminary hand silhouette by computing frame difference between images before and after motion, then thresholding image by base threshold
                            Image<Gray, byte> imageSubtraction = null;
                            getFrameDifference(ref imageAfterMotion, ref imageBeforeMotion, ref imageSubtraction, ref baseThreshold2);
                            imageSubtraction._ThresholdBinary(new Gray(/*baseThreshold2 / 3*/ 10), white);     //todo: threshold using baseThreshold2 instead of a fixed value
                            imageResult = imageSubtraction.Erode(2).Dilate(2);
                            handDetected = true;
                        }
                        gestureCaptureDelayCurrent = 0;
                    }
                    gestureActiveOld = gestureActive;
                    OmniViewer.Show(ref imageSmoothed, "MotionDetector_imageSmoothed");
                    OmniViewer.Show(ref imageProcessed, "MotionDetector_imageProcessed");
                }
    		}
    		imageBackground = imageSource;
            return handDetected;
    	}

        private static void getFrameDifference(ref Image<Bgr, byte> foregoundImage, ref Image<Bgr, byte> backgroundImage, ref Image<Gray, byte> imageResult, ref byte baseThreshold) {
            imageResult = imageBlank.Clone();
            for (int i = 0; i < imageWidth; i++)    //get frame difference of foreground and background frames, use the maximum value between differences in R, G, and B channels
                for (int j = 0; j < imageHeight; j++) {
                    byte bSc = foregoundImage.Data[j, i, 0],  gSc = foregoundImage.Data[j, i, 1],  rSc = foregoundImage.Data[j, i, 2],
                         bBg = backgroundImage.Data[j, i, 0], gBg = backgroundImage.Data[j, i, 1], rBg = backgroundImage.Data[j, i, 2];
                    int bDiff = Math.Abs(bSc - bBg), gDiff = Math.Abs(gSc - gBg), rDiff = Math.Abs(rSc - rBg), maxDiff = Math.Max(bDiff, gDiff);
                                                                                                               maxDiff = Math.Max(maxDiff, rDiff);
                    imageResult.Data[j, i, 0] = (byte)maxDiff;
                }

            for (int i = imageWidth - 20; i < imageWidth; i++)    //find out base threshold by surveying a 20*20 block on the top-right corner
                for (int j = imageHeight / 3; j >= 0; j--)
                    if (imageResult.Data[j, i, 0] > baseThreshold) baseThreshold = imageResult.Data[j, i, 0];
        }

        private static int getTopPixelY(ref Image<Gray, byte> imageSource) {
            for (int i = 0; i < imageWidthCropped; i++)
                for (int j = 0; j < imageHeight; j++)
                    if (imageSource.Data[j, i, 0] == 255) return j;
            return 0;
        }

        private static int getHighestPixel(int x, int halfSearchWidth, ref Image<Gray, byte> imageSource) {
            int x0 = x - halfSearchWidth, x1 = x + halfSearchWidth;
            if (x0 < 0) x0 = 0; if (x1 >= imageWidth) x1 = imageWidth - 1;
            for (int i = x0; i <= x1; i++)
                for (int j = 0; j < imageHeight; j++)
                    if (imageSource.Data[j, i, 0] == 255) return j;
            return 0;
        }
    }
}
