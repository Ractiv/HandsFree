using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.UI;
using Emgu.CV.Structure;

namespace HandsFree {
    class Main {
        public const int ImageWidth = 160, ImageHeight = 120;    //global constants
        public bool Enabled;

        private KeyControl keyControl = new KeyControl();    //class that outputs operating system commands
        private Capture camera = new Capture(0);
        private Image<Gray, byte> imageProcessed = null;
        private int frameCount = 0, complexityOld = 0;

        public Main(bool enable = true) {
            OmniViewer.Suppressed = true;    //turn off visualizer windows
            Enabled = enable;
            
            Timer timer = new Timer(); //start timer to query webcam image, as well as read gestures once every 50ms
            timer.Interval = 50;
            timer.Tick += timer_Tick;
            timer.Start();
        }

        public void timer_Tick(object sender, EventArgs e) {
            Image<Bgr, byte> imageQueried = camera.QueryFrame();
            imageQueried = imageQueried.Resize(ImageWidth, ImageHeight, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);    //downsize image for better performance
            if (frameCount > 20) {    //wait for webcam exposure to stabilize

                OmniViewer.Show(ref imageQueried, "imageQueried");
                if (Enabled && MotionDetector.Compute(ref imageQueried, ref imageProcessed)) {    //motion is recognized as hand raise/drop

                    int complexity = ComplexityComputer.Compute(ref imageQueried);    //query the complexity of the current image
                    if (complexity > complexityOld) {    //motion is recognized as hand being raised if current complexity is larger than previous complexity
                        
                        int gestureNum = GestureReader.Compute(ref imageProcessed);    //GestureReader returns an integer indicating which gesture is being detected
                             if (gestureNum == 0) keyControl.PausePlay();
                        else if (gestureNum == 1) keyControl.PrevTrack();
                        else if (gestureNum == 2) keyControl.NextTrack();
                        else if (gestureNum == 3) keyControl.Start();
                    }
                    complexityOld = complexity;    //store current complexity for future comparisons
                }
            }
            else frameCount++;
        }
    }
}
