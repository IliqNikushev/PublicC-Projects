using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace ThreadControl
{
    class ThreadControl
    {
        static ThreadControl instance;
        static object instanceLocker = new object();
        public int TasksCompleted { get; private set; }
        public static int DefaultPoolSize { get { return (Environment.ProcessorCount * 3); } }
        public static ThreadControl Instance
        {
            get
            {
                lock (instanceLocker)
                {
                    if (instance == null)
                        instance = new ThreadControl(DefaultPoolSize);
                    return instance;
                }
            }
        }

        private object threadCompletionLock = new object();
        private object taskIssueLock = new object();
        private List<SmartThread> threads;
        private Queue<SmartThread> availableThreads;
        private Queue<Action> tasksToIssue = new Queue<Action>();

        public int ThreadCount { get { return this.threads.Count; } }
        public Func<bool> CompletionState;
        public TaskCompletionSource<bool> Completion;

        public ThreadControl(int threadCount = -1)
        {
            if (threadCount <= 0)
                threadCount = DefaultPoolSize;

            this.threads = new List<SmartThread>(threadCount);
            availableThreads = new Queue<SmartThread>(threadCount);
            for (int i = 0; i < threadCount; i++)
            {
                this.threads.Add(new SmartThread((x) => Instance.NotifyThreadCompleted(x)));
                availableThreads.Enqueue(this.threads[i]);
            }

            Completion = new TaskCompletionSource<bool>();
            Completion.SetResult(true);
        }

        private void NotifyThreadCompleted(SmartThread thread)
        {
            lock (thread.IssueLock)
            {
                Action task;
                lock (taskIssueLock)
                {
                    TasksCompleted += 1;
                    if (tasksToIssue.Count == 0)
                    {
                        if (CompletionState != null)
                            if (CompletionState())
                                Completion.SetResult(true);
                        return;
                    }
                    if (thread.IsWorking) return;

                    task = tasksToIssue.Dequeue();
                }
                thread.Issue(task);
            }
        }

        private void IssueNext()
        {
            for (int i = 0; i < threads.Count; i++)
            {
                SmartThread availableThread = threads[i];

                Action task;
                lock (availableThread.IssueLock)
                {
                    lock (taskIssueLock)
                    {
                        if (availableThread.IsWorking) continue;
                        if (tasksToIssue.Count == 0)
                            return;
                        task = tasksToIssue.Dequeue();
                    }
                    availableThread.Issue(task);
                }
            }
        }

        public void Issue(Action task)
        {
            lock (taskIssueLock)
                tasksToIssue.Enqueue(task);
            IssueNext();
        }


        public void Abort(Action<ThreadAbortException> onException = null)
        {
            lock (taskIssueLock)
            {
                lock (threadCompletionLock)
                {
                    foreach (SmartThread thread in this.threads)
                        thread.Abort(onException);

                    int threadsCount = this.threads.Count;
                    this.threads.Clear();

                    for (int i = 0; i < threadsCount; i++)
                        this.threads.Add(new SmartThread((x) => Instance.NotifyThreadCompleted(x)));
                }
            }
        }
    }
}
