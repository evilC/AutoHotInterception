using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AutoHotInterception
{
    class WorkerThread : IDisposable
    {
        private Task _worker;
        private CancellationTokenSource _cancellationToken;
        public BlockingCollection<Action> Actions { get; }

        public WorkerThread()
        {
            Actions = new BlockingCollection<Action>();
            _cancellationToken = new CancellationTokenSource();
            _worker = new Task(Run, _cancellationToken.Token);
            _worker.Start();
        }

        private void Run(Object obj)
        {
            var token = (CancellationToken)obj;
            while (!token.IsCancellationRequested)
            {
                var action = Actions.Take();
                action.Invoke();
            }
        }

        public void Dispose()
        {
            _cancellationToken.Cancel();
            _cancellationToken.Dispose();
        }
    }
}
