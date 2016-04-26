using System;
using System.IO;

namespace ConsoleApplication2
{
    class Program
    {        
        static void Main(string[] args)
        {
            string threadsOutputFilesPath = @"C:\temp\threads";
            string threadsFile = @"C:\temp\threads.txt";

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
                var newFile = File.Create($@"{threadsOutputFilesPath}\{groupOfThreads.Value.Count} {groupOfThreads.Key}.txt");
                var threadsInGroup = groupOfThreads.Value;
                var stremWriter = new StreamWriter(newFile);

                stremWriter.WriteLine($"Details for thread {groupOfThreads.Key}, which has {threadsInGroup.Count} similiar threads");
                stremWriter.WriteLine("Original Call Stack: ");
                stremWriter.WriteLine("\n");
                var key = groupOfThreads.Key;

                WriteCallStackToStream(aggregator.GetThread(key), stremWriter);

                stremWriter.WriteLine("\n");
                stremWriter.WriteLine("------------------------------------------------");
                stremWriter.WriteLine("\n");
                foreach (var similiarThread in threadsInGroup)
                {
                    stremWriter.WriteLine($"Call Stack For Thread {similiarThread}:");
                    stremWriter.WriteLine("\n");
                    WriteCallStackToStream(aggregator.GetThread(similiarThread), stremWriter);
                    stremWriter.WriteLine("\n");
                    stremWriter.WriteLine("------------------------------------------------");
                    stremWriter.WriteLine("\n");
                }

                stremWriter.Flush();
                stremWriter.Dispose();
                newFile.Dispose();
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
