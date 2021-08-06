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
                sizeX = 4 * 5 * 2, 
                sizeY = 3 * 5 * 2;

            byte[,] current = new byte[sizeX, sizeY];
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

                        Bitmap resized = new Bitmap(bmp, new Size(sizeX, sizeY));

                        for (int x = 0; x < sizeX; x++)
                        {
                            for (int y = 0; y < sizeY; y++)
                            {
                                var p = resized.GetPixel(x, y);

                                float b = p.GetBrightness();

                                if (b >= 0.67f) // white
                                {
                                    if (current[x, y] != 0) // prev is not white
                                    {
                                        outp.Write($"{x}_{y} 0\t");
                                        current[x, y] = 0;
                                    }
                                }
                                else if (b >= 0.33f) // gray
                                {
                                    if (current[x,y] != 2) // prev is not gray
                                    {
                                        outp.Write($"{x}_{y} 2\t");
                                        current[x, y] = 2;
                                    }
                                }
                                else // black
                                {
                                    if (current[x, y] != 1) // prev is not black
                                    {
                                        outp.Write($"{x}_{y} 1\t");
                                        current[x, y] = 1;
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
