using System;

using NAudio.Wave;


namespace SpeechEnergyLibrary.Provider
{
    /// <summary>
    /// 
    /// </summary>
    public class ThresholdSampleProvider : ISampleProvider
    {
        /// <summary>
        /// Sound source of the Sample Provider
        /// </summary>
        ISampleProvider _source;

        float _lowerGate = 0.1f;

        bool _isSilent = false;

        public WaveFormat WaveFormat { get => this._source.WaveFormat; }

        public int WourdCount { get; private set; }

        /// <summary>
        /// Creates the threshold Sample Provider
        /// </summary>
        /// <param name="source"></param>
        /// <param name="gate"></param>
        public ThresholdSampleProvider(ISampleProvider inputSource, float gate)
        {
            this._source = inputSource;
            this._lowerGate = gate;
            this.WourdCount = 0;
        }

        /// <summary>
        /// Process the sound to create a threshold
        /// </summary>
        /// <param name="buffer">Sound buffer</param>
        /// <param name="offset">Offset within the sound buffer in sound samples</param>
        /// <param name="count">Sample count to read</param>
        /// <returns></returns>
        public int Read(float[] buffer, int offset, int count)
        {
            var samples = _source.Read(buffer, offset, count);
            var sum = 0.0f;

            int j = 0;
            while (j < samples)
            {
                var maxsamples = Math.Min(samples, j + 10);
                for (int n = j; n < j + 10; n++)
                    sum += Math.Abs(buffer[n]);

                if (sum / count < _lowerGate)
                {
                    if (!_isSilent)
                        WourdCount += 1;

                    for (int n = 0; n < samples; n++)
                        buffer[n] = 0.0f;

                    _isSilent = true;
                }
                else
                    _isSilent = false;

                j++;
            }

            return samples;
        }
    }
}
