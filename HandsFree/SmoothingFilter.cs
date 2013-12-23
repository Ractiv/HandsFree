using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace HandsFree
{
    class SmoothingFilter
    {
        private Dictionary<string, double> valueOld = new Dictionary<string, double>();
        private Dictionary<string, int> valueOldInt = new Dictionary<string, int>();
        private Dictionary<string, Point> ptOld = new Dictionary<string, Point>(), ptOldRaw = new Dictionary<string, Point>();

        public double LowPass(double value, string key, double alpha)
        {
            double result = value;
            if (valueOld.ContainsKey(key))
                result = valueOld[key] + alpha * (value - valueOld[key]);

            valueOld[key] = result;
            return result;
        }

        public int LowPass(int value, string key, double alpha)
        {
            int result = value;
            if (valueOldInt.ContainsKey(key))
                result = valueOldInt[key] + (int)(alpha * (value - valueOldInt[key]));

            valueOldInt[key] = result;
            return result;
        }

        public Point LowPass(Point pt, string key, double alpha)
        {
            Point result = pt;

            if (ptOld.ContainsKey(key))
            {
                result.X = ptOld[key].X + (int)(alpha * (pt.X - ptOld[key].X));
                result.Y = ptOld[key].Y + (int)(alpha * (pt.Y - ptOld[key].Y));
            }

            ptOld[key] = result;
            return result;
        }

        public Point ExponentialSmooth(Point pt, string key, double alpha)
        {
            Point result = pt;

            if (ptOld.ContainsKey(key))
            {
                result.X = (int)((alpha * ptOldRaw[key].X) + ((1d - alpha) * ptOld[key].X));
                result.Y = (int)((alpha * ptOldRaw[key].Y) + ((1d - alpha) * ptOld[key].Y));
            }

            ptOld[key] = result;
            ptOldRaw[key] = pt;
            return result;
        }

        public void Clear()
        {
            valueOld = new Dictionary<string, double>();
            valueOldInt = new Dictionary<string, int>();
            ptOld = new Dictionary<string, Point>();
        }
    }
}
