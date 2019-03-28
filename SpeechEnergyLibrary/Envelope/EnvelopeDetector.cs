using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechEnergyLibrary.Envelope
{
    public class EnvelopeDetector
    {
        private double sampleRate;
        private double ms;
        private double coeff;

        public EnvelopeDetector() : this(1.0, 44100.0)
        {
        }

        public EnvelopeDetector(double ms, double sampleRate)
        {
            System.Diagnostics.Debug.Assert(sampleRate > 0.0);
            System.Diagnostics.Debug.Assert(ms > 0.0);
            this.sampleRate = sampleRate;
            this.ms = ms;
            SetCoef();
        }

        public double TimeConstant
        {
            get
            {
                return ms;
            }
            set
            {
                System.Diagnostics.Debug.Assert(value > 0.0);
                this.ms = value;
                SetCoef();
            }
        }

        public double SampleRate
        {
            get
            {
                return sampleRate;
            }
            set
            {
                System.Diagnostics.Debug.Assert(value > 0.0);
                this.sampleRate = value;
                SetCoef();
            }
        }

        public void Run(double inValue, ref double state)
        {
            state = inValue + coeff * (state - inValue);
        }

        private void SetCoef()
        {
            coeff = Math.Exp(-1.0 / (0.001 * ms * sampleRate));
        }
    }
}
