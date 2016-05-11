using System;
using System.Threading.Tasks;
using System.Threading;

namespace ThreadControl
{
    class SmartThread : IDisposable
    {
        public readonly object IssueLock = new object();
        private Thread thread;
        private TaskCompletionSource<bool> taskIssuedSource;
        public bool IsWorking { get { return currentTaskToExecute != null; } }

        public void Abort(Action<ThreadAbortException> onException = null)
        {
            try
            {
                thread.Abort();
            }
            catch (ThreadAbortException ex)
            {
                if (onException != null)
                    onException(ex);
            }

            this.Dispose();
        }

        private Action<SmartThread> onCompleted;

        public SmartThread(Action<SmartThread> onCompleted)
        {
            this.onCompleted = onCompleted;
            this.taskIssuedSource = new TaskCompletionSource<bool>();
            this.thread = new Thread(() => ProcessThread());
            this.thread.Start();
        }

        private async void ProcessThread()
        {
            while (true)
            {
                await taskIssuedSource.Task;
                taskIssuedSource = new TaskCompletionSource<bool>();
                currentTaskToExecute();
                currentTaskToExecute = null;

                onCompleted(this);
            }
        }

        private Action currentTaskToExecute;

        public void Issue(Action taskToExecute)
        {
            currentTaskToExecute = taskToExecute;
            System.Threading.Thread t = new Thread(() => taskIssuedSource.SetResult(true));
            t.Start();
        }

        public void Dispose()
        {
            try
            {
                thread.Abort();
            }
            catch { }
            currentTaskToExecute = null;
            onCompleted = null;
            taskIssuedSource.SetResult(false);
            taskIssuedSource = null;
            thread = null;
        }
    }

}
