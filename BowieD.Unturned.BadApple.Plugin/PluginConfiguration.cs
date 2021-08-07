using Rocket.API;
using System.Xml.Serialization;
using UnityEngine;

namespace BowieD.Unturned.BadApple.Plugin
{
    public sealed class PluginConfiguration : IRocketPluginConfiguration
    {
        private float[] _spacingRaw;
        private Vector3 _spacing;

        public int ResolutionX { get; set; }
        public int ResolutionY { get; set; }
        public ushort WhitePixel { get; set; } = 1065;
        public ushort BlackPixel { get; set; } = 892;
        public ushort GrayPixel { get; set; } = 1092;
        public ushort DarkBrownPixel { get; set; } = 1071;
        public ushort SoundEffectID { get; set; } = 0;

        public float[] SpacingRaw
        {
            get => _spacingRaw;
            set
            {
                if (value.Length <= 3)
                {
                    switch (value.Length)
                    {
                        case 0:
                            _spacingRaw = new float[3] { 0, 0, 0 };
                            break;
                        case 1:
                            _spacingRaw = new float[3] { value[0], 0, 0 };
                            break;
                        case 2:
                            _spacingRaw = new float[3] { value[0], value[1], 0 };
                            break;
                        case 3:
                            _spacingRaw = value;
                            break;
                    }
                }
                else
                {
                    _spacingRaw = new float[3] { value[0], value[1], value[2] };
                }

                _spacing = new Vector3(_spacingRaw[0], _spacingRaw[1], _spacingRaw[2]);
            }
        }
        [XmlIgnore]
        public Vector3 Spacing => _spacing;

        public void LoadDefaults()
        {
            ResolutionX = 40;
            ResolutionY = 30;

            BlackPixel = 892;
            DarkBrownPixel = 1071;
            GrayPixel = 1092;
            WhitePixel = 1065;

            SpacingRaw = new float[3] { 2f, 2f, 2f };

            SoundEffectID = 0;
        }
    }
}
