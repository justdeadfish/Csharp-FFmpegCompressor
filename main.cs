using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FFmpegBatchEncoder
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("请输入视频所在的路径:");
            string inputPath = Console.ReadLine();
            Console.WriteLine("请输入压制后输出路径:");
            string outputPath = Console.ReadLine();
            Console.WriteLine("请输入crf值:");
            string crf = Console.ReadLine();
            Console.WriteLine("请输入FFmpeg的路径 (默认为 D:\\ffmpeg\\bin\\ffmpeg.exe):");
            string ffmpegPath = Console.ReadLine();

            if (string.IsNullOrEmpty(ffmpegPath))
            {
                ffmpegPath = @"D:\ffmpeg\bin\ffmpeg.exe";
            }

            if (!File.Exists(ffmpegPath))
            {
                Console.WriteLine("找不到FFmpeg，请提供正确的路径。");
                return;
            }

            string[] videoExtensions = new[] { ".mp4", ".mkv", ".m2t", ".mts", ".avi" };
            var files = Directory.GetFiles(inputPath).Where(f => videoExtensions.Contains(Path.GetExtension(f).ToLower())).ToList();

            if (files.Count == 0)
            {
                Console.WriteLine("在指定路径下找不到视频文件。");
                return;
            }

            int threadCount = Environment.ProcessorCount;

            Console.WriteLine($"将使用 {threadCount} 线程进行压制。");

            Console.WriteLine("是否在压缩完所有视频文件后自动关机？(y/n):");
            string shutDownResponse = Console.ReadLine();
            bool shutDownAfterEncoding = shutDownResponse.ToLower() == "y";

            foreach (var file in files)
            {
                string inputFile = file;
                string outputFile = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(file) + "_compressed" + Path.GetExtension(file));

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-i \"{inputFile}\" -c:v libx265 -preset medium -crf {crf} -threads {threadCount} \"{outputFile}\"",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data) && e.Data.Contains("frame="))
                        {
                            Console.WriteLine($"{Path.GetFileName(inputFile)}: {e.Data}");
                        }
                    };

                    process.Start();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        Console.WriteLine($"压制 {Path.GetFileName(inputFile)} 失败，请检查日志。");
                        break;
                    }
                }
            }

            Console.WriteLine("所有文件已处理完毕。");

            if (shutDownAfterEncoding)
            {
                Console.WriteLine("正在准备关机，请确保您已保存所有工作。");
                var shutDownProcess = new ProcessStartInfo("shutdown", "/s /t 60")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(shutDownProcess);
            }
        }
    }
}
