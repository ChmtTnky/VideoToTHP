using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFMpegCore;
using FFMpegCore.Arguments;
using FFMpegCore.Enums;

namespace VideoToTHP
{
    public static class Converter
    {
        const string DEFAULT_OUTPUT = "DOKAPON.THP";
        const string AUDIO = "audio.wav";
        const string JPEG_FOLDER = "jpegs";
        const string THPCONV = "THPConv\\THPConv.exe";

        // THP files are made from a folder of jpegs and a wav file
        // Steps:
        // Get the audio
        // Get the proper THP dimensions
        // Get the frames as individual jpegs
        // Generate a THP file
        public static bool Convert(string source_vid, string output_name=DEFAULT_OUTPUT)
        {
            // Check input
            // Verify that it has audio and video
            var source_analysis = FFProbe.Analyse(source_vid);
            if (source_analysis.PrimaryVideoStream == null)
            {
                Console.WriteLine(
                    "Error:Invalid Format\n" +
                    "Input has no video stream\n");
                return false;
            }
            if (source_analysis.PrimaryAudioStream == null)
            {
                Console.WriteLine(
                    "Error:Invalid Format\n" +
                    "Input has no audio stream\n");
                return false;
            }

            // Setup
            File.Delete(AUDIO);
            File.Delete(output_name);
            if (Directory.Exists(JPEG_FOLDER))
                Directory.Delete(JPEG_FOLDER, true);
            Directory.CreateDirectory(JPEG_FOLDER);

            // THP needs the audio as a separate wav file
            // 32k hz is the only samnple rate that works consistently
            Console.WriteLine(
                "Extracting Audio...");
            FFMpegArguments
                .FromFileInput(source_vid)
                .OutputToFile(AUDIO, true, options => options
                    .ForceFormat("wav")
                    .WithAudioSamplingRate(32000)
                .WithFastStart())
                .ProcessSynchronously();
            if (!File.Exists(AUDIO))
            {
                Console.WriteLine(
                    "Error: Could not extract audio\n" +
                    $"{AUDIO} was not found\n");
                return false;
            }
            else
            {
                Console.WriteLine(
                    "Done\n");
            }

            // Get proper dimensions
            Console.WriteLine(
                "Getting Dimensions...");
            var dimensions = GetTHPDimensions(source_analysis.PrimaryVideoStream.Width, source_analysis.PrimaryVideoStream.Height);
            Console.WriteLine(
                $"New dimensions: {dimensions.width}x{dimensions.height}\n");

            // Extract the video frames
            // THP needs a folder of jpegs
            // This extracts them at max quality
            // 29.97 hz is the best framerate, but 59.94 works with the added risk of lagging the game
            Console.WriteLine(
                "Extracting Frames...");
            FFMpegArguments
                .FromFileInput(source_vid)
                .OutputToFile(Path.Combine(JPEG_FOLDER, "%05d.jpeg"), true, options => options
                    .WithCustomArgument("-q:v 1")
                    .WithFramerate(29.97)
                    .WithVideoFilters(filterOptions => filterOptions
                        .Scale(dimensions.width, dimensions.height))
                .WithFastStart())
                .ProcessSynchronously();
            Console.WriteLine(
                $"Extracted {Directory.GetFiles(JPEG_FOLDER).Length} Frames\n" +
                "Done\n");

            // Generate THP file
            Console.WriteLine(
                "Generating THP...");
            Process? process = Process.Start(new ProcessStartInfo()
            {
                FileName = THPCONV,
                Arguments = $"-j {Path.Combine(JPEG_FOLDER, "*.jpeg")} -s {AUDIO} -r 29.97 -d {output_name}"
            });
            process?.WaitForExit();
            process?.Close();
            if (!File.Exists(output_name))
            {
                Console.WriteLine(
                    "Error: Could not create THP file\n" +
                    $"{output_name} was not found\n");
                return false;
            }
            else
            {
                Console.WriteLine(
                    "Done\n");
            }

            // Cleanup
            File.Delete(AUDIO);
            if (Directory.Exists(JPEG_FOLDER))
                Directory.Delete(JPEG_FOLDER, true);

            return true;
        }

        // Width and Height need to be converted to fit within specific bounds
        // Assume the user wants the highest resolution possible
        // THP max width is 672 px (no max height)
        // Dokapon max height is 480 px (max width less than THP max width)
        // THP files needs both dimensions to be a multiple of 16
        private static (int width, int height) GetTHPDimensions(int src_width, int src_height)
        {
            double aspect_ratio = (double)src_width / src_height;
            // start by assuming the video is wider than it is tall (e.g. 16 by 9)
            int new_width = 672;
            int new_height = (int)(new_width * (1.0 / aspect_ratio));
            // normalize dimensions to multiples of 16
            if (new_height > 480) 
            {
                new_height = 480;
                new_width = (int)(new_height * aspect_ratio);
                if (new_width % 16 < 8)
                    new_width -= new_width % 16;
                else
                    new_width += 16 - (new_width % 16);
            }
            else
            {
                if (new_height % 16 < 8)
                    new_height -= new_height % 16;
                else
                    new_height += 16 - (new_height % 16);
            }
            // safeguard in case the calcs fucked up
            new_width = Math.Clamp(new_width, 16, 672);
            new_height = Math.Clamp(new_height, 16, 480);
            return (new_width, new_height);
        }
    }
}
