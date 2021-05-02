namespace EasyProc
{
    public class CommandProgress
    {
        internal CommandProgress(string text, bool done)
        {
            Text = text;
            Done = done;
        }

        public string Text { get; }
        public bool Done { get; }
    }
}
