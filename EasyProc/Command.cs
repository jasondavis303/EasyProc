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
        public event EventHandler<CommandCompleteEventArgs> OnComplete;

        Process _process = null;
        DateTime _started;

        public Command() { }

        ~Command()
        {
            try { CancelProcess(); }
            catch { }
        }

        private void CancelProcess()
        {
            if (_process != null && !_process.HasExited)
                _process.Kill(true);
        }

        public Task<int> RunAsync(string program, string args = null, CancellationToken cancellationToken = default) => RunAsync(program, args, null, null, cancellationToken);

        public async Task<int> RunAsync(string program, string args = null, IProgress<CommandProgress> stdOutProgress = null, IProgress<CommandProgress> stdErrProgress = null, CancellationToken cancellationToken = default)
        {
            if (_process != null)
                throw new Exception("This instance is already running");

            _started = DateTime.Now;
            _process = new Process()
            {
                StartInfo = new ProcessStartInfo(program, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            int ret;
            using (cancellationToken.Register(() => CancelProcess()))
            {
                _process.Start();

                Task stdoutTask = Task.Run(() => RedirectedThread(_process.StandardOutput, OnStdOut, stdOutProgress));
                Task stderrTask = Task.Run(() => RedirectedThread(_process.StandardError, OnStdErr, stdErrProgress));

                Task proc = Task.Run(() => _process.WaitForExit());

                await Task.WhenAll(proc, stdoutTask, stderrTask).ConfigureAwait(false);

                ret = _process.ExitCode;
                _process.Dispose();
                _process = null;

                cancellationToken.ThrowIfCancellationRequested();
            }

            OnComplete?.Invoke(this, new CommandCompleteEventArgs(ret, _started));
            stdOutProgress?.Report(new CommandProgress(null, true, _started));
            stdErrProgress?.Report(new CommandProgress(null, true, _started));

            return ret;
        }

        private void RedirectedThread(StreamReader sr, EventHandler<CommandOutputEventArgs> eh, IProgress<CommandProgress> progress)
        {
            if (sr == null && progress == null)
                return;

            string line;
            while ((line = sr.ReadLine()) != null)
            {
                eh?.Invoke(this, new CommandOutputEventArgs(line, _started));
                progress?.Report(new CommandProgress(line, false, _started));
            }
        }
    }
}
