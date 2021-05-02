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
        public event EventHandler OnComplete;

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

        public Task<int> RunAsync(string program, string args = null, CancellationToken cancellationToken = default) => RunAsync(program, args, null, null, cancellationToken);

        public async Task<int> RunAsync(string program, string args = null, IProgress<CommandProgress> stdOutProgress = null, IProgress<CommandProgress> stdErrProgress = null, CancellationToken cancellationToken = default)
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

                Task stdoutTask = Task.Run(() => RedirectedThread(process.StandardOutput, OnStdOut, stdOutProgress));
                Task stderrTask = Task.Run(() => RedirectedThread(process.StandardError, OnStdErr, stdErrProgress));

                Task proc = Task.Run(() =>
                {
                    process.WaitForExit();
                });

                await Task.WhenAll(proc, stdoutTask, stderrTask).ConfigureAwait(false);

                int ret = process.ExitCode;
                process.Dispose();
                process = null;

                cancellationToken.ThrowIfCancellationRequested();

                OnComplete?.Invoke(this, EventArgs.Empty);
                stdOutProgress?.Report(new CommandProgress(null, true));
                stdErrProgress?.Report(new CommandProgress(null, true));

                return ret;
            }
        }

        private void RedirectedThread(StreamReader sr, EventHandler<CommandOutputEventArgs> eh, IProgress<CommandProgress> progress)
        {
            if (sr == null)
                return;

            string line;
            while ((line = sr.ReadLine()) != null)
            {
                eh?.Invoke(this, new CommandOutputEventArgs(line));
                progress?.Report(new CommandProgress(line, false));
            }
        }
    }
}
