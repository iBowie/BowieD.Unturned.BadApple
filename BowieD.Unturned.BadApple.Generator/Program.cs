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
            bool[,] current = new bool[30, 14];
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

                        Bitmap resized = new Bitmap(bmp, new Size(30, 14));

                        for (int x = 0; x < 30; x++)
                        {
                            for (int y = 0; y < 14; y++)
                            {
                                var p = resized.GetPixel(x, y);

                                float b = p.GetBrightness();

                                if (b >= 0.5f) // white
                                {
                                    if (current[x, y] == true) // prev is black
                                    {
                                        outp.Write($"{x}_{y} 0\t");
                                        current[x, y] = false;
                                    }
                                }
                                else // black
                                {
                                    if (current[x, y] == false) // prev is white
                                    {
                                        outp.Write($"{x}_{y} 1\t");
                                        current[x, y] = true;
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
