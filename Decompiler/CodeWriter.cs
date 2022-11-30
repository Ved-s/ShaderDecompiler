using System.Text;

namespace ShaderDecompiler.Decompiler
{
    public class CodeWriter
    {
        public StringBuilder Builder = new();

        public bool LineStart = true;
        public bool LastSpace = true;
        Stack<string> Blocks = new();

        public void Write(string str)
        {
            WriteLineStart();
            Builder.Append(str);
            LastSpace = str.EndsWith(" ");
            if (str.EndsWith('\n'))
            {
                LineStart = true;
                LastSpace = true;
            }
        }

        public void WriteSpaced(string str)
        {
            WriteLineStart();
            if (!LastSpace)
                Builder.Append(' ');
            Write(str);
        }

        public void NewLine()
        {
            Builder.Append('\n');
            LineStart = true;
            LastSpace = true;
        }

        public void StartBlock(string start = "{", string end = "}")
        {
            Write(start);
            Blocks.Push(end);
        }

        public void EndBlock()
        {
            Write(Blocks.Pop());
        }

        void WriteLineStart()
        {
            if (!LineStart)
                return;

            LineStart = false;
            for (int i = 0; i < Blocks.Count; i++)
                Builder.Append('\t');
            LastSpace = true;
        }

        public override string ToString()
        {
            return Builder.ToString();
        }
    }
}
