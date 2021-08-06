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
            bool[,] current = new bool[4 * 5, 3 * 5];
            const string file = "badApple.mp4";
            
            if (File.Exists(file))
            {
                using (var vfreader = new VideoFileReader())
                using (var outp = new StreamWriter("result.dat", false, Encoding.UTF8))
                {
                    vfreader.Open(file);

                    int index = 0;

                    while (true)
                    {
                        Bitmap bmp = vfreader.ReadVideoFrame();

                        if (bmp == null)
                            break;

                        Bitmap resized = new Bitmap(bmp, new Size(4 * 5, 3 * 5));

                        for (int x = 0; x < 4 * 5; x++)
                        {
                            for (int y = 0; y < 3 * 5; y++)
                            {
                                var p = resized.GetPixel(x, y);

                                float b = p.GetBrightness();

                                if (b >= 0.5f) // white
                                {
                                    if (current[x, y] == false) // prev is black
                                    {
                                        outp.Write($"{x}_{y} 0\t");
                                        current[x, y] = true;
                                    }
                                }
                                else // black
                                {
                                    if (current[x, y] == true) // prev is white
                                    {
                                        outp.Write($"{x}_{y} 1\t");
                                        current[x, y] = false;
                                    }
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
