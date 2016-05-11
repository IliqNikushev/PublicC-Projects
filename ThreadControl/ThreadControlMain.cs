using System;
using System.Threading.Tasks;
using System.Threading;

namespace ThreadControl
{
    class ThreadControlMain
    {
        const int TasksToIssueMAX = 1000 * 10;

        static async void ControlMain()
        {
            ThreadControl.Instance.CompletionState = () => ThreadControl.Instance.TasksCompleted % TasksToIssueMAX == 0;
            ThreadControl.Instance.Completion = new TaskCompletionSource<bool>();
            for (int i = 0; i < TasksToIssueMAX; i++)
            {
                ThreadControl.Instance.Issue(() => { System.Threading.Thread.Sleep(1);});
            }
            await ThreadControl.Instance.Completion.Task;
        }

        static async void ThreadPoolMain()
        {
            TaskCompletionSource<bool> finished = new TaskCompletionSource<bool>();
            object finishedLock = new object();
            int finishedTasks = 0;
            for (int i = 0; i < TasksToIssueMAX; i++)
            {
                System.Threading.ThreadPool.QueueUserWorkItem((j) => { System.Threading.Thread.Sleep(1); lock (finishedLock) { finishedTasks += 1; done += 1; if (finishedTasks % TasksToIssueMAX == 0)finished.SetResult(true); } }, i);
            }
            await finished.Task;
        }

        static object donelock = new object();
        static int done = 0;
        static void TestThreadControl()
        {
            ControlMain();
        }

        static void TestThreadPool()
        {
            ThreadPoolMain();
        }

        static void Main(string[] args)
        {
            int threadCount, b;
            ThreadPool.GetMaxThreads(out threadCount, out b);
            Console.WriteLine(ThreadControl.Instance.ThreadCount + " Threads @ ThreadControl.Instance");
            Console.WriteLine(threadCount + " Threads @ System.Threading.ThreadPool");
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            TestThreadControl();
            while (ThreadControl.Instance.TasksCompleted != TasksToIssueMAX) { System.Threading.Thread.Sleep(1); }
            var t = stopwatch.Elapsed;

            string t1 = t.ToString();
            stopwatch.Restart();
            TestThreadPool();
            while (done != TasksToIssueMAX) { System.Threading.Thread.Sleep(1); }
            t = stopwatch.Elapsed;
            string t2 = t.ToString();

            Console.WriteLine(ThreadControl.Instance.TasksCompleted + " completed by ThreadControl");
            Console.WriteLine(done + " completed by ThreadPool");
            Console.WriteLine(t1 + " ThreadControl's time ");
            Console.WriteLine(t2 + " ThreadPool's time ");

            Console.WriteLine("Press any key");
            Console.ReadKey();
        }
    }
}
