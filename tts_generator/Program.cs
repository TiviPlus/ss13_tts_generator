using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpTalk;
using System.IO;
using System.Diagnostics;

namespace tts_generator
{
    class Program
    {
        private static String ConverterProgram = "OggEnc.exe";
        
        static void Main(string[] args)
        //[DllExport("generate", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        //public static void generate(string msg)
        {
            if (args.Length <= 0)
            {
                Environment.Exit(-1);
            }

            List<string> argv = new List<string>();
            string message = "";
            string voice = "";
            
            foreach (string s in args)
            {
                argv.Add(s);
            }
            
            // parse arguments
            for (int k = 0; k < argv.Count; k++)
            {
                if (k == argv.Count-1 && message == "")
                {
                    message = argv[k];
                }

                if ((argv[k].ToLower() == "-t" || argv[k].ToLower() == "--text") && k < argv.Count-1)
                {
                    message = argv[++k];
                }
                else if ((argv[k].ToLower() == "-v" || argv[k].ToLower() == "--voice") && k < argv.Count-1)
                {
                    voice = argv[++k];
                }
            }

            // Check to make sure we have all we need
            if (message.Length <= 0)
            {
                Environment.Exit(-1);
            }

            String programPath = new Uri(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)).LocalPath;

            if (!programPath.EndsWith("\\"))
            {
                programPath += "\\";
            }
            
            FonixTalkEngine fte = new FonixTalkEngine();
            
            // check voices
            switch(voice.ToLower().Trim())
            {
                case "betty":
                    fte.Voice = TtsVoice.Betty;
                    break;
                case "dennis":
                    fte.Voice = TtsVoice.Dennis;
                    break;
                case "frank":
                    fte.Voice = TtsVoice.Frank;
                    break;
                case "harry":
                    fte.Voice = TtsVoice.Harry;
                    break;
                case "kit":
                    fte.Voice = TtsVoice.Kit;
                    break;
                case "paul":
                    fte.Voice = TtsVoice.Paul;
                    break;
                case "rita":
                    fte.Voice = TtsVoice.Rita;
                    break;
                case "ursula":
                    fte.Voice = TtsVoice.Ursula;
                    break;
                case "wendy":
                    fte.Voice = TtsVoice.Wendy;
                    break;
                default:
                    fte.Voice = TtsVoice.Paul;
                    break;
            }

            fte.SpeakToWavFile(programPath + "speech.wav", message);
            
            Process converter = new Process();
            
            converter.StartInfo.FileName = programPath + ConverterProgram;
            converter.StartInfo.Arguments = "\"" + programPath + "speech.wav" + "\"";
            converter.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            converter.Start();
            
            Environment.Exit(0);
        }
    }
}
