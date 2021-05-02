using System;

namespace EasyProc
{
    public class CommandCompleteEventArgs
    {
        internal CommandCompleteEventArgs(int exitCode, DateTime started)
        {
            ExitCode = exitCode;
            RunTime = DateTime.Now - started;
        }
        
        public int ExitCode { get; }

        public TimeSpan RunTime { get; }
    }
}
