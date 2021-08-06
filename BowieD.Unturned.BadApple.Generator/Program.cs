using Accord.Video.FFMPEG;
using System;
using System.Drawing;
using System.IO;
using System.Text;

namespace BowieD.Unturned.BadApple.Generator
{
    class Program
    {
        static void Main(string[] args)
        {
            int 
                sizeX = 4 * 5 * 2, // 40
                sizeY = 3 * 5 * 2; // 30

            byte[,] current = new byte[sizeX, sizeY];
            const string file = "badApple.mp4";
            
            if (File.Exists(file))
            {
                using (var vfreader = new VideoFileReader())
                using (var outp = new StreamWriter("result.dat", false, Encoding.UTF8))
                {
                    vfreader.Open(file);

                    int index = 0;
                    int steps = 3; // 0 - darkest, 4 - lightest

                    while (true)
                    {
                        Bitmap bmp = vfreader.ReadVideoFrame();

                        if (bmp == null)
                            break;

                        Bitmap resized = new Bitmap(bmp, new Size(sizeX, sizeY));

                        for (int x = 0; x < sizeX; x++)
                        {
                            for (int y = 0; y < sizeY; y++)
                            {
                                var p = resized.GetPixel(x, y);

                                float b = p.GetBrightness();

                                byte val = (byte)Math.Floor(b * steps);

                                if (current[x,y] != val)
                                {
                                    outp.Write($"{x}_{y} {val}\t");
                                    current[x, y] = val;
                                }
                            }
                        }

                        outp.WriteLine();
                        Console.WriteLine(index++);
                    }

                    vfreader.Close();
                }
            }
            else
            {
                Console.WriteLine("File not found.");
            }

            Console.ReadKey(true);
        }
    }
}
