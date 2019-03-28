using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechEnergyLibrary.Envelope
{
    public class AttackReleaseEnvelope
    {
        // DC offset to prevent denormal
        protected const double DC_OFFSET = 1.0E-25;

        private readonly EnvelopeDetector attack;
        private readonly EnvelopeDetector release;

        public AttackReleaseEnvelope(double attackMilliseconds, double releaseMilliseconds, double sampleRate)
        {
            attack = new EnvelopeDetector(attackMilliseconds, sampleRate);
            release = new EnvelopeDetector(releaseMilliseconds, sampleRate);
        }

        public double Attack
        {
            get { return attack.TimeConstant; }
            set { attack.TimeConstant = value; }
        }

        public double Release
        {
            get { return release.TimeConstant; }
            set { release.TimeConstant = value; }
        }

        public double SampleRate
        {
            get { return attack.SampleRate; }
            set { attack.SampleRate = release.SampleRate = value; }
        }

        public void Run(double inValue, ref double state)
        {
            // assumes that:
            // positive delta = attack
            // negative delta = release
            // good for linear & log values
            if (inValue > state)
                attack.Run(inValue, ref state);   // attack
            else
                release.Run(inValue, ref state);  // release
        }
    }
}
