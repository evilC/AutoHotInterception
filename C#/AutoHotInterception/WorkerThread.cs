using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoHotInterception
{
    class WorkerThread : IDisposable
    {
        private readonly Thread _worker;
        private volatile bool _running;

        public WorkerThread()
        {
            Actions = new BlockingCollection<Action>();
            _worker = new Thread(Run);
            _running = false;
        }

        public BlockingCollection<Action> Actions { get; }

        public void Dispose()
        {
            if (!_running) return;
            _running = false;
            _worker.Join();
        }

        public void Start()
        {
            if (_running) return;
            _running = true;
            _worker.Start();
        }

        private void Run()
        {
            while (_running)
            {
                var action = Actions.Take();
                action.Invoke();
            }
        }
    }
}
