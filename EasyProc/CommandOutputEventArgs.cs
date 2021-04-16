using System;

namespace EasyProc
{
    public class CommandOutputEventArgs : EventArgs
    {
        public string Text { get; private set; }

        public CommandOutputEventArgs(string text)
        {
            Text = text;
        }
    }
}
