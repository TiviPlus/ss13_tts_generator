using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpTalk;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

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

    class TTS_Generator
    {
        private static string ConverterProgram = "OggEnc.exe";
        private static string regex_voice = "^voice=(?<voice>.*)$";
        private static string regex_text = "^text=(?<text>.*)$";
        private static string regex_name = "^name=(?<name>.*)$";

        private static string program_folder
        {
            get
            {
                string p = new Uri(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)).LocalPath;
                return p;
            }
        }

        private static string data_folder
        {
            get
            {
                string p = program_folder;
                return Path.Combine(p, "data\\");
            }
        }

        private static FonixTalkEngine fte = null;

        static void Log(string message, bool excludeTimestamp = false)
        {
            if (String.IsNullOrEmpty(message))
            {
                return;
            }

            if (!excludeTimestamp)
            {
                message = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + " - " + message;
            }

            Console.WriteLine(message);
            File.AppendAllText(Path.Combine(program_folder, "log.txt"), message + "\n");
        }
        
        static TtsVoice checkVoice(string voice)
        {
            switch (voice.ToLower().Trim())
            {
                case "betty":
                    return TtsVoice.Betty;
                case "dennis":
                    return TtsVoice.Dennis;
                case "frank":
                    return TtsVoice.Frank;
                case "harry":
                    return TtsVoice.Harry;
                case "kit":
                    return TtsVoice.Kit;
                case "paul":
                    return TtsVoice.Paul;
                case "rita":
                    return TtsVoice.Rita;
                case "ursula":
                    return TtsVoice.Ursula;
                case "wendy":
                    return TtsVoice.Wendy;
                default:
                    return TtsVoice.Paul;
            }
        }

        static void generateTTS(TtsVoice? voice, string text, string file)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(file) || voice == null || fte == null)
            {
                return;
            }
            
            /* 
             * The process for generating TTS:
             * 1. Create .lock file
             * 2. Create .wav file
             * 3. Convert to .ogg
             * 4. Create .meta file (containing audio length)
             * 5. Delete .lock file
             */

            /* Lock file */
            File.WriteAllText(file + ".lock", "");

            /* .wav file */
            fte.Voice = (TtsVoice)voice;
            fte.SpeakToWavFile(file + ".wav", text);

            /* Convert to .ogg */
            Process converter = new Process();
            converter.StartInfo.FileName = ConverterProgram;
            converter.StartInfo.WorkingDirectory = program_folder;
            converter.StartInfo.Arguments = file + ".wav";
            converter.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            converter.Start();
            converter.WaitForExit();

            /* Meta file */
            int length = SoundInfo.GetSoundLength(file + ".wav");
            File.WriteAllText(file + ".meta", length.ToString());

            /* Delete lock */
            File.Delete(file + ".lock");
        }

        static string findRequest()
        {
            string output = "";

            /* Check for any request files */
            string[] files = Directory.GetFiles(data_folder, "*.request", SearchOption.AllDirectories);
            string[] locks = Directory.GetFiles(data_folder, "*.rlock", SearchOption.AllDirectories);

            if (files.Length > 0)
            {
                /* Make sure this file isn't locked */
                foreach (string s in files)
                {
                    FileInfo sfi = new FileInfo(s);
                    bool isLocked = false;

                    foreach (string l in locks)
                    {
                        FileInfo lfi = new FileInfo(l);

                        if (sfi.Name == lfi.Name)
                        {
                            isLocked = true;
                            break;
                        }
                    }

                    if (isLocked)
                    {
                        continue;
                    } else
                    {
                        output = s;
                        break;
                    }
                }
            }

            return output;
        }

        static bool AlreadyRunning()
        {
            Process current = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(current.ProcessName);

            foreach (Process p in processes)
            {
                if (p.Id != current.Id)
                {
                    return true;
                }
            }

            return false;
        }

        static void Main(string[] args)
        {
            if (AlreadyRunning())
            {
                Environment.Exit(0);
            }

            fte = new FonixTalkEngine();

            /* Create data folder */
            if (!Directory.Exists(data_folder))
            {
                Directory.CreateDirectory(data_folder);
            }

            DirectoryInfo di = new DirectoryInfo(data_folder);

            /* Empty out the data folder */
            foreach (FileInfo fi in di.GetFiles())
            {
                if (fi.Extension == ".wav" || fi.Extension == ".ogg" || fi.Extension == ".lock" || fi.Extension == ".meta" || fi.Extension == ".request" || fi.Extension == ".rlock")
                {
                    fi.Delete();
                }
            }

            Log("Started successfully!");

            while (true)
            {
                try
                {
                    if (fte == null)
                    {
                        throw new Exception("FonixTalkEngine not started!");
                    }

                    
                    string working_file = findRequest();

                    if (String.IsNullOrEmpty(working_file))
                    {
                        continue;
                    }

                    if (!File.Exists(working_file))
                    {
                        Log("File '" + working_file + "' could not be found!");
                        continue;
                    }

                    string name = "";
                    Log(working_file);

                    /* Open the request
                     * 
                     * We should see three lines:
                     *   name=123johnfg
                     *   voice=paul
                     *   text=hello world
                     */

                    StreamReader sr = new StreamReader(working_file);
                    string line = "";

                    if (sr != null)
                    {
                        string text = "";
                        TtsVoice? voice = null;

                        while ((line = sr.ReadLine()) != null)
                        {
                            Match match = null;

                            if ((match = Regex.Match(line, regex_voice)).Length > 0)
                            {
                                voice = checkVoice(match.Groups["voice"].Value);
                                continue;
                            }

                            if ((match = Regex.Match(line, regex_text)).Length > 0)
                            {
                                text = match.Groups["text"].Value;
                                continue;
                            }

                            if ((match = Regex.Match(line, regex_name)).Length > 0)
                            {
                                name = match.Groups["name"].Value;
                                continue;
                            }
                        }

                        if (String.IsNullOrEmpty(name))
                        {
                            Log("Name not found in '" + working_file + "'!");
                        } else if (String.IsNullOrEmpty(text))
                        {
                            Log("Text not found in '" + working_file + "'!");
                        } else
                        {
                            generateTTS(voice, text, Path.Combine(data_folder, name));
                        }

                        sr.Close();
                    } else
                    {
                        Log("Unable to open '" + working_file + "'!");
                    }

                    /* Delete the request file */
                    File.Delete(working_file);
                }
                catch(Exception ex)
                {
                    if (ex != null)
                    {
                        Log(ex.Message);
                    }

                    break;
                }
            }
        }
    }
}
