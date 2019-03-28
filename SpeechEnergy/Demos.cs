using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using NAudio.Wave;

using NWaves.Audio;
using NWaves.Signals;
using NWaves.Filters;
using NWaves.Operations;
using NWaves.Transforms;

using SpeechEnergyLibrary.Gate;
using SpeechEnergyLibrary.Detection;

namespace SpeechEnergy
{
    public static class Demos
    {
        private static List<string> audioExtensions = new List<string> { ".mp3", ".wav", ".ogg" };

        private static string DATA_PATH = "../../../Data";
        private static string US_DATA_PATH = $"{DATA_PATH}/us";
        private static string UK_DATA_PATH = $"{DATA_PATH}/uk";
        private static string FR_DATA_PATH = $"{DATA_PATH}/fr";
        private static string BETTE_DAVIS_DATA_PATH = $"{DATA_PATH}/bette-davis";
        private static List<string> audioSourcePaths = new List<string> {
            US_DATA_PATH,
            UK_DATA_PATH,
            FR_DATA_PATH,
            BETTE_DAVIS_DATA_PATH,
        };
        public static Dictionary<string, List<string>> audioFilesDataset = new Dictionary<string, List<string>>();

        static Demos()
        {
            foreach (var srcPath in audioSourcePaths)
            {
                try

                {
                    string subjectFolderName = Path.GetFileName(srcPath);

                    // read folder files
                    var audioFiles = Directory.GetFiles(srcPath, "*.*", SearchOption.AllDirectories)
                        .Where(s => audioExtensions.Contains(Path.GetExtension(s)));

                    // add them to dictionary
                    foreach (var af in audioFiles)
                    {
                        if (!audioFilesDataset.ContainsKey(subjectFolderName))
                            audioFilesDataset[subjectFolderName] = new List<string> { af };
                        else
                            audioFilesDataset[subjectFolderName].Add(af);
                    }
                }
                catch (DirectoryNotFoundException ex) {
                    Console.WriteLine("DirectoryNotFoundException: " + ex.Message);
                }
            }
        }

        public static void SoundPlayback(string filePath = "../../../Data/us/OSR_us_000_0010_8k.wav")
        {
            using (var afr = new AudioFileReader(filePath))
            {
                using (var outputDevice = new WaveOutEvent())
                {
                    outputDevice.Init(afr);
                    outputDevice.Play();

                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                        Thread.Sleep(1000);
                }
            }
        }

        public static void ApplyFftToSignal(string filePath)
        {
            Console.WriteLine($"Applying FFT to {filePath} file");

            using (var afr = new FileStream(filePath, FileMode.Open))
            {
                // create a signal file
                var waveFile = new WaveFile(afr, false);
                DiscreteSignal signal = waveFile[Channels.Left];

                float[] real = signal.First(1024).Samples;
                float[] imag = new float[1024];

                var fft = new Fft(1024);
                fft.Direct(real, imag); // in-place FFT

                Console.WriteLine($"FFT size: {fft.Size}");
            }
        }

        public static void TestMovingAverageFilter(string filePath)
        {
            Console.WriteLine($"Testing 'MovingAverage' filters on file {filePath}");

            using (var afr = new FileStream(filePath, FileMode.Open))
            {
                // create a signal file
                var waveFile = new WaveFile(afr);
                DiscreteSignal signal = waveFile[Channels.Left];

                int[] wSizes = new int[] { 3, 7, 11, 15, 19, 23, 27, 31, 35, 39, 43, 47, 51};

                for (int i = 0; i < wSizes.Length; i++)
                {
                    // build moving average filter of specified window size
                    int wSize = wSizes[i];
                    var maFilter = new MovingAverageFilter(wSize);

                    // apply moving average filter
                    DiscreteSignal smoothed = maFilter.ApplyTo(signal);

                    // compute signal differences
                    DiscreteSignal diff = Operation.SpectralSubtract(signal, smoothed);

                    using (var stream = new FileStream($"{DATA_PATH}/moving-average-{wSize}.wav", FileMode.Create))
                    {
                        var smoothedFile = new WaveFile(smoothed);
                        smoothedFile.SaveTo(stream);
                    }

                    using (var stream = new FileStream($"{DATA_PATH}/diff-original-smoothed-{wSize}.wav", FileMode.Create))
                    {
                        var diffFile = new WaveFile(diff);
                        diffFile.SaveTo(stream);
                    }
                }
            }
        }

        public static void TestSimpleGate(string filePath)
        {
            Console.WriteLine($"Testing 'SingleGate' class with file {filePath}");

            using (var afr = new FileStream(filePath, FileMode.Open))
            {
                // create a signal file
                var waveFile = new WaveFile(afr);
                DiscreteSignal signal = waveFile[Channels.Left];

                List<Tuple<int, int>> attackReleaseValues = new List<Tuple<int, int>> {
                    new Tuple<int, int>(5, 5),
                    new Tuple<int, int>(5, 10),
                    new Tuple<int, int>(10, 5),
                    new Tuple<int, int>(10, 10),
                    new Tuple<int, int>(10, 15),
                    new Tuple<int, int>(15, 10),
                    new Tuple<int, int>(15, 15),
                    new Tuple<int, int>(15, 20),
                    new Tuple<int, int>(20, 15),
                    new Tuple<int, int>(20, 20),
                    new Tuple<int, int>(20, 25),
                    new Tuple<int, int>(25, 20),
                    new Tuple<int, int>(25, 25),
                };

                // for each attack/release pair combination
                for (int i = 0; i < attackReleaseValues.Count; i++)
                {
                    int attack, release;
                    attack = attackReleaseValues[i].Item1;
                    release = attackReleaseValues[i].Item2;

                    // instance of SimpleGate
                    SimpleGate sg = new SimpleGate(attack, release, signal.SamplingRate);

                    for (int j = 0; j < signal.Length; j++)
                    {
                        double inValue = signal.Samples[j];
                        sg.Process(ref inValue);
                        signal.Samples[j] = (float)inValue;
                    }

                    using (var stream = new FileStream($"{DATA_PATH}/gate-{attack}-{release}.wav", FileMode.Create))
                    {
                        var signalFile = new WaveFile(signal);
                        signalFile.SaveTo(stream);
                    }
                }
            }
        }

