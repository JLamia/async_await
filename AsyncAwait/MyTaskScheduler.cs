using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncAwait
{
    public enum MyTaskStatus
    {
        //Completed,
        InProgress,
        NotStarted
    }
    
    public class MyTask
    {
        private Action _action;

        private MyTaskStatus _taskStatus;

        public MyTaskStatus getStatus()
        {
            return _taskStatus;
        }

        public MyTask(Action action)
        {
            _action = action;
            _taskStatus = MyTaskStatus.NotStarted;
        }

        public Action getAction()
        {
            return _action;
        }
        
        public void Execute()
        {
            lock (this)
                _taskStatus = MyTaskStatus.InProgress;
            _action();
        }
    }
    
    public class MyThreadPool : IDisposable
    {
        private List<MyTask> _tasks;
        private List<Thread> _threads;

        private ManualResetEvent _stopEvent;
        private bool _isStopping;
        private object _stopLock;
        private Dictionary<int, ManualResetEvent> _threadsEvent;

        private ManualResetEvent _scheduleEvent;
        private Thread _scheduleThread;

        private bool _isDisposed;

        public MyThreadPool()
        {
            //_isStopping = isStopping;
            _tasks = new List<MyTask>();
            _threads = new List<Thread>();
            _isStopping = false;

            _stopLock = new object();
            _stopEvent = new ManualResetEvent(false);
            
            _scheduleEvent = new ManualResetEvent(false);
            _scheduleThread = new Thread(StartFreeThread) {Name = "Schedule Thread", IsBackground = true};
            _scheduleThread.Start();
            
            _threadsEvent = new Dictionary<int, ManualResetEvent>();
        }

        ~MyThreadPool()
        {
            Dispose(false);
        }

        private void StartFreeThread()
        {
            while (true)
            {
                _scheduleEvent.WaitOne();
                lock (_threads)
                {
                    foreach (var thread in _threads)
                    {
                        if (_threadsEvent[thread.ManagedThreadId].WaitOne((0)) == false)
                        {
                            _threadsEvent[thread.ManagedThreadId].Set();
                            break;
                        }
                    }
                }
                _scheduleEvent.Reset();
            }
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                _scheduleThread.Abort();
                _scheduleEvent.Dispose();

                foreach (var thread in _threads)
                {
                    thread.Abort();
                    _threadsEvent[thread.ManagedThreadId].Dispose();
                }
            }
            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /*protected internal void QueueTask(MyTask task)
        {
            Thread thread = new Thread(new ThreadStart(task.getAction()));
            thread.IsBackground = true; // Keep this thread from blocking process shutdown
            thread.Start();
            _threads.Add(thread);
        }*/

        private void ThreadWork()
        {
            while (true)
            {
                _threadsEvent[Thread.CurrentThread.ManagedThreadId].WaitOne();
                MyTask task = _tasks.First();
                if (_tasks.Count == 0) continue;
                try
                {
                    task.Execute();
                }
                finally
                {
                    RemoveTask(task);
                    if (_isStopping)
                        _stopEvent.Set();
                    _threadsEvent[Thread.CurrentThread.ManagedThreadId].Reset();
                }
            }
        }

        private void AddTask(MyTask task)
        {
            lock (_tasks) 
            {
                _tasks.Add(task);
            }
            _scheduleEvent.Set();
        }

        private void RemoveTask(MyTask task)
        {
            lock (_tasks)
            {
                _tasks.Remove(task);
            }

            if (_tasks.Count != 0 && _tasks.Any(t => t.getStatus() != MyTaskStatus.InProgress))
                _scheduleEvent.Set();
        }
        
        public bool Execute(MyTask task)
        {
            if (task == null) throw new ArgumentNullException();
            lock (_stopLock)
            {
                if (_isStopping) return false;
                
                AddTask(task);
                return true;
            }
        }

        public void Stop()
        {
            lock (_stopLock)
            {
                _isStopping = true;
            }

            while (_tasks.Count > 0)
            {
                _stopEvent.WaitOne();
                _stopEvent.Reset();
            }

            Dispose(true);
        }
    }
}