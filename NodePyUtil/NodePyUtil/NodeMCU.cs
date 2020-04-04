using System;
using System.Collections.Generic;
using System.Linq;
using System.IO.Ports;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace NodePyUtil
{
    public class NodeMCU : IDisposable
    {
        public readonly string Port;

        public bool IsDisposed { get; private set; }

        public string WorkingDirectory => Run("import uos\nprint( uos.getcwd() )");

        private readonly SimpleRepl Repl;
        private readonly object ThreadLock = new object();

        public NodeMCU(string port, int baudRate = 115200, int timeout = int.MaxValue)
        {
            if (!SerialPort.GetPortNames().Contains(port))
                throw new ArgumentException("Invalid port name.", "port");

            Port = port;
            Repl = new SimpleRepl(Port, baudRate, timeout);
        }

        ~NodeMCU()
        {
            Dispose();
        }

        public void Dispose()
        {
            lock (ThreadLock)
            {
                if (!IsDisposed)
                {
                    Repl?.Dispose();
                    IsDisposed = true;
                }
            }
        }

        public string Echo(string message)
        {
            lock (ThreadLock)
            {
                return Run($"print( '{ message.Replace("'", "\\'") }' )");
            }
        }


        public void Rename(string path, string newPath)
        {
            lock (ThreadLock)
            {
                Run($"import uos\nuos.rename( '{path}', '{newPath}' )");
            }
        }

        public void CreateDirectory(string path)
        {
            lock (ThreadLock)
            {
                Run($"import uos\nuos.mkdir( '{ path }' )");
            }
        }

        public void DeleteDirectory(string path)
        {
            lock (ThreadLock)
            {
                Console.WriteLine("Delete Directory: {0}", path);
                List<NodeFile> entries = List(path);
                foreach (NodeFile entry in entries)
                {
                    if (entry.IsDirectory)
                        DeleteDirectory(path + "/" + entry.Name);
                    else
                        DeleteFile(path + "/" + entry.Name);
                }

                Run($"import uos\nuos.rmdir( '{ path }' )");
            }
        }

        public void CreateFile(string path)
        {
            lock (ThreadLock)
            {
                Run($"with open( '{ path }', 'w' ) as fp:\n\tpass");
            }
        }

        public void DeleteFile(string path)
        {
            lock (ThreadLock)
            {
                Console.WriteLine("Delete File: {0}", path);
                Run($"import uos\nuos.remove( '{ path }') ");
            }
        }

        public bool Exists(string path)
        {
            lock (ThreadLock)
            {
                try
                {
                    Run($"import os\nos.stat( '{ path }' )");
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
        }

        public void Download(string path, string localPath, int chunkSize = 128)
        {
            lock (ThreadLock)
            {
                string pathName = Path.GetDirectoryName(localPath);
                if (!Directory.Exists(pathName))
                    Directory.CreateDirectory(pathName);

                using (FileStream stream = File.Create(localPath))
                {
                    Execute(exec => {
                        exec("import sys\nimport ubinascii");
                        exec($"infile = open('{path}', 'rb')");

                        string buffer = string.Empty;
                        do {
                            buffer = exec($"try:\n\tprint( ubinascii.b2a_base64( infile.read( {chunkSize} ) ).strip() )\nexcept Exception as ex:\n\tinfile.close()\n\traise ex");
                            buffer = buffer.Remove(0, 2).TrimEnd('\'');
                            byte[] data = Convert.FromBase64String(buffer);
                            stream.Write(data, 0, data.Length);
                        } while (!string.IsNullOrWhiteSpace(buffer));
                        exec("infile.close()");
                    });
                }
            }
        }

        public void Upload(string localPath, string remotePath, int chunkSize = 128)
        {
            lock (ThreadLock)
            {
                using (FileStream stream = File.OpenRead(localPath))
                {
                    Execute(exec => {
                        exec("import sys\nimport ubinascii");
                        exec($"infile = open('{remotePath}', 'wb')");



                        byte[] buffer = new byte[chunkSize];
                        int read = stream.Read(buffer, 0, buffer.Length);

                        do {
                            exec($"try:\n\tinfile.write( b'{ buffer.ToHex(0, read) }' )\nexcept Exception as ex:\n\tinfile.close()\n\traise ex");
                        } while ((read = stream.Read(buffer, 0, buffer.Length)) > 0);
                        exec("infile.close()");
                    });
                }
            }
        }

        public List<NodeFile> List(string path)
        {
            lock (ThreadLock)
            {
                List<NodeFile> files = new List<NodeFile>();
                Execute(exec => {
                    exec("import os\nimport ujson\nresults = []");

                    string[] fileList = exec($"print( '\\r\\n'.join( os.listdir( '{ path }' ) ) )").Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < fileList.Length; i++)
                        fileList[i] = fileList[i].Trim();

                    foreach(string file in fileList)
                    {
                        try
                        {
                            // if its a directory, then it should provide some children.
                            exec($"os.listdir( '{ (path.EndsWith("/") ? path : path + "/") + file }' )");
                            files.Add(new NodeFile { Name = file, IsDirectory = true });
                        }
                        catch (InvalidOperationException)
                        {
                            // # probably a file. run stat() to confirm.
                            exec($"os.stat( '{ (path.EndsWith("/") ? path : path + "/") + file }' )");
                            files.Add(new NodeFile { Name = file, IsDirectory = false });
                        }
                    }
                });

                return files;
            }
        }

        public void Reset()
        {
            lock (ThreadLock)
            {
                Run("import machine\nmachine.reset()");
            }
        }

        public void Execute(Action<SimpleRepl.ReplExecDelegate> exeCallback)
        {
            lock (ThreadLock)
            {
                Repl.ExecuteRaw(exeCallback);
            }
        }


        public void Execute(string command, Action<string> callback, Action exit, ManualResetEventSlim resetEvent)
        {
            lock (ThreadLock)
            {
                Repl.ExecuteRaw(command, callback, exit, resetEvent);
            }
        }

        public string ExecuteFile(string path)
        {
            lock (ThreadLock)
            {
                return Run($"exec(open( '{ path }' ).read())");
            }
        }

        public string Run(string command)
        {
            lock (ThreadLock)
            {
                return Repl.ExecuteRaw(command, 0);
            }
        }
    }

    public class NodeFile
    {
        public string Name { get; set; }
        public bool IsDirectory { get; set; }
    }

    public static class PyHex
    {
        public static string ToHex(this byte[] data, int startIndex, int length)
        {
            string hexString = string.Empty;
            for (int i = startIndex; i < startIndex + length; i++)
                hexString += "\\x" + data[i].ToString("X2");
            return hexString;
        }
    }
}
