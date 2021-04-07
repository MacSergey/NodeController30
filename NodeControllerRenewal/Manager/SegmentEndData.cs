using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using CSUtil.Commons;
using KianCommons;
using KianCommons.Math;
using NodeController.GUI;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using UnityEngine;
using CSURUtil = NodeController.Util.CSURUtil;
using static KianCommons.HelpersExtensions;
using static KianCommons.ReflectionHelpers;
using System.Linq;
using KianCommons.Serialization;
using Vector3Serializable = KianCommons.Math.Vector3Serializable;
using NodeController;
using ModsCommon;
using System.Collections.Generic;
using ModsCommon.UI;
using ModsCommon.Utilities;

namespace NodeController
{
    [Serializable]
    public class SegmentEndData : INetworkData, INetworkData<SegmentEndData>, ISerializable
    {
        #region PROPERTIES

        public string Title => $"Segment #{SegmentId}";

        public ushort NodeId { get; set; }
        public ushort SegmentId { get; set; }

        public ref NetSegment Segment => ref SegmentId.ToSegment();
        public NetInfo Info => Segment.Info;
        public ref NetNode Node => ref NodeId.ToNode();
        public NodeData NodeData => NodeManager.Instance.buffer[NodeId];
        public NodeTypeT NodeType => NodeData.NodeType;

        public CornerData LeftCorner { get; set; } = new CornerData { Left = true };
        public CornerData RightCorner { get; set; } = new CornerData { Left = false };

        public bool IsStartNode => NetUtil.IsStartNode(SegmentId, NodeId);
        public Vector3 Direction => IsStartNode ? Segment.m_startDirection : Segment.m_endDirection;

        public float DefaultCornerOffset => CSURUtil.GetMinCornerOffset(SegmentId, NodeId);
        public bool DefaultFlatJunctions => Info.m_flatJunctions || Node.m_flags.IsFlagSet(NetNode.Flags.Untouchable);
        public bool DefaultTwist => DefaultFlatJunctions && !Node.m_flags.IsFlagSet(NetNode.Flags.Untouchable);
        public NetSegment.Flags DefaultFlags { get; set; }

        public bool HasPedestrianLanes { get; set; }
        public float CurveRaduis0 { get; set; }
        public int PedestrianLaneCount { get; set; }
        public float CachedSuperElevationDeg { get; set; }

        public bool NoCrossings { get; set; }
        public bool NoMarkings { get; set; }
        public bool NoJunctionTexture { get; set; }
        public bool NoJunctionProps { get; set; }
        public bool NoTLProps { get; set; }
        public bool FlatJunctions { get; set; }
        public bool Twist { get; set; }

        public float Stretch { get; set; }
        public float EmbankmentAngle { get; set; }
        public float DeltaSlopeAngle { get; set; }

        public bool IsDefault
        {
            get
            {
                bool ret = Mathf.Abs(CornerOffset - DefaultCornerOffset) < 0.1f;
                ret &= DeltaSlopeAngle == 0;
                ret &= Stretch == 0;
                ret &= EmbankmentAngle == 0;
                ret &= FlatJunctions == DefaultFlatJunctions;
                ret &= Twist == DefaultTwist;
                ret &= LeftCorner.IsDefault();
                ret &= RightCorner.IsDefault();

                ret &= NoCrossings == false;
                ret &= NoMarkings == false;
                ret &= NoJunctionTexture == false;
                ret &= NoJunctionProps == false;
                ret &= NoTLProps == false;
                return ret;
            }
        }
        public float Offset { get; set; }
        public float Angle { get; set; }

        public float CornerOffset
        {
            get => (LeftCorner.Offset + RightCorner.Offset) * 0.5f;
            set
            {
                LeftCorner.Offset = RightCorner.Offset = value;
                Update();
            }
        }
        public float EmbankmentPercent
        {
            get => Mathf.Tan(EmbankmentAngle * Mathf.Deg2Rad) * 100;
            set => EmbankmentAngle = Mathf.Atan(value * 0.01f) * Mathf.Rad2Deg;
        }
        public float SlopeAngle
        {
            get => DeltaSlopeAngle + AngleDeg(AverageDirY00);
            set => DeltaSlopeAngle = value - AngleDeg(AverageDirY00);
        }
        float AverageDirY00 => (LeftCorner.Dir00.y + RightCorner.Dir00.y) * 0.5f;

        public bool HasUniformCornerOffset => LeftCorner.Offset == RightCorner.Offset;
        bool CrossingIsRemoved => HideCrosswalks.Patches.CalculateMaterialCommons.ShouldHideCrossing(NodeId, SegmentId);

