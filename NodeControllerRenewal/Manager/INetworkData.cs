using ColossalFramework.UI;
using System;
using UnityEngine;

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
        public bool? NoMarkings { get; set; }
        public bool? Collision { get; set; }
        public bool? ForceNodeLess { get; set; }
        public bool? FollowSlope { get; set; }
        public Vector3 LeftPosDelta { get; set; }
        public Vector3 RightPosDelta { get; set; }
        public Vector3 LeftDirDelta { get; set; }
        public Vector3 RightDirDelta { get; set; }
    }
}
