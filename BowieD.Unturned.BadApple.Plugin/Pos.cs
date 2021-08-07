namespace BowieD.Unturned.BadApple.Plugin
{
    public struct Pos
    {
        public Pos(int x, int y, byte type)
        {
            this.x = x;
            this.y = y;
            this.type = type;
        }

        public readonly int x;
        public readonly int y;
        public readonly byte type;
    }
}
