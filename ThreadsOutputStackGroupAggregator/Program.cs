using System;
using System.IO;

namespace ConsoleApplication2
{
    class Program
    {        
        static void Main(string[] args)
        {
            string threadsOutputFilesPath = @"C:\temp\threadsUnmanagedStack";
            string threadsFile = @"C:\temp\threadsUnmanagedStack.txt";

            if (args.Length > 0 && args.Length != 2)
                throw new ArgumentException("There should be either none or two arguments");

            if (args.Length == 2)
            {
                threadsFile = args[0];
                threadsOutputFilesPath = args[1];
            }

            var aggregator = new ThreadsOutputStackGroupAggregator(File.Open(threadsFile, FileMode.Open));
            aggregator.Proccess();

            GenerateThreadGroupFiles(threadsOutputFilesPath, aggregator);
        }

        private static void GenerateThreadGroupFiles(string threadsOutputFilesPath, ThreadsOutputStackGroupAggregator aggregator)
        {
            var dir = Directory.CreateDirectory(threadsOutputFilesPath);

            foreach (var file in dir.GetFiles())
                file.Delete();

            foreach (var groupOfThreads in aggregator.GetGroupsEnumenrator())
            {
                var threadsInGroup = groupOfThreads.Value;
                using (var newFile = File.Create($@"{threadsOutputFilesPath}\{groupOfThreads.Value.Count} {groupOfThreads.Key}.txt"))
                using (var streamWriter = new StreamWriter(newFile))
                {
                    streamWriter.WriteLine($"Details for thread {groupOfThreads.Key}, which has {threadsInGroup.Count - 1} similiar threads");
                    streamWriter.WriteLine("Original Call Stack: ");
                    streamWriter.WriteLine("\n");
                    var key = groupOfThreads.Key;

                    WriteCallStackToStream(aggregator.GetThread(key), streamWriter);

                    if (threadsInGroup.Count > 1)
                    {
                        streamWriter.WriteLine("\n");
                        streamWriter.WriteLine("------------------------------------------------");
                        streamWriter.WriteLine("\n");
                        foreach (var similiarThread in threadsInGroup)
                        {
                            streamWriter.WriteLine($"Call Stack For Thread {similiarThread}:");
                            streamWriter.WriteLine("\n");
                            WriteCallStackToStream(aggregator.GetThread(similiarThread), streamWriter);
                            streamWriter.WriteLine("\n");
                            streamWriter.WriteLine("------------------------------------------------");
                            streamWriter.WriteLine("\n");
                        }
                    }

                    streamWriter.Flush();
                }
            }
        }

        private static void WriteCallStackToStream(ThreadStackData threadStackData, StreamWriter stremWriter)
        {
            foreach (var line in threadStackData.CallStack)
            {
                stremWriter.WriteLine(line.Item1);
            }
        }
    }

    
}
