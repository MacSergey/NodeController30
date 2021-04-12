using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using KianCommons;
using System;
using System.Runtime.Serialization;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using ModsCommon.Utilities;
using NodeController.Utilities;

namespace NodeController
{
    [Serializable]
    public class SegmentEndData : INetworkData, IOverlay
    {
        #region STATIC

        public static float CircleRadius => 2.5f;
        public static float DotRadius => 0.75f;

        #endregion

        #region PROPERTIES

        public string Title => $"Segment #{Id}";

        public ushort NodeId { get; set; }
        public ushort Id { get; set; }

        public NetSegment Segment => Id.GetSegment();
        public NetInfo Info => Segment.Info;
        public NetNode Node => NodeId.GetNode();
        public NodeData NodeData => Manager.Instance[NodeId];
        public bool IsStartNode => Segment.IsStartNode(NodeId);
        public SegmentEndData Other => Manager.Instance[Segment.GetOtherNode(NodeId), Id, true];

        public float DefaultOffset => CSURUtilities.GetMinCornerOffset(Id, NodeId);
        public bool DefaultIsFlat => Info.m_flatJunctions || Node.m_flags.IsFlagSet(NetNode.Flags.Untouchable);
        public bool DefaultIsTwist => DefaultIsFlat && !Node.m_flags.IsFlagSet(NetNode.Flags.Untouchable);
        public NetSegment.Flags DefaultFlags { get; set; }

        public int PedestrianLaneCount { get; set; }
        public float CachedSuperElevationDeg { get; set; }

        public bool NoCrossings { get; set; }
        public bool NoMarkings { get; set; }
        public bool NoJunctionTexture { get; set; }
        public bool NoJunctionProps { get; set; }
        public bool NoTLProps { get; set; }
        public bool IsSlope { get; set; }
        public bool IsTwist { get; set; }

        public bool IsDefault
        {
            get
            {
                var ret = SlopeAngle == 0f;
                ret &= TwistAngle == 0;
                ret &= IsSlope == !DefaultIsFlat;
                ret &= IsTwist == DefaultIsTwist;

                ret &= NoCrossings == false;
                ret &= NoMarkings == false;
                ret &= NoJunctionTexture == false;
                ret &= NoJunctionProps == false;
                ret &= NoTLProps == false;
                return ret;
            }
        }
        public float Offset { get; set; }
        public float Shift { get; set; }
        public float RotateAngle { get; set; }
        public float SlopeAngle { get; set; }
        public float TwistAngle { get; set; }

        public bool CanModifyTwist => CanTwist(Id, NodeId);
        public bool? ShouldHideCrossingTexture
        {
            get
            {
                if (NodeData != null && NodeData.Type == NodeStyleType.Stretch)
                    return false; // always ignore.
                else if (NoMarkings)
                    return true; // always hide
                else
                    return null; // default.
            }
        }

        public SegmentCorner this[bool isLeft]
        {
            get => isLeft ? LeftCorner : RightCorner;
            set
            {
                if (isLeft)
                    LeftCorner = value;
                else
                    RightCorner = value;
            }
        }

        private SegmentCorner LeftCorner { get; set; }
        private SegmentCorner RightCorner { get; set; }
        public Vector3 Position => (LeftCorner.Position + RightCorner.Position) / 2;
        public Vector3 Direction => (RightCorner.Direction + LeftCorner.Direction).normalized;
        public Vector3 EndDirection => (RightCorner.Position - LeftCorner.Position).normalized;
        //private Vector3 Position
        //{
        //    get => Bounds.center;
        //    set
        //    {
        //        Bounds = new Bounds(value, Vector3.one * DotSize);
        //        BoundsInner = new Bounds(value, Vector3.one * (CircleRadius * 2 - 1));
        //        BoundsOutter = new Bounds(value, Vector3.one * (CircleRadius * 2));
        //    }
        //}
        //public Bounds Bounds { get; protected set; }
        //public Bounds BoundsInner { get; protected set; }
        //public Bounds BoundsOutter { get; protected set; }


        #endregion

        #region BASIC

        public SegmentEndData(ushort segmentId, ushort nodeId)
        {
            Id = segmentId;
            NodeId = nodeId;

            DefaultFlags = Segment.m_flags;
            PedestrianLaneCount = Info.CountPedestrianLanes();

            ResetToDefault();
        }
        public void UpdateNode() => Manager.Instance.Update(NodeId);

