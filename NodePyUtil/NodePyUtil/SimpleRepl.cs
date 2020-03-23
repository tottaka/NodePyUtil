using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;

namespace NodePyUtil
{
    public class SimpleRepl : IDisposable
    {
        public readonly string Port;
        public readonly int Timeout;
        public readonly int BaudRate;

        public bool IsDisposed { get; private set; }

        private readonly SerialPort Connection;
        private readonly object ThreadLock = new object();

        public delegate string ReplExecDelegate(string command, int timeout = int.MaxValue);

        public SimpleRepl(string portName, int baudRate = 115200, int timeout = int.MaxValue)
        {
            Port = portName;
            Timeout = timeout;
            BaudRate = baudRate;
            Connection = new SerialPort(Port, BaudRate) { ReadTimeout = 1, NewLine = "\r\n" };
            Connection.Open();

            Thread.Sleep(1000);
            Connection.WriteLine("");

            // wait for repl to be ready
            ReadUntil(">>>", Timeout);
        }

        ~SimpleRepl()
        {
            Dispose();
        }

        public void Dispose()
        {
            lock (ThreadLock)
            {
                if (!IsDisposed)
                {
                    if (Connection.IsOpen)
                        Connection.Close();
                    Connection?.Dispose();

                    IsDisposed = true;
                }
            }
        }

        public void ExecuteRaw(Action<ReplExecDelegate> execFunction)
        {
            lock (ThreadLock)
            {
                EnterRawRepl();
                Thread.Sleep(100);

                try
                {
                    execFunction(Exec);
                    ExitRawRepl();
                }
                catch (Exception ex)
                {
                    ExitRawRepl();
                    throw ex;
                }
            }
        }

        /// <summary>
        /// Enters raw mode, executes the command, then exits raw mode and returns the output.
        /// </summary>
        public string ExecuteRaw(string command, int timeout = 0)
        {
            lock (ThreadLock)
            {
                if (timeout <= 0)
                    timeout = Timeout;

                EnterRawRepl();
                Thread.Sleep(100);

                try
                {
                    string output = Exec(command, timeout);
                    ExitRawRepl();
                    return output;
                }
                catch (Exception ex)
                {
                    ExitRawRepl();
                    throw ex;
                }
            }
        }

        private string ReadUntil(string value, int timeout)
        {
            lock (ThreadLock)
            {
                long millisTimeout = 0;
                string buffer = string.Empty;
                while (!buffer.EndsWith(value))
                {
                    if (Connection.BytesToRead > 0)
                    {
                        buffer += (char)Connection.ReadChar();
                        millisTimeout = 0;
                    }
                    else
                    {
                        millisTimeout += 1;
                        if (millisTimeout >= timeout)
                            throw new TimeoutException($"Read operation timed out after {millisTimeout}ms.");

                        Thread.Sleep(1);
                    }
                }

                return buffer.Replace("\x04", string.Empty);
            }
        }

        private string WaitForEot(int timeout)
        {
            lock (ThreadLock)
            {
                // wait for normal output
                return ReadUntil("\x04", timeout);
            }
        }

        private string Exec(string command, int timeout)
        {
            Connection.Write($"{command}\x04");

            ReadUntil("OK", timeout);
            string output = WaitForEot(timeout).Trim();

            string error = ReadUntil("\x04", 100);
            if (!string.IsNullOrWhiteSpace(error))
                throw new InvalidOperationException(error);

            return output;
        }

        private void EnterRawRepl()
        {
            lock (ThreadLock)
            {
                // ctrl-C twice to interrupt any running program
                Connection.Write("\r\x03");
                Thread.Sleep(100);
                Connection.Write("\r\x03");
                Thread.Sleep(100);

                Connection.DiscardInBuffer();
                Connection.Write("\r\x01"); // ctrl-A to enter raw REPL
                ReadUntil("raw REPL; CTRL-B to exit\r\n>", Timeout);
            }
        }

        private void ExitRawRepl()
        {
            lock (ThreadLock)
            {
                // ctrl-B to enter friendly REPL
                Connection.Write("\r\x02");
            }
        }
    }
}
