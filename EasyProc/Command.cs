using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EasyProc
{
    public class Command
    {
        public event EventHandler<CommandOutputEventArgs> OnStdOut;
        public event EventHandler<CommandOutputEventArgs> OnStdErr;

        Process process = null;
       
        public Command() { }

        ~Command()
        {
            try { CancelProcess(); }
            catch { }
        }

        private void CancelProcess()
        {
            if (process != null && !process.HasExited)
                process.Kill();
        }
        
        public async Task<int> RunAsync(string program, string args = null, CancellationToken cancellationToken = default)
        {
            if (process != null)
                throw new Exception("This instance is already running");

            using (cancellationToken.Register(() => CancelProcess()))
            {

                process = new Process()
                {
                    StartInfo = new ProcessStartInfo(program, args)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                process.PriorityClass = ProcessPriorityClass.BelowNormal;

                Task stdoutTask = Task.Run(() => RedirectedThread(process.StandardOutput, OnStdOut));
                Task stderrTask = Task.Run(() => RedirectedThread(process.StandardError, OnStdErr));

                Task proc = Task.Run(() =>
                {
                    process.WaitForExit();
                });

                await Task.WhenAll(proc, stdoutTask, stderrTask).ConfigureAwait(false);

                int ret = process.ExitCode;
                process.Dispose();
                process = null;

                cancellationToken.ThrowIfCancellationRequested();

                return ret;
            }
        }

        private void RedirectedThread(StreamReader sr, EventHandler<CommandOutputEventArgs> eh)
        {
            if (sr == null)
                return;

            string line;
            while ((line = sr.ReadLine()) != null)
            {
                eh?.Invoke(this, new CommandOutputEventArgs(line));
            }
        }
    }
}
