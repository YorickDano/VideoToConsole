using NAudio.Wave;
using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace StartUp
{
    public class Program
    {
        private static readonly int ConsoleWidth = 230;
        private static readonly int ConsoleHeight = 61;
        private const string BrightnessLevels = " .-+*wGHM#&%"; 
        private static WaveOutEvent Woe;
        private static AudioFileReader Reader;
        public static async Task Main()
        {
            var files = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Source"))
                .Select(x => x.Replace(Environment.CurrentDirectory + "\\Source\\", string.Empty)).ToArray();
            string left = "--> ", right = " <--";
            files[0] = left + files[0] + right;
            
            while (true)
            {
                Console.Clear();
                foreach (var file in files)
                {
                    Console.WriteLine(file);
                }
                var pressed = Console.ReadKey().Key;

                switch (pressed)
                {

                    case ConsoleKey.DownArrow:
                        {
                            Console.Clear();
                            var index = GetIndexOfCurrentVideo(files, left, right);
                            files[index] = files[index].Replace(left, "").Replace(right, "");
                            if (index >= files.Length - 1)
                            {
                                index = -1;
                            }
                            files[index + 1] = left + files[index + 1] + right;
                            break;
                        }
                    case ConsoleKey.UpArrow:
                        {
                            Console.Clear();
                            var index = GetIndexOfCurrentVideo(files, left, right);
                            files[index] = files[index].Replace(left, "").Replace(right, "");
                            if (index <= 0)
                            {
                                index = files.Length;
                            }
                            files[index - 1] = left + files[index - 1] + right;
                            break;
                        }
                    case ConsoleKey.Enter:
                        {
                            Console.Clear();
                            var index = GetIndexOfCurrentVideo(files, left, right);
                            PlayAsync(files[index].Replace(left, "").Replace(right, ""));
                            break;
                        }
                }
            }
        }

        private static async void PlayAsync(string fileNameFull)
        {
            FoldersHandling();

            var fileName = fileNameFull.Remove(fileNameFull.IndexOf('.'));
            var inputFileName = $"Source\\{fileNameFull}";

            var isExist = File.Exists(Path.Combine(Environment.CurrentDirectory, "DoneFrames", fileName + ".txt"));

            if (!isExist)
            {
                ExtractFrames(inputFileName);
            }

            ExtractAudio(inputFileName);   
            Reader = new AudioFileReader("tmp\\audio.wav");
            Woe = new WaveOutEvent();
            
            var frames = GetFrames(isExist, fileName);
            int frameCount = frames.Count;

            Woe.Init(Reader);
            Woe.Volume = 0.1f;
            Woe.Play();
            Console.Clear();
            var f = File.Create("tmp\\test.txt");
            f.Dispose();
            while (true)
            {     
                float percentage = (Woe.GetPosition() / (float) Reader.Length);
                int frame = (int)(percentage * frameCount);
                if (frame >= frames.Count)
                    break;

                Console.SetCursorPosition(0, 0);
                Console.WriteLine(frames[frame]);

                if (Console.KeyAvailable)
                {
                    ConsoleKey pressed = Console.ReadKey().Key;
                    switch (pressed)
                    {
                        case ConsoleKey.Spacebar:
                            {
                                if (Woe.PlaybackState == PlaybackState.Playing)
                                    Woe.Pause();
                                else Woe.Play();
                                break;
                            }
                        case ConsoleKey.Escape:
                            {
                                Woe.Stop();
                                Reader.Dispose();
                                Woe.Dispose();
                                return;
                            }
                    }
                }
            }
        }

        private static int GetIndexOfCurrentVideo(IEnumerable<string> files, string left, string right)
        {
            for (int i = 0; i < files.Count(); ++i)
            {
                var file = files.ElementAt(i);
                if (file.Contains(left) && file.Contains(right))
                {
                    return i;
                }
            }
            return -1;
        }

        private static List<string> GetFrames(bool wasCreatedBefore, string fileName)
        {
            var frames = new List<string>();

            if (wasCreatedBefore)
            {
                frames = new List<string>(File.ReadAllText($"DoneFrames\\{fileName}.txt").Split('~'));
            }
            else
            {
                int currentCursorHeight = Console.CursorTop;
                var frameCount = Directory.GetFiles("tmp\\frames", "*.bmp").Length;
                int frameIndex = 1;
                while (true)
                {
                    string filename = "tmp\\frames\\" + frameIndex.ToString() + ".bmp";
                    if (!File.Exists(filename))
                    {
                        Directory.CreateDirectory("DoneFrames");
                        var file = File.CreateText($"DoneFrames\\{fileName}.txt");
                        file.Write(string.Join(string.Empty, frames));
                        break;
                    }
                    var frameBuilder = new StringBuilder();
                    using (var b = new Bitmap(filename))
                    {
                        for (int y = 0; y < b.Height; y++)
                        {
                            for (int x = 0; x < b.Width; x++)
                            {
                                int dIndex = (int)(b.GetPixel(x, y).GetBrightness() * BrightnessLevels.Length);
                                if (dIndex < 0)
                                {
                                    dIndex = 0;
                                }
                                else if (dIndex >= BrightnessLevels.Length)
                                {
                                    dIndex = BrightnessLevels.Length - 1;
                                }
                                frameBuilder.Append(BrightnessLevels[dIndex]);
                            }
                            frameBuilder.Append("\n");
                        }
                    }
                    frames.Add(frameBuilder.ToString() + "~");
                    frameIndex++;

                    int percentage = (int)(frameIndex / (float)frameCount * 100);
                    Console.SetCursorPosition(15, currentCursorHeight);
                    Console.Write(percentage.ToString());
                    Console.SetCursorPosition(22, currentCursorHeight);
                    for (int i = 0; i < percentage / 5; i++)
                    {
                        Console.Write("#");
                    }
                }

            }
            return frames;
        }

        private static void ExtractFrames(string inputFileName)
        {
            using (var ffmpegProcess = new Process())
            {
                ffmpegProcess.StartInfo.FileName = "ffmpeg\\ffmpeg.exe";
                ffmpegProcess.StartInfo.Arguments = "-i \"" + inputFileName + "\" -vf scale=" +
                                        ConsoleWidth + ":" + ConsoleHeight + " tmp\\frames\\%0d.bmp";
                ffmpegProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                ffmpegProcess.Start();
                Console.WriteLine("[INFO] Waiting for ffmpeg.exe to finish...");
                ffmpegProcess.WaitForExit();
            }
        }

        private static void ExtractAudio(string inputFileName)
        {
            using (var ffmpegProcess = new Process())
            {
                ffmpegProcess.StartInfo.FileName = "ffmpeg\\ffmpeg.exe";
                ffmpegProcess.StartInfo.Arguments = "-i \"" + inputFileName + "\" tmp\\audio.wav";
                ffmpegProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                ffmpegProcess.Start();
                Console.WriteLine("[INFO] Waiting for ffmpeg.exe to finish...");
                ffmpegProcess.WaitForExit();
            }
        }

        private static void FoldersHandling()
        {
            if (Directory.Exists("tmp"))
            {
                if (Directory.Exists("tmp\\frames\\"))
                {
                    Directory.Delete("tmp\\frames\\", true);
                }
                Directory.CreateDirectory("tmp\\frames\\");
                if (File.Exists("tmp\\audio.wav"))
                {
                    File.Delete("tmp\\audio.wav");
                }
            }
            else
            {
                Directory.CreateDirectory("tmp\\");
                Directory.CreateDirectory("tmp\\frames\\");
            }
        } 
    }
}