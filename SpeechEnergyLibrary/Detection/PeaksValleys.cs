using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechEnergyLibrary.Detection
{
    /// <summary>
    /// Integral type
    /// </summary>
    public enum IntegralType { Horizontal, Vertical };

    /// <summary>
    /// Finds peaks and valleys
    /// </summary>
    public static class PeakValleyFinder
    {
        #region Fields
        /// <summary>
        /// Represents Math.PI
        /// </summary>
        private static double PI = Math.PI;
        #endregion

        #region Contructors
        /// <summary>
        /// PeakValleyFinder static constructor
        /// </summary>
        static PeakValleyFinder()
        {
            // default Δx value for the class
            Dx = 5;

            // default growth angle of 30 degrees
            GrowthAngle = 45;

            // default abate angle of -30 degress
            AbateAngle = -45;

            // default curve smoothness for the class
            Smoothness = 3;
        }
        #endregion

        #region Enums
        /// <summary>
        /// Represents the monotony of the function in interval I
        /// </summary>
        private enum Monotony
        {
            Grow,       // the function increases
            Abate,      // the funtion decreases
            Stable      // the function is stable
        };

        /// <summary>
        /// Represents the posible extreme points
        /// </summary>
        private enum ExtremeType
        {
            Min,        // the extreme point is a MIN
            Max,        // the extreme point is a MAX
            None        // no extreme point
        };

        #endregion

        #region Properties
        /// <summary>
        /// Size of interval of analysis I
        /// </summary>
        static int deltha = 1;
        /// <summary>
        /// Represents the interval size (Δx)
        /// </summary>
        public static int Dx
        {
            get { return deltha; }
            set
            {
                // Δx must be at least 1 
                if (deltha < 1)
                    throw new InvalidOperationException("Interval size must be positive.");

                deltha = value;
            }
        }

        /// <summary>
        /// Growth angle of certain interval
        /// </summary>
        private static int growth_angle;
        /// <summary>
        /// Angle that indicates if the function is really growing
        /// </summary>
        /// <remarks>The angle is in degrees</remarks>
        public static int GrowthAngle
        {
            get { return growth_angle; }
            set
            {
                growth_angle = value;
                GrowthThres = Math.Tan(growth_angle * PI / 180);
            }
        }
        /// <summary>
        /// Numeric threshold that really determines if f really grows
        /// </summary>
        private static double GrowthThres { get; set; }

        /// <summary>
        /// Abate angle of certain interval
        /// </summary>
        private static int abate_angle;
        /// <summary>
        /// Angle that indicates if the function is really abating
        /// </summary>
        /// <remarks>The angle is in degrees</remarks>
        public static int AbateAngle
        {
            get { return abate_angle; }
            set
            {
                abate_angle = value;
                AbateThres = Math.Tan(abate_angle * PI / 180);
            }
        }
        /// <summary>
        /// Numeric threshold that really determines if f really abates
        /// </summary>
        private static double AbateThres { get; set; }

        /// <summary>
        /// Indicates the "smoothness" of the curve
        /// </summary>
        /// <remarks>At the end, represents the amount of intervals I between MINs and MAXs</remarks>
        public static int Smoothness { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// Finds peaks and valleys in certain histogram
        /// </summary>
        /// <param name="histogram">Histogram involved</param>
        /// <returns>Histogram's peaks and valleys</returns>
        /// <remarks>Does not perform histogram normalization</remarks>
        public static KeyValuePair<List<KeyValuePair<int, int>>, List<KeyValuePair<int, int>>> HistFind(double[] histogram)
        {
            // using var because static types are so darn long
            var peaks = new List<KeyValuePair<int, int>>();
            var valleys = new List<KeyValuePair<int, int>>();
            var result = new KeyValuePair<List<KeyValuePair<int, int>>, List<KeyValuePair<int, int>>>(peaks, valleys);

            // setting interval size
            int dx = Dx;

            // this is because there's always point shared betwwen intervals
            int offset = dx - 1;

            // there can not exist peaks or valleys (there's at most one interval)
            if (dx < 2 || histogram.Length < 2 * offset + 1)
                return result;

            // setting the smoothness of the curve
            int smoothness = Smoothness;

            // Ip: previous interval (Ip = [a, b])
            // Ic: current interval  (Ic = [b, c])
            int a = 0;
            int b = offset;
            int c = b + offset;

            // analyzing Ip monotony
            Monotony ip_monotony = FindMonotony(histogram[a], histogram[b], dx);

            // setting previous extreme point
            ExtremeType previous_extreme = ExtremeType.None;
            int previous_extreme_index = 1;

            if (ip_monotony == Monotony.Abate)
                previous_extreme = ExtremeType.Max;
            else if (ip_monotony == Monotony.Grow)
                previous_extreme = ExtremeType.Min;

            // setting current extreme point
            ExtremeType current_extreme = ExtremeType.None;
            int current_extreme_index = 0;

            // setting the first extreme point of the last registered shift
            ExtremeType previous_shift_extreme = ExtremeType.None;
            int previous_shift_extreme_index = 0;

            // while the current interval is in range
            while (c < histogram.Length)
            {
                // analyzing the monotony in Ic
                Monotony ic_monotony = FindMonotony(histogram[b], histogram[c], dx);

                // if there was a change in the monotony => there's a extreme
                if (ip_monotony != ic_monotony)
                {
                    // classifying the new extreme founded
                    current_extreme = GetExtremeType(ip_monotony, ic_monotony);
                    current_extreme_index = b;

                    // if there was a shift indeed
                    if (current_extreme != previous_extreme/*&& previous_extreme != ExtremeType.None*/)
                    {
                        // if this is not the first shift and phenomenon is smooth enough
                        if (previous_shift_extreme != ExtremeType.None && (current_extreme_index - previous_shift_extreme_index) / dx >= smoothness)
                        {
                            // we are in the presence of a VALLEY
                            if (previous_shift_extreme == ExtremeType.Max && current_extreme == ExtremeType.Max)
                            {
                                valleys.Add(new KeyValuePair<int, int>(previous_shift_extreme_index, current_extreme_index));
                            }
                            // we are in the presence of a PEAK
                            else if (previous_shift_extreme == ExtremeType.Min && current_extreme == ExtremeType.Min)
                            {
                                peaks.Add(new KeyValuePair<int, int>(previous_shift_extreme_index, current_extreme_index));
                            }
                        }

                        // update previous shift extreme
                        previous_shift_extreme = previous_extreme;
                        previous_shift_extreme_index = previous_extreme_index;
                    }

                    // updating previous extreme point
                    previous_extreme = current_extreme;
                    previous_extreme_index = current_extreme_index;
                }

                // updating intervals
                a = b;
                b = c;
                c += offset;

                // updating last interval monotony
                ip_monotony = ic_monotony;
            }

            return result;
        }

        /// <summary>
        /// Clasifies an extreme point acording to monotony
        /// </summary>
        /// <param name="previous">Monotony of the previous interval</param>
        /// <param name="current">Monotony of the current interval</param>
        /// <returns>Extreme point type</returns>
        private static ExtremeType GetExtremeType(Monotony previous, Monotony current)
        {
            if (previous == Monotony.Grow && current == Monotony.Stable || previous == Monotony.Grow && current == Monotony.Abate || previous == Monotony.Stable && current == Monotony.Abate)
                return ExtremeType.Max;

            if (previous == Monotony.Abate && current == Monotony.Stable || previous == Monotony.Abate && current == Monotony.Grow || previous == Monotony.Stable && current == Monotony.Grow)
                return ExtremeType.Min;

            return ExtremeType.None;
        }

        /// <summary>
        /// Finds the monotony in an interval using central differences
        /// </summary>
        /// <param name="fa">Function value at a [f(a)]</param>
        /// <param name="fb">Function value at b [f(b)]</param>
        /// <param name="dx">Interval size</param>
        /// <returns>The type of monotony</returns>
        private static Monotony FindMonotony(double fa, double fb, int dx)
        {
            double m = (fb - fa) / dx;

            if (m >= GrowthThres)
                return Monotony.Grow;

            if (m <= AbateThres)
                return Monotony.Abate;

            return Monotony.Stable;
        }

        /// <summary>
        /// Determines if there was a shift between 2 extreme points
        /// </summary>
        /// <param name="p1">First extreme point</param>
        /// <param name="p2">Second extreme point</param>
        /// <returns>If there was a shift or not</returns>
        private static bool IsShift(ExtremeType p1, ExtremeType p2)
        {
            // if points are not defined or are of the same type
            if (p1 == ExtremeType.None || p2 == ExtremeType.None || p1 == p2)
                return false;

            // that is a shift
            return true;
        }

        private static int GetValleySymmetryCenter(double[] h, int lb, int ub)
        {
            // half point
            int half = -1;

            // while possible
            while (lb <= ub)
            {
                // getting half point
                half = (lb + ub) / 2;

                if (h[lb] < h[half])
                {
                    // choosing left interval
                    ub = half;
                }
                else if (h[ub] < h[half])
                {
                    // choosing right interval
                    lb = half;
                }
                else
                    break;
            }

            return half;
        }

        private static KeyValuePair<int, int> ApplySymmetry(double[] h, int lb, int ub)
        {
            int middle = GetValleySymmetryCenter(h, lb, ub);

            // if h is "OK", then keep interval
            if (middle < 0)
                return new KeyValuePair<int, int>(lb, ub);

            // getting interval size
            int dx = Dx;

            int new_lb = middle;
            int new_ub = middle;

            // keeping aspect ratio between x axis and y axis
            while (Math.Abs(h[new_lb] - h[new_ub]) < dx)
            {
                int lb_dx = (new_lb - dx < lb) ? 0 : dx;
                int ub_dx = (new_ub + dx > ub) ? 0 : dx;

                // if one of both sides is out of range
                if (lb_dx == 0 || ub_dx == 0)
                    break;

                // if not symmetric
                if (Math.Abs(h[lb - lb_dx] - h[ub + ub_dx]) > dx)
                    break;

                // ubdating boundaries
                new_lb -= lb_dx;
                new_ub += ub_dx;
            }

            // checking if not symmetric at all
            if (new_lb == new_ub)
                return new KeyValuePair<int, int>(lb, ub);

            return new KeyValuePair<int, int>(new_lb, new_ub);
        }
        #endregion
    }

    /// <summary>
    /// Detection utilities
    /// </summary>
    static class DetectionUtils
    {
        #region Methods
        /// <summary>
        /// Performs histogram normalization
        /// </summary>
        /// <param name="h">Histogram to normalize</param>
        /// <remarks>Normalization betwwen min value and max value (at 255)</remarks>
        public static void NormalizeHistogram(double[] h)
        {
            double max = h.Max();

            for (int i = 0; i < h.Length; i++)
                h[i] = h[i] * 255 / max;
        }
        #endregion
    }
}
