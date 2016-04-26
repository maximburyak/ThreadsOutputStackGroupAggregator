using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication2
{
    public class ThreadStackData
    {
        public int Id { get; set; }
        public int ThreadId { get; set; }
        public List<Tuple<string, int>> CallStack = new List<Tuple<string, int>>();
        public List<Tuple<string, int>> SortedCallStack = new List<Tuple<string, int>>();
        public HashSet<int> SimiliarThreads = new HashSet<int>();
    }

    public class ThreadsOutputStackGroupAggregator
    {
        BlockingCollection<string> _lines = new BlockingCollection<string>();
        public ConcurrentBag<ThreadStackData> _threads = new ConcurrentBag<ThreadStackData>();
        bool readFinished = false;
        Stream _fileStream;
        Dictionary<int, HashSet<int>> _groupedThreads = new Dictionary<int, HashSet<int>>();
        private Dictionary<int, ThreadStackData> _threadsByIDs;

        public ThreadsOutputStackGroupAggregator(Stream fileStream)
        {
            _fileStream = fileStream;
        }
        public void Proccess()
        {
            ReadFile();
            MapThreadsWithSimiliarStacks();
            GroupStacks();
            _threadsByIDs = _threads.ToDictionary(x => x.Id);
        }
        public void ReadFile()
        {

            var readingTask = Task.Run(() =>
            {
                var streamReader = new StreamReader(_fileStream);
                string curLine;

                while ((curLine = streamReader.ReadLine()) != null)
                {
                    _lines.Add(curLine);
                }
                readFinished = true;
            });

            var liensProcessingTask = Task.Run(() =>
            {
                bool startedNewThread = false;
                bool isProcessingCallStack = false;
                ThreadStackData curThreadStack = null;
                while (this.readFinished == false || _lines.Count > 0)
                {
                    string curLine;

                    if (_lines.TryTake(out curLine, 1000) == false)
                        continue;

                    if (startedNewThread)
                    {
                        if (curLine.StartsWith("Child-SP") == false)
                        {
                            continue;
                        }
                        isProcessingCallStack = true;
                        startedNewThread = false;
                        continue;
                    }
                    if (curLine.StartsWith("OS Thread Id"))
                    {
                        startedNewThread = true;
                        isProcessingCallStack = false;
                        var previousThreadStack = curThreadStack;
                        curThreadStack = new ThreadStackData();
                        var leftParanthesis = curLine.IndexOf('(');
                        var rightParanthesis = curLine.IndexOf(')');
                        var threadId = Int32.Parse(curLine.Substring(leftParanthesis + 1, rightParanthesis - leftParanthesis - 1));
                        curThreadStack.Id = threadId;
                        if (previousThreadStack != null)
                        {
                            _threads.Add(previousThreadStack);
                        }
                        continue;
                    }
                    if (isProcessingCallStack == false)
                        continue;
                    try
                    {
                        var essentialStack = curLine.Substring(34);
                        curThreadStack.CallStack.Add(Tuple.Create(essentialStack, essentialStack.GetHashCode()));
                    }
                    catch
                    {
                        _threads.Add(curThreadStack);
                    }

                }
            });
            Task.WaitAll(liensProcessingTask, readingTask);
        }
        public void MapThreadsWithSimiliarStacks()
        {
            Parallel.ForEach(_threads, new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            }, curThread =>
            {
                foreach (var candidateThread in _threads)
                {
                    if (candidateThread.Id == curThread.Id)
                        continue;

                    if (Math.Abs(candidateThread.CallStack.Count - curThread.CallStack.Count) > curThread.CallStack.Count * 0.4)
                        continue;
                    var matchingCallStackLines = 0;
                    var candidateThreadCursor = 0;
                    foreach (var curThreadStackLine in curThread.CallStack)
                    {
                        var lineFound = false;
                        var prevJ = candidateThreadCursor;
                        for (; candidateThreadCursor < candidateThread.CallStack.Count; candidateThreadCursor++)
                        {
                            var candidateThreadStackLine = candidateThread.CallStack[candidateThreadCursor];
                            if (candidateThreadStackLine.Item2 == curThreadStackLine.Item2 &&
                                candidateThreadStackLine.Item1.Equals(curThreadStackLine.Item1))
                            {
                                lineFound = true;
                                break;
                            }
                        }
                        if (lineFound)
                            matchingCallStackLines++;
                        else
                            candidateThreadCursor = prevJ;
                    }

                    if (matchingCallStackLines > (candidateThread.CallStack.Count + curThread.CallStack.Count) * 0.5 * 0.8)
                        curThread.SimiliarThreads.Add(candidateThread.Id);
                }
            });
        }
        public void GroupStacks()
        {
            var alreadyInGroups = new HashSet<int>();
            foreach (var thread in _threads)
            {
                if (alreadyInGroups.Contains(thread.Id))
                    continue;
                var threadIdsWithSimiliarStack = thread.SimiliarThreads.Concat(new[] { thread.Id }).ToList();
                var threadAdded = false;
                foreach (var group in _groupedThreads)
                {
                    if (threadIdsWithSimiliarStack.Any(x => group.Value.Contains(x)))
                    {
                        group.Value.Add(thread.Id);
                        alreadyInGroups.Add(thread.Id);
                        threadAdded = true;
                        break;
                    }
                }
                if (threadAdded == false)
                {
                    _groupedThreads.Add(thread.Id, new HashSet<int>(threadIdsWithSimiliarStack.Except(alreadyInGroups)));
                    foreach (var threadId in threadIdsWithSimiliarStack)
                        alreadyInGroups.Add(threadId);
                }
            }

        }

        public IEnumerable<KeyValuePair<int, HashSet<int>>> GetGroupsEnumenrator()
        {
            return _groupedThreads.AsEnumerable();
        }

        public ThreadStackData GetThread(int key)
        {
            return _threadsByIDs[key];
        }
    }
}
