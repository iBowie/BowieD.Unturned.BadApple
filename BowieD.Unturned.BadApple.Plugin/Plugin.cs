using Rocket.Core.Plugins;
using SDG.Unturned;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BowieD.Unturned.BadApple.Plugin
{
    public sealed class Plugin : RocketPlugin<PluginConfiguration>
    {
        private const long TICKDELAY_30FPS = 333333;
        private const long TICKDELAY_60FPS = 166666;

        internal static Plugin Instance { get; private set; }

        protected override void Load()
        {
            Instance = this;
        }

        protected override void Unload()
        {
            if (screen != null)
                DestroyScreen();

            screen = null;

            Instance = null;
        }

        static Transform[,] screen;
        static Stack<Transform> whitePool, blackPool, grayPool, darkBrownPool;
        static byte[,] currentFramePixels;
        public static bool isPlaying, isReady, demandStop;
        public readonly static Queue<Frame> frames = new Queue<Frame>();
        private static Stack<long> frameDelays = new Stack<long>();
        private static Stack<long> realDelays = new Stack<long>();
        public static void InitScreenAt(Vector3 position)
        {
            Vector3 spacing = Instance.Configuration.Instance.Spacing;

            ushort whitePixel = Instance.Configuration.Instance.WhitePixel;
            ushort blackPixel = Instance.Configuration.Instance.BlackPixel;
            ushort grayPixel = Instance.Configuration.Instance.GrayPixel;
            ushort darkBrownPixel = Instance.Configuration.Instance.DarkBrownPixel;

            int resX = Instance.Configuration.Instance.ResolutionX;
            int resY = Instance.Configuration.Instance.ResolutionY;

            if (screen != null)
            {
                DestroyScreen();
            }

            screen = new Transform[resX, resY];
            whitePool = new Stack<Transform>(resX * resY);
            blackPool = new Stack<Transform>(resX * resY);
            grayPool = new Stack<Transform>(resX * resY);
            darkBrownPool = new Stack<Transform>(resX * resY);
            currentFramePixels = new byte[resX, resY];

            for (int x = 0; x < resX; x++)
            {
                for (int y = 0; y < resY; y++)
                {
                    int invY = resY - y - 1;

                    Vector3 offset = new Vector3(spacing.x * x, spacing.y * invY, 0f);

                    Vector3 fakeOffset = new Vector3(0f, -spacing.y * resY, 0f);

                    Vector3 resultPos = position + offset;
                    Vector3 resultFakePos = resultPos + fakeOffset;

                    Quaternion rot = Quaternion.Euler(90f, 0f, 0f);

                    Transform 
                        blackT = BarricadeManager.dropNonPlantedBarricade(new Barricade(blackPixel), resultPos, rot, 0, 0), 
                        grayT = BarricadeManager.dropNonPlantedBarricade(new Barricade(grayPixel), resultFakePos, rot, 0, 0), 
                        darkBrownT = BarricadeManager.dropNonPlantedBarricade(new Barricade(darkBrownPixel), resultFakePos, rot, 0, 0), 
                        whiteT = BarricadeManager.dropNonPlantedBarricade(new Barricade(whitePixel), resultFakePos, rot, 0, 0);

                    screen[x, invY] = blackT;
                    
                    whitePool.Push(whiteT);
                    grayPool.Push(grayT);
                    darkBrownPool.Push(darkBrownT);

                    currentFramePixels[x, invY] = 0;
                }
            }
        }
        public static async Task PlayBadApple()
        {
            if (isReady && !isPlaying)
            {
                ushort soundEffect = Instance.Configuration.Instance.SoundEffectID;

                int resX = Instance.Configuration.Instance.ResolutionX;
                int resY = Instance.Configuration.Instance.ResolutionY;

                isPlaying = true;

                Stopwatch sw = new Stopwatch();

                async Task drawFrame(Frame frame)
                {
                    sw.Restart();

                    foreach (var p in frame.poses)
                    {
                        int invY = resY - p.y - 1;

                        Transform t = screen[p.x, invY];

                        if (p.type != currentFramePixels[p.x, invY])
                        {
                            Transform fakeT;

                            switch (p.type)
                            {
                                case 0: // black
                                    fakeT = blackPool.Pop();
                                    break;
                                case 1: // dark brown
                                    fakeT = darkBrownPool.Pop();
                                    break;
                                case 2: // gray
                                    fakeT = grayPool.Pop();
                                    break;
                                case 3: // white
                                    fakeT = whitePool.Pop();
                                    break;
                                default:
                                    throw new System.Exception();
                            }

                            Vector3 oldPos = t.localPosition;
                            Quaternion oldRot = t.localRotation;

                            Vector3 oldFakePos = fakeT.localPosition;
                            Quaternion oldFakeRot = fakeT.localRotation;

                            switch (currentFramePixels[p.x, invY])
                            {
                                case 0: // go to black pool
                                    blackPool.Push(t);
                                    break;
                                case 1: // go to dark brown pool
                                    darkBrownPool.Push(t);
                                    break;
                                case 2: // go to gray pool
                                    grayPool.Push(t);
                                    break;
                                case 3: // go to white pool
                                    whitePool.Push(t);
                                    break;
                                default:
                                    throw new System.Exception();
                            }

                            screen[p.x, invY] = fakeT;
                            currentFramePixels[p.x, invY] = p.type;

                            BarricadeManager.ServerSetBarricadeTransform(t, oldFakePos, oldFakeRot);
                            BarricadeManager.ServerSetBarricadeTransform(fakeT, oldPos, oldRot);
                        }
                    }

                    await Task.Yield();

                    frameDelays.Push(sw.ElapsedTicks);

                    while (sw.ElapsedTicks < TICKDELAY_30FPS)
                    {

                    }

                    realDelays.Push(sw.ElapsedTicks);
                }

                if (soundEffect > 0)
                    EffectManager.sendEffect(soundEffect, EffectManager.LARGE, screen[0, resY - 1].position);

                while (frames.Count > 0)
                {
                    var curFrame = frames.Dequeue();

                    await drawFrame(curFrame);

                    if (demandStop)
                    {
                        demandStop = false;

                        foreach (var sp in Provider.clients)
                        {
                            if (sp == null || sp.player == null)
                                continue;

                            if (soundEffect > 0)
                                EffectManager.askEffectClearByID(soundEffect, sp.playerID.steamID);
                        }

                        break;
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter(Path.Combine(Plugin.Instance.Directory, "frames.txt"), false))
            {
                while (frameDelays.Count > 0)
                {
                    var delay = frameDelays.Pop();
                    var realDelay = realDelays.Pop();

                    sw.WriteLine($"{delay} ticks\t{delay / 10000.0:0.##} ms\t{(1000.0 / (delay / 10000.0)):0.#} fps" +
                        $"\t{realDelay} real ticks\t{realDelay / 10000.0:0.##} real ms\t{(1000.0 / (realDelay / 10000.0)):0.#} real fps");
                }
            }

            isPlaying = false;
            isReady = false;
            frames.Clear();
            DestroyScreen();
        }
        public static void PrepareBadApple()
        {
            string file = Path.Combine(Instance.Directory, "badApple.dat");

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
                            f.poses.Push(new Pos(x, y, typeData));
                        }
                    }

                    frames.Enqueue(f);
                }
            }

            frameDelays = new Stack<long>(frames.Count);
            realDelays = new Stack<long>(frames.Count);
            isReady = true;
        }
        public static void DestroyScreen()
        {
            foreach (var t in screen)
            {
                if (BarricadeManager.tryGetInfo(t, out byte x, out byte y, out ushort plant, out ushort index, out var region))
                {
                    BarricadeManager.destroyBarricade(region, x, y, plant, index);
                }
            }

            void clearPool(Stack<Transform> pool)
            {
                while (pool.Count > 0)
                {
                    var t = pool.Pop();

                    if (BarricadeManager.tryGetInfo(t, out byte x, out byte y, out ushort plant, out ushort index, out var region))
                    {
                        BarricadeManager.destroyBarricade(region, x, y, plant, index);
                    }
                }
            }

            clearPool(whitePool);
            clearPool(grayPool);
            clearPool(darkBrownPool);
            clearPool(blackPool);
        }
    }
}