        public bool IsCSUR => NetUtil.IsCSUR(Info);
        public bool CanModifyOffset => NodeData?.CanModifyOffset ?? false;
        public bool CanModifyCorners => NodeData != null && (CanModifyOffset || NodeType == NodeTypeT.End || NodeType == NodeTypeT.Middle);
        public bool CanModifyFlatJunctions => NodeData?.CanModifyFlatJunctions ?? false;
        public bool CanModifyTwist => CanTwist(SegmentId, NodeId);
        public bool ShowNoMarkingsToggle
        {
            get
            {
                if (IsCSUR)
                    return false;
                else
                    return NodeData == null || NodeData.NodeType == NodeTypeT.Custom;
            }
        }
        public bool? ShouldHideCrossingTexture
        {
            get
            {
                if (NodeData != null && NodeData.NodeType == NodeTypeT.Stretch)
                    return false; // always ignore.
                else if (NoMarkings)
                    return true; // always hide
                else
                    return null; // default.
            }
        }

        #endregion

        #region BASIC

        public SegmentEndData() { }
        private SegmentEndData(SegmentEndData template) => CopyProperties(this, template);
        public SegmentEndData(SerializationInfo info, StreamingContext context)
        {
            SerializationUtil.SetObjectFields(info, this);

            // corner offset and slope angle deg
            SerializationUtil.SetObjectProperties(info, this);

            if (SerializationUtil.DeserializationVersion < new Version(2, 1, 1))
            {
                LeftCorner.Left = true;
                RightCorner.Left = false;

                LeftCorner.DeltaPos = info.GetValue<Vector3Serializable>("DeltaLeftCornerPos");
                LeftCorner.DeltaDir = info.GetValue<Vector3Serializable>("DeltaLeftCornerDir");
                RightCorner.DeltaPos = info.GetValue<Vector3Serializable>("DeltaRightCornerPos");
                RightCorner.DeltaDir = info.GetValue<Vector3Serializable>("DeltaRightCornerDir");
            }
            Update();
        }
        public SegmentEndData(ushort segmentID, ushort nodeID)
        {
            NodeId = nodeID;
            SegmentId = segmentID;

            Calculate();
            CornerOffset = DefaultCornerOffset;
            FlatJunctions = DefaultFlatJunctions;
            Twist = DefaultTwist;

            Update();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) => SerializationUtil.GetObjectFields(info, this);
        public SegmentEndData Clone() => new SegmentEndData(this);

        public void Calculate()
        {
            DefaultFlags = Segment.m_flags;
            PedestrianLaneCount = Info.CountPedestrianLanes();

            Refresh();
        }
        public void RefreshAndUpdate()
        {
            Refresh();
            Update();
        }
        private void Refresh()
        {
            if (!CanModifyOffset)
                CornerOffset = DefaultCornerOffset;

            if (!CanModifyFlatJunctions)
                FlatJunctions = DefaultFlatJunctions;

            if (!CanModifyTwist)
                Twist = DefaultTwist;

            if (!CanModifyCorners)
            {
                DeltaSlopeAngle = 0;
                Stretch = EmbankmentAngle = 0;
            }

            if (!FlatJunctions)
                Twist = false;
        }
        public void Update()
        {
            NetManager.instance.UpdateNode(NodeId);
        }
        public void ResetToDefault()
        {
            CornerOffset = DefaultCornerOffset;
            DeltaSlopeAngle = 0;
            FlatJunctions = DefaultFlatJunctions;
            Twist = DefaultTwist;
            NoCrossings = false;
            NoMarkings = false;
            NoJunctionTexture = false;
            NoJunctionProps = false;
            NoTLProps = false;
            Stretch = EmbankmentAngle = 0;
            LeftCorner.ResetToDefault();
            RightCorner.ResetToDefault();
            RefreshAndUpdate();
        }

