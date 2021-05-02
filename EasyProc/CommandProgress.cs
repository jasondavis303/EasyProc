using System;

namespace EasyProc
{
    public class CommandProgress
    {
        internal CommandProgress(string text, bool done, DateTime started)
        {
            Text = text;
            Done = done;
            RunTime = DateTime.Now - started;
        }

        public string Text { get; }
        public bool Done { get; }
        public TimeSpan RunTime { get; }
    }
}
