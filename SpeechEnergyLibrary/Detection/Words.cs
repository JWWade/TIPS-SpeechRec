using System;
using System.IO;
using System.Linq;

using NWaves.Audio;
using NWaves.Filters;
using NWaves.Operations;
using NWaves.Signals;

using SpeechEnergyLibrary.Gate;


namespace SpeechEnergyLibrary.Detection
{
    public static class ManualWordCount
    {
        public static DiscreteSignal LoadAudioFile(string filePath)
        {
            // discrete signal where audio file will be kept
            DiscreteSignal signal;

            try
            {
                // attempt to load audio file
                using (var afr = new FileStream(filePath, FileMode.Open))
                {
                    // create a signal file
                    var waveFile = new WaveFile(afr);
                    signal = waveFile[Channels.Left];
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                signal = null;
            }

            return signal;
        }
        public static DiscreteSignal PreprocessAudio(DiscreteSignal signal)
        {
            // smooth signal via moving average filter
            var maFilter = new MovingAverageFilter(19);
            DiscreteSignal smoothedSignal = maFilter.ApplyTo(signal);



            using (var stream = new FileStream("smoothedSignal.wav", FileMode.Create))
            {
                var signalFile = new WaveFile(smoothedSignal);
                signalFile.SaveTo(stream);
            }



            // pre-process signal with SimpleGate
            SimpleGate sg = new SimpleGate(30, 30, smoothedSignal.SamplingRate);

            // apply for each measured sample
            for (int i = 0; i < smoothedSignal.Length; i++)
            {
                double inValue = smoothedSignal.Samples[i];

                sg.Process(ref inValue);

                smoothedSignal.Samples[i] = (float)inValue;
            }

            // apply envelope operation
            DiscreteSignal envelopeSignal = Operation.Envelope(smoothedSignal);

            using (var stream = new FileStream("envelopeSignal.wav", FileMode.Create))
            {
                var signalFile = new WaveFile(envelopeSignal);
                signalFile.SaveTo(stream);
            }

            return envelopeSignal;
        }

        public static int WordCount(DiscreteSignal signal)
        {
            // re-scale y-axis for further detection
            int xLength = signal.Length;
            double maxValue = signal.Samples.Max();
            double scaleFactor = xLength / maxValue;
            for (int i = 0; i < signal.Length; i++)
                signal.Samples[i] = (float)(signal.Samples[i] * scaleFactor);

            // get samples/histogram as double values
            double[] histogram = signal.Samples.Select(s => (double)s).ToArray();

            //// set parameters
            //PeakValleyFinder.Dx = 50;
            //PeakValleyFinder.GrowthAngle = 10;
            //PeakValleyFinder.AbateAngle = -10;
            //PeakValleyFinder.Smoothness = 4;

            //// set parameters
            //PeakValleyFinder.Dx = 50;
            //PeakValleyFinder.GrowthAngle = 30;
            //PeakValleyFinder.AbateAngle = -30;
            //PeakValleyFinder.Smoothness = 35;

            // set parameters
            PeakValleyFinder.Dx = 50;
            PeakValleyFinder.GrowthAngle = 45;
            PeakValleyFinder.AbateAngle = -45;
            PeakValleyFinder.Smoothness = 15;

            // perform peaks/valleys detection
            var detection = PeakValleyFinder.HistFind(histogram);

            // word count is the amount of peaks found in samples/histogram data
            int wordCount = detection.Key.Count;

            return wordCount;
        }
    }
}