        bool InProgress { get; set; } = false;
        public void ApplyCornerAdjustments(ref Vector3 cornerPos, ref Vector3 cornerDir, bool leftSide)
        {
            CornerData corner = Corner(leftSide);
            if (InProgress)
            {
                corner.Dir00 = cornerDir;
                corner.Pos00 = cornerPos;
            }

            CornerData.CalculateTransformVectors(cornerDir, leftSide, out var outwardDir, out var forwardDir);

            //float slopeAngleDeg = DeltaSlopeAngle + AngleDeg(corner.Dir00.y);
            //float slopeAngleRad = slopeAngleDeg * Mathf.Deg2Rad;

            //if (89 <= slopeAngleDeg && slopeAngleDeg <= 91)
            //{
            //    cornerDir.x = cornerDir.z = 0;
            //    cornerDir.y = 1;
            //}
            //else if (-89 >= slopeAngleDeg && slopeAngleDeg >= -91)
            //{
            //    cornerDir.x = cornerDir.z = 0;
            //    cornerDir.y = -1;
            //}
            //else if (slopeAngleDeg > 90 || slopeAngleDeg < -90)
            //{
            //    cornerDir.y = -Mathf.Tan(slopeAngleRad);
            //    cornerDir.x = -cornerDir.x;
            //    cornerDir.z = -cornerDir.z;
            //}
            //else
            //    cornerDir.y = Mathf.Tan(slopeAngleRad);

            if (!Node.m_flags.IsFlagSet(NetNode.Flags.Middle))
            {
                float d = VectorUtils.DotXZ(cornerPos - Node.m_position, cornerDir);
                cornerPos.y += d * (cornerDir.y - corner.Dir00.y);
            }

            if (GUI.Settings.GameConfig.UnviversalSlopeFixes)
            {
                float absY = Mathf.Abs(cornerDir.y);
                if (absY > 2)
                    cornerDir *= 2 / absY;
            }

            //Vector3 deltaPos = Vector3.zero;

            //float embankmentAngleRad = (leftSide ? -1 : 1) * EmbankmentAngle * Mathf.Deg2Rad;
            //float sinEmbankmentAngle = Mathf.Sin(embankmentAngleRad);
            //float cosEmbankmentAngle = Mathf.Cos(embankmentAngleRad);
            //float halfWidth = Info.m_halfWidth;
            //float stretch = Stretch * 0.01f;
            //float totalHalfWidth = halfWidth * (1 + stretch);
            //deltaPos.x += -totalHalfWidth * (1 - cosEmbankmentAngle);
            //deltaPos.y = totalHalfWidth * sinEmbankmentAngle;

            // Stretch:
            //deltaPos.x += halfWidth * stretch * cosEmbankmentAngle;
            //deltaPos.y += halfWidth * stretch * sinEmbankmentAngle;

            //cornerPos += CornerData.TransformCoordinates(deltaPos, outwardDir, Vector3.up, forwardDir);

            if (InProgress)
            {
                // take a snapshot of pos0/dir0 then apply delta pos/dir
                corner.Dir0 = cornerDir;
                corner.Pos0 = cornerPos;
            }

            //cornerPos += CornerData.TransformCoordinates(corner.DeltaPos, outwardDir, Vector3.up, forwardDir);
            //cornerDir += CornerData.TransformCoordinates(corner.DeltaDir, outwardDir, Vector3.up, forwardDir);

            //if (corner.LockLength)
            //{
            //    float prevSqrmagnitiude = ((Vector3)corner.CachedDir).sqrMagnitude;
            //    float newSqrmagnitiude = cornerDir.sqrMagnitude;
            //    cornerDir *= Mathf.Sqrt(prevSqrmagnitiude / newSqrmagnitiude);
            //}
        }
        public void OnAfterCalculate()
        {
            InProgress = true;

            Segment.CalculateCorner(SegmentId, true, IsStartNode, leftSide: true, cornerPos: out var lpos, cornerDirection: out var ldir, out _);
            Segment.CalculateCorner(SegmentId, true, IsStartNode, leftSide: false, cornerPos: out var rpos, cornerDirection: out var rdir, out _);

            LeftCorner.CachedPos = lpos;
            RightCorner.CachedPos = rpos;
            LeftCorner.CachedDir = ldir;
            RightCorner.CachedDir = rdir;

            Vector3 diff = rpos - lpos;
            float se = Mathf.Atan2(diff.y, VectorUtils.LengthXZ(diff));
            CachedSuperElevationDeg = se * Mathf.Rad2Deg;

            SimulationManager.instance.m_ThreadingWrapper.QueueMainThread(delegate ()
            {
                //var activePanel = UIPanelBase.ActivePanel;
                //if (activePanel != null)
                //{
                //    if (activePanel.NetworkType == NetworkTypeT.Node && NodeID == SelectedNodeID)
                //    {
                //        activePanel.RefreshValues();
                //    }
                //    else if (activePanel.NetworkType == NetworkTypeT.SegmentEnd && this.IsSelected())
                //    {
                //        activePanel.RefreshValues();
                //    }
                //}
            });
            InProgress = false;
        }

        #endregion

        #region UTILITIES

        public CornerData Corner(bool left) => left ? LeftCorner : RightCorner;
        public static bool CanTwist(ushort segmentId, ushort nodeId)
        {
            var segmentIds = nodeId.GetNode().SegmentsId().ToArray();

            if (segmentIds.Length == 1)
                return false;

            var segment = segmentId.GetSegment();
            ushort segment1Id = segment.GetLeftSegment(nodeId);
            ushort segment2Id = segment.GetRightSegment(nodeId);
            var segEnd1 = SegmentEndManager.Instance.GetAt(segment1Id, nodeId);
            var segEnd2 = SegmentEndManager.Instance.GetAt(segment2Id, nodeId);

            bool flat1 = segEnd1?.FlatJunctions ?? segment1Id.GetSegment().Info.m_flatJunctions;
            bool flat2 = segEnd2?.FlatJunctions ?? segment2Id.GetSegment().Info.m_flatJunctions;
            if (flat1 && flat2)
                return false;

            if (segmentIds.Length == 2)
            {
                var dir1 = segment1Id.GetSegment().GetDirection(nodeId);
                var dir = segmentId.GetSegment().GetDirection(nodeId);
                if (Mathf.Abs(VectorUtils.DotXZ(dir, dir1)) > 0.999f)
                    return false;
            }

            return true;
        }
        public static float AngleDeg(float y) => Mathf.Atan(y) * Mathf.Rad2Deg;

