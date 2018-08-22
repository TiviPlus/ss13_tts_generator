using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpTalk;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace tts_generator
{
    /* Solution: https://stackoverflow.com/a/82408 */
    public static class SoundInfo
    {
        [DllImport("winmm.dll")]
        private static extern uint mciSendString(
            string command,
            StringBuilder returnValue,
            int returnlength,
            IntPtr winHandle);

        public static int GetSoundLength(string fileName)
        {
            StringBuilder lengthBuf = new StringBuilder(32);

            mciSendString(string.Format("open \"{0}\" type waveaudio alias wave", fileName), null, 0, IntPtr.Zero);
            mciSendString("status wave length", lengthBuf, lengthBuf.Capacity, IntPtr.Zero);
            mciSendString("close wave", null, 0, IntPtr.Zero);

            int length = 0;
            int.TryParse(lengthBuf.ToString(), out length);

            return length;
        }
    }

    class Program
    {
        private static String ConverterProgram = "OggEnc.exe";

        static void Main(string[] args)
        {
            if (args.Length <= 0)
            {
                Environment.Exit(-1);
            }

            List<string> argv = new List<string>();
            string outfile = "";
            string message = "";
            string voice = "";
            
            foreach (string s in args)
            {
                argv.Add(s);
            }
            
            // parse arguments
            for (int k = 0; k < argv.Count; k++)
            {
                string _arg = argv[k].ToLower();

                if (k == argv.Count-1 && message == "")
                {
                    message = argv[k];
                }

                if ((_arg == "-t" || _arg == "--text") && k < argv.Count - 1)
                {
                    message = argv[++k];
                }
                else if ((_arg == "-v" || _arg == "--voice") && k < argv.Count - 1)
                {
                    voice = argv[++k];
                }
                else if ((_arg == "-o" || _arg == "--output") && k < argv.Count - 1)
                {
                    outfile = argv[++k];
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

            if (String.IsNullOrEmpty(outfile))
            {
                outfile = "speech.wav";
            }

            char[] charArray = outfile.ToCharArray();
            Array.Reverse(charArray);
            String outfileExtensionless = new string(charArray);
            int dotIndex = outfileExtensionless.IndexOf(".");
            outfileExtensionless = outfileExtensionless.Substring(dotIndex + 1);
            charArray = outfileExtensionless.ToCharArray();
            Array.Reverse(charArray);
            outfileExtensionless = new string(charArray);

            /* 
             * The process for generating TTS:
             * 1. Create .lock file
             * 2. Create .wav file
             * 3. Convert to .ogg
             * 4. Create .data file (containing audio length)
             * 5. Delete .lock file
             */

            /* Lock file */
            String lockFile = outfileExtensionless + ".lock";
            FileStream fs = File.Create(programPath + lockFile);
            fs.Close();            

            /* Wav file */
            outfile = outfile.Replace("\"", "");
            fte.SpeakToWavFile(programPath + outfile, message);
            
            /* Ogg file */
            Process converter = new Process();            
            converter.StartInfo.FileName = programPath + ConverterProgram;
            converter.StartInfo.Arguments = "\"" + programPath + outfile + "\"";
            converter.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            converter.Start();

            /* Metadata file */
            String metaFile = outfileExtensionless + ".meta";
            int length = SoundInfo.GetSoundLength(programPath + outfile);
            File.WriteAllText(programPath + metaFile, length.ToString());

            /* Delete lock */
            File.Delete(programPath + lockFile);

            Environment.Exit(0);
        }
    }
}
