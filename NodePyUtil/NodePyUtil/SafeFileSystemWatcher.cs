using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NodePyUtil
{
    public class SafeFileSystemWatcher : IDisposable
    {
        public int Threshold = 1000;
        public bool IsDisposed { get; private set; }

        public event EventHandler<FileSystemEventArgs> BeforeChange;
        public event EventHandler<FileSystemEventArgs> AfterChange;

        private readonly object ThreadLock = new object();
        private FileSystemWatcher Watcher;
        private Stopwatch Timer = new Stopwatch();
        private bool Waiting = false;

        public SafeFileSystemWatcher(string path, string filter = "")
        {
            Watcher = new FileSystemWatcher(path, filter) { EnableRaisingEvents = true, NotifyFilter = NotifyFilters.LastWrite };
            Watcher.Changed += Watcher_Changed;
        }

        ~SafeFileSystemWatcher()
        {
            Dispose();
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            lock (ThreadLock)
            {
                Timer.Restart();

                if (!Waiting)
                {
                    Waiting = true;
                    BeforeChange?.Invoke(this, e);
                    Task.Run(() => {
                        while (true)
                        {
                            lock (ThreadLock)
                            {
                                Waiting = true;
                                if (Timer.ElapsedMilliseconds >= Threshold)
                                    break;
                            }

                            Thread.Sleep(1);
                        }

                        Waiting = false;
                        AfterChange?.Invoke(this, e);
                    });
                }
            }
        }

        public void Dispose()
        {
            lock (ThreadLock)
            {
                if (!IsDisposed)
                {
                    Watcher?.Dispose();
                    Watcher = null;
                    Timer = null;
                    IsDisposed = true;
                }
            }
        }
    }
}