        public void ResetToDefault()
        {
            Offset = DefaultOffset;

            IsSlope = !DefaultIsFlat;
            IsTwist = DefaultIsTwist;
            NoCrossings = false;
            NoJunctionTexture = false;
            NoJunctionProps = false;
            NoTLProps = false;
        }

        public void OnAfterCalculate()
        {
            var diff = RightCorner.Position - LeftCorner.Position;
            var se = Mathf.Atan2(diff.y, VectorUtils.LengthXZ(diff));
            CachedSuperElevationDeg = se * Mathf.Rad2Deg;
        }

        #endregion

        #region UTILITIES

        public static bool CanTwist(ushort segmentId, ushort nodeId)
        {
            var segmentIds = nodeId.GetNode().SegmentIds().ToArray();

            if (segmentIds.Length == 1)
                return false;

            var segment = segmentId.GetSegment();
            var firstSegmentId = segment.GetLeftSegment(nodeId);
            var secondSegmentId = segment.GetRightSegment(nodeId);
            var nodeData = Manager.Instance[nodeId];
            var segmentEnd1 = nodeData[firstSegmentId];
            var segmentEnd2 = nodeData[secondSegmentId];

            bool flat1 = !segmentEnd1?.IsSlope ?? firstSegmentId.GetSegment().Info.m_flatJunctions;
            bool flat2 = !segmentEnd2?.IsSlope ?? secondSegmentId.GetSegment().Info.m_flatJunctions;
            if (flat1 && flat2)
                return false;

            if (segmentIds.Length == 2)
            {
                var dir1 = firstSegmentId.GetSegment().GetDirection(nodeId);
                var dir = segmentId.GetSegment().GetDirection(nodeId);
                if (Mathf.Abs(VectorUtils.DotXZ(dir, dir1)) > 0.999f)
                    return false;
            }

            return true;
        }
        //public bool IsHoverCenter(Ray ray) => Bounds.IntersectRay(ray);
        //public bool IsHoverCircle(Ray ray) => BoundsOutter.IntersectRay(ray) && !BoundsInner.IntersectRay(ray);

        public void Render(OverlayData data)
        {
            RenderEnd(data);
            RenderInnerCircle(data);
            RenderOther(data);
        }
        public void RenderEnd(OverlayData data)
        {
            var leftLine = new StraightTrajectory(LeftCorner.Position, Position);
            leftLine = (StraightTrajectory)leftLine.Cut(0f, 1f - (CircleRadius / leftLine.Length));
            leftLine.Render(data);

            var rightLine = new StraightTrajectory(RightCorner.Position, Position);
            rightLine = (StraightTrajectory)rightLine.Cut(0f, 1f - (CircleRadius / rightLine.Length));
            rightLine.Render(data);

            RenderOutterCircle(data);
        }
        public void RenderOther(OverlayData data)
        {
            if (Other is SegmentEndData otherSegmentData)
            {
                var otherLeftCorner = otherSegmentData[true];
                var otherRightCorner = otherSegmentData[false];

                var leftSide = new BezierTrajectory(LeftCorner.Position, LeftCorner.Direction, otherRightCorner.Position, otherRightCorner.Direction);
                leftSide.Render(data);
                var rightSide = new BezierTrajectory(RightCorner.Position, RightCorner.Direction, otherLeftCorner.Position, otherLeftCorner.Direction);
                rightSide.Render(data);
                var endSide = new StraightTrajectory(otherLeftCorner.Position, otherRightCorner.Position);
                endSide.Render(data);
            }
        }

        public void RenderInnerCircle(OverlayData data) => RenderCircle(data, DotRadius * 2, 0f);
        public void RenderOutterCircle(OverlayData data) => RenderCircle(data, CircleRadius * 2, CircleRadius * 2 - 0.5f);

        public void RenderCircle(OverlayData data) => Position.RenderCircle(data);
        public void RenderCircle(OverlayData data, float from, float to)
        {
            data.Width = from;
            do
            {
                RenderCircle(data);
                data.Width = Mathf.Max(data.Width.Value - 0.43f, to);
            }
            while (data.Width > to);
        }

        public override string ToString() => $"{GetType().Name} (segment:{Id} node:{NodeId})";

        #endregion

        #region UI COMPONENTS

        public void GetUIComponents(UIComponent parent, Action refresh)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
    public struct SegmentCorner
    {
        public Vector3 Position;
        public Vector3 Direction;
    }
}
