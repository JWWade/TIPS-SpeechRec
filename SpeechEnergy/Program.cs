using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpeechEnergy
{
    // You are not much of a bargain, you are con and thoughtless and messy

    class Program
    {
        static void Main(string[] args)
        {
            // play sound from file using NAudio
            //Demos.SoundPlayback();

            //string soundFilePath = Demos.audioFilesDataset["us"][5];
            //Demos.ApplyFftToSignal(soundFilePath);

            //string soundFilePath = Demos.audioFilesDataset["us"][0];
            //Demos.TestSimpleGate(soundFilePath);

            //string soundFilePath = Demos.audioFilesDataset["us"][0];
            //Demos.TestMovingAverageFilter(soundFilePath);

            //string soundFilePath = Demos.audioFilesDataset["us"][0];
            //Demos.PreprocessForWordCount(soundFilePath);

            //string soundFilePath = Demos.audioFilesDataset["us"][0];
            //Demos.PreprocessWithEnvelope(soundFilePath);

            //string soundFilePath = Demos.audioFilesDataset["us"][0];
            //Demos.PreprocessWithAbsEnvelope(soundFilePath);

            for (int i = 0; i < 27; i++)
            {
                string soundFilePath = Demos.audioFilesDataset["us"][i];
                Demos.WordCount(soundFilePath);
            }

            //string soundFilePath = Demos.audioFilesDataset["bette-davis"][4];
            //Demos.SpeechRecognitionFromFile(soundFilePath);

            //string soundFilePath = Demos.audioFilesDataset["us"][0];
            //Demos.WordCountFromSpeechRecognition(soundFilePath);
            Console.ReadLine();
        }
    }
}
