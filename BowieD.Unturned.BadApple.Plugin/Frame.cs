using System.Collections.Generic;

namespace BowieD.Unturned.BadApple.Plugin
{
    public class Frame
    {
        public Frame()
        {
            poses = new Stack<Pos>();
        }

        public readonly Stack<Pos> poses;
    }
}
