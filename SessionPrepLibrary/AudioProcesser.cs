using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.IO;
using System.Linq;

namespace SessionPrepLibrary
{
    public class AudioProcesser
    {
        public void ConvertChannels(string filePath, string outputFilePath, string fileName)
        {
            AudioFileReader reader = new AudioFileReader(@$"{filePath}");

            if (reader.WaveFormat.Channels == 2)
            {
                using (var inputReader = new AudioFileReader(@$"{filePath}"))
                {
                    var phaseCheck = new StereoToMonoSampleProvider(inputReader);
                    
                    phaseCheck.LeftVolume = 0.5f;
                    phaseCheck.RightVolume = -0.5f;
                    
                    WaveFileWriter.CreateWaveFile16(@$"{AppDomain.CurrentDomain.BaseDirectory}\PhaseCheck.wav", phaseCheck);
                }

                AudioFileReader phaseCheckReader = new AudioFileReader(@$"{AppDomain.CurrentDomain.BaseDirectory}\PhaseCheck.wav");

                float[] phaseCheckSamples = new float[phaseCheckReader.Length];

                phaseCheckReader.Read(phaseCheckSamples, 0, (int)phaseCheckReader.Length);

                bool allAre0 = phaseCheckSamples.All(index => index.Equals(0));

                if (allAre0)
                {
                    WaveFileWriter.CreateWaveFile(@$"{outputFilePath}\{fileName}", reader.ToMono().ToWaveProvider());

                    reader.Dispose();
                    phaseCheckReader.Dispose();
                    System.IO.File.Delete(@$"{AppDomain.CurrentDomain.BaseDirectory}\PhaseCheck.wav");
                }
                else
                {
                    WaveFileWriter.CreateWaveFile(@$"{outputFilePath}\{fileName}", reader);
                    
                    reader.Dispose();
                    phaseCheckReader.Dispose();
                    System.IO.File.Delete(@$"{AppDomain.CurrentDomain.BaseDirectory}\PhaseCheck.wav");
                }
            }
            else
            {
                WaveFileWriter.CreateWaveFile(@$"{outputFilePath}\{fileName}", reader.ToMono().ToWaveProvider());
                reader.Dispose();
            }
        }

        public void Normalize(string filePath, string outputFilePath, string fileName, float dbfs)
        {
            float conversion;
            conversion = dbfs / 20;
            conversion = (float)Math.Pow(10, conversion);

            var inPath = filePath;
            var outPath = $@"{outputFilePath}\{fileName}";
            var tempLocation = @$"{AppDomain.CurrentDomain.BaseDirectory}\{fileName}";
            float max = 0;

            using (var reader = new AudioFileReader(inPath))
            {
                // find the max peak
                float[] buffer = new float[reader.WaveFormat.SampleRate];
                int read;
                do
                {
                    read = reader.Read(buffer, 0, buffer.Length);
                    for (int n = 0; n < read; n++)
                    {
                        var abs = Math.Abs(buffer[n]);
                        if (abs > max) max = abs;
                    }
                } while (read > 0);

                // rewind and amplify
                reader.Position = 0;
                reader.Volume = conversion / max;

                // write out to a new WAV file
                WaveFileWriter.CreateWaveFile(tempLocation, reader);
            }
            File.Move(tempLocation, outPath, true);
        }

        public bool CheckForEmptyFile(string filePath) 
        {
            using (var inputReader = new AudioFileReader(@$"{filePath}"))
            {
                float[] leftSamples = new float[inputReader.Length];
                inputReader.Read(leftSamples, 0, (int)inputReader.Length);
                bool allAre0 = leftSamples.All(index => index.Equals(0));

                inputReader.HasData((int)inputReader.Length/2);

                if (allAre0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
                
        }


    }
}
