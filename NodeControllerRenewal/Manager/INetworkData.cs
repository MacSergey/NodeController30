using ColossalFramework.UI;
using System;

namespace NodeController
{
    public interface INetworkData
    {
        public string Title { get; }

        public float Offset { get; set; }
        public float RotateAngle { get; set; }
        public float SlopeAngle { get; set; }
        public float TwistAngle { get; set; }
        public float Shift { get; set; }
        public float Stretch { get; set; }
        public float StretchPercent { get; set; }
        public bool NoMarkings { get; set; }
        public bool Collision { get; set; }
    }
}