        public override string ToString() => $"{GetType().Name} (segment:{SegmentId} node:{NodeId})";

        #endregion

        #region UI COMPONENTS

        public List<EditorItem> GetUIComponents(UIComponent parent)
        {
            throw new NotImplementedException();
        }

        #endregion

        [Serializable]
        public class CornerData
        {
            public bool Left;

            public bool IsDefault()
            {
                bool ret = DeltaPos == Vector3.zero && DeltaDir == Vector3.zero;
                //ret &= Offset == ?; // cannot test unless I have link to segment end.
                ret &= LockLength == false;
                return ret;
            }

            public void ResetToDefault()
            {
                DeltaPos = DeltaDir = Vector3.zero;
                LockLength = false;
            }

            public Vector3Serializable CachedPos, CachedDir;
            public Vector3Serializable Dir00, Pos00; // before sliders
            public Vector3Serializable Dir0, Pos0; // after sliders but before 3x4 table
            public Vector3Serializable DeltaPos, DeltaDir;

            public void ResetDeltaDirI(int index)
            {
                Vector3 v = DeltaDir;
                v[index] = 0;
                DeltaDir = v;
            }

            public float Offset;
            public bool LockLength;

            public void SetDirI(float val, int index) => Dir = Dir.SetI(val, index);

            public Vector3 Dir
            {
                get
                {
                    CalculateTransformVectors(Dir0, Left, out var outward, out var forward);
                    return ReverseTransformCoordinats(CachedDir, outward, Vector3.up, forward);
                }
                set
                {
                    if (LockLength)
                        value *= DirLength / value.magnitude;
                    CalculateTransformVectors(Dir0, Left, out var outward, out var forward);
                    DeltaDir = value - ReverseTransformCoordinats(Dir0, outward, Vector3.up, forward);
                    CachedDir = Dir0 + TransformCoordinates(DeltaDir, outward, Vector3.up, forward);
                    //Update();
                }
            }

            public Vector3 GetTransformedDir0()
            {
                CalculateTransformVectors(Dir0, Left, out var outward, out var forward);
                return ReverseTransformCoordinats(Dir0, outward, Vector3.up, forward);
            }

            public float DirLength
            {
                get => ((Vector3)CachedDir).magnitude;
                set
                {
                    bool prevLockLength = LockLength;
                    LockLength = false;
                    Dir *= Mathf.Clamp(value, 0.001f, 1000) / DirLength;
                    LockLength = prevLockLength;
                    //Update();
                }
            }

            public Vector3 Pos
            {
                get => CachedPos;
                set
                {
                    CalculateTransformVectors(Dir0, left: Left, outward: out var outwardDir, forward: out var forwardDir);
                    CachedPos = value;
                    DeltaPos = ReverseTransformCoordinats(value - Pos0, outwardDir, Vector3.up, forwardDir);
                    //Update();
                }
            }

            /// <summary>
            /// all directions going away fromt he junction
            /// </summary>
            public static void CalculateTransformVectors(Vector3 dir, bool left, out Vector3 outward, out Vector3 forward)
            {
                Vector3 rightward = Vector3.Cross(Vector3.up, dir).normalized; // going away from the junction
                Vector3 leftward = -rightward;
                forward = new Vector3(dir.x, 0, dir.z).normalized; // going away from the junction
                outward = left ? leftward : rightward;
            }

            /// <summary>
            /// tranforms input vector from relative (to x y z inputs) coordinate to absulute coodinate.
            /// </summary>
            public static Vector3 TransformCoordinates(Vector3 v, Vector3 x, Vector3 y, Vector3 z)
                => v.x * x + v.y * y + v.z * z;

            /// <summary>
            /// reverse transformed coordinates.
            /// </summary>
            public static Vector3 ReverseTransformCoordinats(Vector3 v, Vector3 x, Vector3 y, Vector3 z)
            {
                Vector3 ret = default;
                ret.x = Vector3.Dot(v, x);
                ret.y = Vector3.Dot(v, y);
                ret.z = Vector3.Dot(v, z);
                return ret;
            }
        }
    }
}
