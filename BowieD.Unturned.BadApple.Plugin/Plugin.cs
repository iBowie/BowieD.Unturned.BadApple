using Rocket.Core.Plugins;
using SDG.Unturned;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace BowieD.Unturned.BadApple.Plugin
{
    public sealed class Plugin : RocketPlugin<PluginConfiguration>
    {
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
        public static bool isPlaying, isReady, demandStop;
        public readonly static Queue<Frame> frames = new Queue<Frame>();
        public static void InitScreenAt(Vector3 position)
        {
            Vector3 spacing = Instance.Configuration.Instance.Spacing;

            ushort whitePixel = Instance.Configuration.Instance.WhitePixel;
            ushort blackPixel = Instance.Configuration.Instance.BlackPixel;

            int resX = Instance.Configuration.Instance.ResolutionX;
            int resY = Instance.Configuration.Instance.ResolutionY;

            if (screen != null)
            {
                DestroyScreen();
            }

            screen = new Transform[resX, resY];

            for (int x = 0; x < resX; x++)
            {
                for (int y = 0; y < resY; y++)
                {
                    int invY = resY - y - 1;

                    Vector3 offset = new Vector3(spacing.x * x, spacing.y * invY, 0f);

                    Vector3 resultPos = position + offset;

                    Quaternion rot = Quaternion.Euler(90f, 0f, 0f);

                    Transform t = BarricadeManager.dropNonPlantedBarricade(new Barricade(whitePixel), resultPos, rot, 0, 0);

                    screen[x, invY] = t;
                }
            }
        }
        public static async Task PlayBadApple()
        {
            if (isReady && !isPlaying)
            {
                ushort soundEffect = Instance.Configuration.Instance.SoundEffectID;

                ushort whitePixel = Instance.Configuration.Instance.WhitePixel;
                ushort blackPixel = Instance.Configuration.Instance.BlackPixel;

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

                        Vector3 oldPos = t.localPosition;
                        Quaternion oldRot = t.localRotation;

                        if (BarricadeManager.tryGetInfo(t, out var bX, out var bY, out var bPlant, out var bIndex, out var bRegion))
                        {
                            BarricadeManager.destroyBarricade(bRegion, bX, bY, bPlant, bIndex);

                            Transform newT;
                            
                            if (p.type)
                                newT = BarricadeManager.dropNonPlantedBarricade(new Barricade(whitePixel), oldPos, oldRot, 0, 0);
                            else
                                newT = BarricadeManager.dropNonPlantedBarricade(new Barricade(blackPixel), oldPos, oldRot, 0, 0);

                            screen[p.x, invY] = newT;
                        }
                    }

                    while (sw.ElapsedTicks < 333333)
                    {
                        await Task.Yield();
                    }
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
                            int.TryParse(type, out int typeData))
                        {
                            switch (typeData)
                            {
                                case 0:
                                    f.poses.Push(new Pos(x, y, false));
                                    break;
                                case 1:
                                    f.poses.Push(new Pos(x, y, true));
                                    break;
                            }
                        }
                    }

                    frames.Enqueue(f);
                }
            }

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
        }
    }
}
