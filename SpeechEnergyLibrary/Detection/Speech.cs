using System;
using System.Threading;

using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace SpeechEnergyLibrary.Detection
{
    public static class SpeechRecognition
    {
        static SpeechSynthesizer ss = new SpeechSynthesizer();
        static SpeechRecognitionEngine sre;
        static bool done = false;
        static bool speechOn = true;
        static bool completed;

        static string textFromSpeech = null;

        public static string SpeechToText(string filePath)
        {
            //// Select a speech recognizer that supports English.
            //RecognizerInfo info = null;
            //foreach (RecognizerInfo ri in SpeechRecognitionEngine.InstalledRecognizers())
            //{
            //    if (ri.Culture.TwoLetterISOLanguageName.Equals("en"))
            //    {
            //        info = ri;
            //        break;
            //    }
            //}

            //if (info == null)
            //    return null;

            using (SpeechRecognitionEngine recognizer = new SpeechRecognitionEngine())
            {
                // Create and load a grammar.  
                Grammar dictation = new DictationGrammar();
                dictation.Name = "Dictation Grammar";

                recognizer.LoadGrammar(dictation);

                // Configure the input to the recognizer.  
                recognizer.SetInputToWaveFile(filePath);

                // Attach event handlers for the results of recognition.
                recognizer.SpeechDetected += Recognizer_SpeechDetected;
                recognizer.SpeechHypothesized += Recognizer_SpeechHypothesized;
                recognizer.SpeechRecognitionRejected += Recognizer_SpeechRecognitionRejected;
                recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(recognizer_SpeechRecognized);
                recognizer.RecognizeCompleted += new EventHandler<RecognizeCompletedEventArgs>(recognizer_RecognizeCompleted);

                // Perform recognition on the entire file.  
                Console.WriteLine("Starting asynchronous recognition...");
                completed = false;
                recognizer.RecognizeAsync();

                // do not close the process  
                while (!completed)
                {
                    // sleep thread for 1/4 of a second
                    Thread.Sleep(250);
                }
            }

            return textFromSpeech;
        }

        private static void Recognizer_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            Console.WriteLine(e.Result.Text);
        }

        private static void Recognizer_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            Console.WriteLine($"Text: {e.Result.Text}");
        }

        private static void Recognizer_SpeechDetected(object sender, SpeechDetectedEventArgs e)
        {
            Console.WriteLine($"{e.AudioPosition}: Word detected");
        }

        // Handle the SpeechRecognized event.  
        private static void recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // save recognized text, if any
            if (e.Result != null && e.Result.Text != null)
                textFromSpeech = e.Result.Text;
            else
            {
                int k = 0;
            }
        }

        // Handle the RecognizeCompleted event.  
        private static void recognizer_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Console.WriteLine("  Error encountered, {0}: {1}",
                e.Error.GetType().Name, e.Error.Message);
            }
            if (e.Cancelled)
            {
                Console.WriteLine("  Operation cancelled.");
            }
            if (e.InputStreamEnded)
            {
                Console.WriteLine("  End of stream encountered.");
            }
            completed = true;
        }
    }
}
