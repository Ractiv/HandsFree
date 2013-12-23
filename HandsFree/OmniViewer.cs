using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.UI;
using Emgu.CV.Structure;

namespace HandsFree
{
    class OmniViewer
    {
        public static Dictionary<string, ImageViewer> viewerDict = new Dictionary<string, ImageViewer>();
        public static bool Suppressed = false;
    	
    	public static void Show(ref Image<Gray, byte> image, string name)
    	{
            if (!Suppressed)
                if (viewerDict.ContainsKey(name))
                    viewerDict[name].Image = image.Resize(2d, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
                else
                {
                    viewerDict.Add(name, new ImageViewer(image.Resize(2d, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR)));
                    viewerDict[name].Show();
                }
    	}

        public static void Show(ref Image<Bgr, byte> image, string name)
        {
            if (!Suppressed)
                if (viewerDict.ContainsKey(name))
                    viewerDict[name].Image = image.Resize(2d, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
                else
                {
                    viewerDict.Add(name, new ImageViewer(image.Resize(2d, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR)));
                	viewerDict[name].Show();
                }
        }

        public static void CloseAll()
        {
            Suppressed = true;

            foreach (ImageViewer viewer in viewerDict.Values)
                viewer.Hide();

            viewerDict = new Dictionary<string, ImageViewer>();
        }
    }
}
