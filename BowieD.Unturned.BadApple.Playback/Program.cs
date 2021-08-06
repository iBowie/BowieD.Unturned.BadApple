using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using WMPLib;

namespace BowieD.Unturned.BadApple.Playback
{
    class Program
    {
        readonly static Queue<Frame> frames = new Queue<Frame>();

        static void Main(string[] args)
        {
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
                                        f.poses.Push(new Pos(x, y, ConsoleColor.DarkGray));
                                        break;
                                    case 2:
                                        f.poses.Push(new Pos(x, y, ConsoleColor.DarkYellow));
                                        break;
                                    case 3:
                                        f.poses.Push(new Pos(x, y, ConsoleColor.Gray));
                                        break;
                                    case 4:
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

            DateTime drawTime = DateTime.UtcNow;

            foreach (var p in frame.poses)
            {
                Console.SetCursorPosition(p.x, p.y);
                Console.BackgroundColor = p.color;
                Console.Write(' ');
            }

            long snap = stopwatch.ElapsedTicks;

            while (stopwatch.ElapsedTicks < 333333)
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
