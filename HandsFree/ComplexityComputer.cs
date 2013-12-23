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
    class ComplexityComputer
    {
		private const int   imageWidthCropped = Main.ImageWidth / 2, imageHeightCropped = (int)(Main.ImageHeight / 1.5d);
        private static Gray cannyThresh = new Gray(50), cannyLinking = new Gray(100);

        public static int Compute(ref Image<Bgr, byte> imageSource)
        {
        	Image<Gray, byte> imageCanny = imageSource.Convert<Gray, byte>().Canny(cannyThresh, cannyLinking);
        	imageCanny.ROI = new Rectangle(0, 0, imageWidthCropped, imageHeightCropped);
        	OmniViewer.Show(ref imageCanny, "imageCanny");

        	int complexity = 0;
        	for (int i = 0; i < imageWidthCropped; i++)
        		for (int j = 0; j < imageHeightCropped; j++)
        			if (imageCanny.Data[j, i, 0] == 255)
        				complexity++;

        	return complexity;
        }
    }
}
