using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using WMPLib;

namespace BowieD.Unturned.BadApple.Playback
{
    class Program
    {
        private const int TICKS_30FPS = 333333, TICKS_60FPS = 166666;
        readonly static Queue<Frame> frames = new Queue<Frame>();

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern SafeFileHandle CreateFile(
            string fileName,
            [MarshalAs(UnmanagedType.U4)] uint fileAccess,
            [MarshalAs(UnmanagedType.U4)] uint fileShare,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] int flags,
            IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteConsoleOutput(
            SafeFileHandle hConsoleOutput,
            CharInfo[] lpBuffer,
            Coord dwBufferSize,
            Coord dwBufferCoord,
            ref SmallRect lpWriteRegion);

        [StructLayout(LayoutKind.Sequential)]
        public struct Coord
        {
            public short X;
            public short Y;

            public Coord(short X, short Y)
            {
                this.X = X;
                this.Y = Y;
            }
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct CharUnion
        {
            [FieldOffset(0)] public char UnicodeChar;
            [FieldOffset(0)] public byte AsciiChar;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct CharInfo
        {
            [FieldOffset(0)] public CharUnion Char;
            [FieldOffset(2)] public short Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SmallRect
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        static SafeFileHandle consoleHandle;
        static CharInfo[] buf;
        static SmallRect rect;

        [STAThread]
        static void Main(string[] args)
        {
            consoleHandle = CreateFile("CONOUT$", 0x40000000, 2, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);

            if (consoleHandle.IsInvalid)
                return;

            buf = new CharInfo[40 * 30];
            rect = new SmallRect()
            {
                Left = 0,
                Top = 0,
                Right = 40,
                Bottom = 30
            };

            for (int i = 0; i < buf.Length; i++)
                buf[i].Char.AsciiChar = (byte)' ';

            Console.OutputEncoding = Encoding.UTF8;

            const string file = "result.dat";

            if (File.Exists(file))
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    while (!sr.EndOfStream)
                    {
                        Frame f = new Frame();

                        string line = sr.ReadLine();

                        string[] fP = line.Split('\t');

                        foreach (var fpi in fP)
                        {
                            string[] fPdata = fpi.Split(' ');

                            if (fPdata.Length < 2)
                                continue;

                            string pos = fPdata[0];
                            string type = fPdata[1];

                            string[] poss = pos.Split('_');

                            if (int.TryParse(poss[0], out int x) &&
                                int.TryParse(poss[1], out int y) &&
                                byte.TryParse(type, out byte typeData))
                            {
                                switch (typeData)
                                {
                                    case 0:
                                        f.poses.Push(new Pos(x, y, ConsoleColor.Black));
                                        break;
                                    case 1:
                                        f.poses.Push(new Pos(x, y, ConsoleColor.Gray));
                                        break;
                                    case 2:
                                        f.poses.Push(new Pos(x, y, ConsoleColor.White));
                                        break;
                                }
                            }
                        }

                        frames.Enqueue(f);
                    }
                }

                Console.WriteLine("done reading. press any key to play.");
                Console.ReadKey(true);


                Console.WriteLine("starting in 3 seconds.");
                Thread.Sleep(3000);
                WindowsMediaPlayer wmp = new WindowsMediaPlayer();
                wmp.URL = "badApple.mp3";
                wmp.controls.play();
                Console.Clear();
                Console.CursorVisible = false;

                stopwatch = new Stopwatch();

                while (frames.Count > 0)
                {
                    var f = frames.Dequeue();

                    drawFrame(f);
                }

                Console.CursorVisible = true;
            }
            else
            {
                Console.WriteLine("File not found.");
            }

            Console.ReadKey(true);
        }

        static Stopwatch stopwatch;

        static void drawFrame(Frame frame)
        {
            stopwatch.Restart();

            foreach (var p in frame.poses)
            {
                buf[p.x + p.y * 40].Attributes = (short)((short)p.color << 4);
            }

            bool b = WriteConsoleOutput(consoleHandle, buf, new Coord(40, 30), new Coord(0, 0), ref rect);

            long snap = stopwatch.ElapsedTicks;

            while (stopwatch.ElapsedTicks < TICKS_30FPS)
            {

            }

            Console.Title = $"{snap / 10000f:0.0} ms / {stopwatch.ElapsedTicks / 10000f:0.0} ms | {(1000f / (stopwatch.ElapsedTicks / 10000f)):0.#} FPS";
        }

        public class Frame
        {
            public Frame()
            {
                poses = new Stack<Pos>();
            }

            public readonly Stack<Pos> poses;
        }
        public struct Pos
        {
            public Pos(int x, int y, ConsoleColor color)
            {
                this.x = x;
                this.y = y;
                this.color = color;
            }

            public readonly int x;
            public readonly int y;
            public readonly ConsoleColor color;
        }
    }
}