        public static void PreprocessForWordCount(string filePath)
        {
            Console.WriteLine($"Pre-processing for word count of file {filePath}");

            using (var afr = new FileStream(filePath, FileMode.Open))
            {
                // create a signal file
                var waveFile = new WaveFile(afr);
                DiscreteSignal signal = waveFile[Channels.Left];

                // smooth signal via moving average filter
                var maFilter = new MovingAverageFilter(19);
                DiscreteSignal smoothedSignal = maFilter.ApplyTo(signal);

                // instance of SimpleGate
                SimpleGate sg = new SimpleGate(30, 30, smoothedSignal.SamplingRate);

                for (int i = 0; i < smoothedSignal.Length; i++)
                {
                    double inValue = smoothedSignal.Samples[i];

                    sg.Process(ref inValue);

                    smoothedSignal.Samples[i] = (float)inValue;
                }

                using (var stream = new FileStream($"{DATA_PATH}/preprocessed.wav", FileMode.Create))
                {
                    var signalFile = new WaveFile(smoothedSignal);
                    signalFile.SaveTo(stream);
                }
            }
        }

        public static void PreprocessWithEnvelope(string filePath)
        {
            Console.WriteLine($"Pre-processing for word count of file {filePath}");

            using (var afr = new FileStream(filePath, FileMode.Open))
            {
                // create a signal file
                var waveFile = new WaveFile(afr);
                DiscreteSignal signal = waveFile[Channels.Left];

                // smooth signal with envelope
                DiscreteSignal envelope = Operation.Envelope(signal);

                // instance of SimpleGate
                SimpleGate sg = new SimpleGate(30, 30, envelope.SamplingRate);

                for (int i = 0; i < envelope.Length; i++)
                {
                    double inValue = envelope.Samples[i];

                    sg.Process(ref inValue);

                    envelope.Samples[i] = (float)inValue;
                }

                using (var stream = new FileStream($"{DATA_PATH}/envelope.wav", FileMode.Create))
                {
                    var signalFile = new WaveFile(envelope);
                    signalFile.SaveTo(stream);
                }
            }
        }

        public static void PreprocessWithAbsEnvelope(string filePath)
        {
            Console.WriteLine($"Pre-processing with Abs and Envelope of file {filePath}");

            using (var afr = new FileStream(filePath, FileMode.Open))
            {
                // create a signal file
                var waveFile = new WaveFile(afr);

                DiscreteSignal signal = waveFile[Channels.Left];
                for (int i = 0; i < signal.Length; i++)
                    signal.Samples[i] = Math.Abs(signal.Samples[i]);

                // smooth signal with envelope
                DiscreteSignal envelope = Operation.Envelope(signal);

                using (var stream = new FileStream($"{DATA_PATH}/abs-envelope.wav", FileMode.Create))
                {
                    var signalFile = new WaveFile(envelope);
                    signalFile.SaveTo(stream);
                }
            }
        }

        public static void WordCountSignalPreprocessing(string filePath)
        {
            Console.WriteLine($"Pre-processing for word count of file {filePath}");

            using (var afr = new FileStream(filePath, FileMode.Open))
            {
                // create a signal file
                var waveFile = new WaveFile(afr);
                DiscreteSignal signal = waveFile[Channels.Left];

                DiscreteSignal preprocessed = ManualWordCount.PreprocessAudio(signal);

                using (var stream = new FileStream($"{DATA_PATH}/word-count-preprocessed.wav", FileMode.Create))
                {
                    var signalFile = new WaveFile(preprocessed);
                    signalFile.SaveTo(stream);
                }
            }
        }

        public static void WordCount(string filePath)
        {
            Console.WriteLine($"Word detection on file {filePath}");

            DiscreteSignal signal = ManualWordCount.LoadAudioFile(filePath);

            DiscreteSignal signalPreprocessed = ManualWordCount.PreprocessAudio(signal);

            int nWords = ManualWordCount.WordCount(signalPreprocessed);

            Console.WriteLine($"{nWords} words detected in speech");
        }

        public static void SpeechRecognitionFromFile(string filePath)
        {
            Console.WriteLine($"Speech recognition on file {filePath}");

            // detect spoken text
            string textSpoken = SpeechRecognition.SpeechToText(filePath);

            Console.WriteLine($"Text spoken is:");
            Console.WriteLine($"{textSpoken}");
        }

        public static void WordCountFromSpeechRecognition(string filePath)
        {
            Console.WriteLine($"Word detection on file {filePath}");

            // detect spoken text
            string textSpoken = SpeechRecognition.SpeechToText(filePath);

            // count text words
            int nWords = textSpoken.Split(null).Length;

            Console.WriteLine($"{nWords} words detected in speech");
            Console.WriteLine("Text detected:");
            Console.WriteLine(textSpoken);
        }
    }
}