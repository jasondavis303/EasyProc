using System;

namespace EasyProc
{
    public class CommandOutputEventArgs : EventArgs
    {
        internal CommandOutputEventArgs(string text, DateTime started)
        {
            Text = text;
            RunTime = DateTime.Now - started;
        }

        public string Text { get; }

        public TimeSpan RunTime { get; }
    }
}
