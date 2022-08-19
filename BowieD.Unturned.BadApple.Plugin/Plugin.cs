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
        private const long TICKDELAY_5FPS = TICKDELAY_15FPS * 3;
        private const long TICKDELAY_15FPS = TICKDELAY_30FPS * 2;
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
        static Stack<Transform>[] pools;
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

            int resX = Instance.Configuration.Instance.ResolutionX;
            int resY = Instance.Configuration.Instance.ResolutionY;

            if (screen != null)
            {
                DestroyScreen();
            }

            screen = new Transform[resX, resY];
            pools = new Stack<Transform>[3]
            {
                new Stack<Transform>(resX * resY),
                new Stack<Transform>(resX * resY),
                new Stack<Transform>(resX * resY),
            };
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

                    Quaternion rot = Quaternion.Euler(-90f, 0f, 0f);

                    Transform 
                        blackT = BarricadeManager.dropNonPlantedBarricade(new Barricade(blackPixel), resultPos, rot, 0, 0), 
                        grayT = BarricadeManager.dropNonPlantedBarricade(new Barricade(grayPixel), resultFakePos, rot, 0, 0), 
                        whiteT = BarricadeManager.dropNonPlantedBarricade(new Barricade(whitePixel), resultFakePos, rot, 0, 0);

                    screen[x, invY] = blackT;

                    pools[1].Push(grayT);
                    pools[2].Push(whiteT);

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

                    while (frame.poses.Count > 0)
                    {
                        var p = frame.poses.Pop();

                        int invY = resY - p.y - 1;

                        Transform t = screen[p.x, invY];
                        Transform fakeT = pools[p.type].Pop();

                        Vector3 oldPos = t.localPosition;
                        Quaternion oldRot = t.localRotation;

                        Vector3 oldFakePos = fakeT.localPosition;
                        Quaternion oldFakeRot = fakeT.localRotation;

                        pools[currentFramePixels[p.x, invY]].Push(t);

                        screen[p.x, invY] = fakeT;
                        currentFramePixels[p.x, invY] = p.type;

                        BarricadeManager.ServerSetBarricadeTransform(t, oldFakePos, oldFakeRot);
                        BarricadeManager.ServerSetBarricadeTransform(fakeT, oldPos, oldRot);
                    }

                    await Task.Yield();

                    frameDelays.Push(sw.ElapsedTicks);

                    while (sw.ElapsedTicks < TICKDELAY_5FPS)
                    {

                    }

                    realDelays.Push(sw.ElapsedTicks);
                }

                if (soundEffect > 0)
                    EffectManager.sendEffect(soundEffect, EffectManager.LARGE, screen[0, resY - 1].position);

                if (Instance.Configuration.Instance.IsTimeLapseMode)
                {
                    foreach (var sp in Provider.clients)
                    {
                        if (sp?.player is null)
                            continue;

                        sp.player.skills.askSpend(sp.player.skills.experience);
                    }
                }

                while (frames.Count > 0)
                {
                    var curFrame = frames.Dequeue();

                    await drawFrame(curFrame);

                    if (Instance.Configuration.Instance.IsTimeLapseMode)
                    {
                        foreach (var sp in Provider.clients)
                        {
                            if (sp?.player is null)
                                continue;

                            sp.player.skills.askAward(1);
                        }
                    }

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
                sw.WriteLine("ticks\tms\tfps\treal ticks\treal ms\treal fps");

                while (frameDelays.Count > 0)
                {
                    var delay = frameDelays.Pop();
                    var realDelay = realDelays.Pop();

                    sw.WriteLine($"{delay}\t{delay / 10000.0:0.##}\t{(1000.0 / (delay / 10000.0)):0.#}" +
                        $"\t{realDelay}\t{realDelay / 10000.0:0.##}\t{(1000.0 / (realDelay / 10000.0)):0.#}");
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

            foreach (var pool in pools)
                clearPool(pool);
        }
    }
}
