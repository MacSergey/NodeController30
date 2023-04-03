using ColossalFramework.Math;
using ModsCommon.Utilities;
using System;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;

namespace NodeController
{
    public class SegmentSide
    {
        private struct DataStruct
        {
            private SegmentSide segmentSide;

            public CombinedTrajectory rawTrajectory;

            public float minT;
            public float maxT;
            public float mainT;
            public float rawT;
            public float defaultT;

            public Vector3 minPos;
            public Vector3 minDir;

            public Vector3 position;
            public Vector3 direction;
            public Quaternion dirRotation;
            public float dirRatio;
            public Vector3 deltaPos;

            public Vector3 maxPos;
            public Vector3 maxDir;

            public float DeltaT
            {
                get
                {
                    var deltaT = 0.05f / rawTrajectory.Length;
                    return deltaT;
                }
            }
            public float CurrentT
            {
                get
                {
                    var deltaT = DeltaT;
                    var currentT = Mathf.Clamp(rawT, minT + deltaT, maxT - deltaT);
                    return currentT;
                }
            }

            public bool IsMinBorderT
            {
                get
                {
                    var deltaT = DeltaT;
                    return rawT - deltaT <= minT;
                }
            }
            public bool IsMaxBorderT
            {
                get
                {
                    var deltaT = DeltaT;
                    return rawT + deltaT >= maxT;
                }
            }
            public bool IsShort
            {
                get
                {
                    var isShort = (maxT - CurrentT) <= (1f / rawTrajectory.Length);
                    return isShort;
                }
            }

            public DataStruct(SegmentSide segmentSide)
            {
                this.segmentSide = segmentSide;
                rawTrajectory = new CombinedTrajectory(new StraightTrajectory(Vector3.zero, Vector3.zero));

                minT = 0f;
                maxT = 1f;
                rawT = 0f;
                mainT = 0f;
                defaultT = 0f;

                minPos = default;
                minDir = default;
                position = default;
                direction = default;
                deltaPos = default;
                dirRotation = Quaternion.identity;
                dirRatio = 1;
                maxPos = default;
                maxDir = default;
            }

            public void Set(ITrajectory trajectory)
            {
                if (rawTrajectory.IsZero || !rawTrajectory[1].Equals(trajectory))
                {
                    var mainTrajectory = trajectory;
                    var additionalTrajectory = new StraightTrajectory(trajectory.StartPosition - trajectory.StartDirection * segmentSide.AdditionalLength, trajectory.StartPosition);
                    rawTrajectory = new CombinedTrajectory(additionalTrajectory, mainTrajectory);

                    minT = 0f;
                    maxT = 1f;
                    mainT = rawTrajectory.Parts[1];

                    Update();
                }
            }

            public void Update()
            {
                minPos = rawTrajectory.Position(minT);
                minDir = rawTrajectory.Tangent(minT).normalized;

                maxPos = rawTrajectory.Position(maxT);
                maxDir = -rawTrajectory.Tangent(maxT).normalized;
            }
        }

        public float AdditionalLength => Mathf.Max(SegmentData.Id.GetSegment().Info.m_halfWidth / Mathf.Cos(75f * Mathf.Deg2Rad), 16f);

        public SideType Type { get; }
        public SegmentEndData SegmentData { get; }

        public Vector3 PosDelta { get; set; }
        public Vector3 DirDelta { get; set; }

        private DataStruct final;
        private DataStruct temp;

        public CombinedTrajectory RawTrajectory => temp.rawTrajectory;
        public ITrajectory MainTrajectory => temp.rawTrajectory[1];
        public ITrajectory AdditionalTrajectory => temp.rawTrajectory[0];

        public float MinT
        {
            get => temp.minT;
            set
            {
                if (value != temp.minT)
                {
                    temp.minT = value;
                    temp.Update();
                }
            }
        }
        public float MaxT
        {
            get => temp.maxT;
            set
            {
                if (Mathf.Abs(value - temp.maxT) > 0.001f)
                {
                    temp.maxT = value;
                    temp.Update();
                }
            }
        }
        public float MainT => temp.mainT;
        public float DefaultT
        {
            get => temp.defaultT;
            set => temp.defaultT = value;
        }

        public float RawT
        {
            set => temp.rawT = value;
        }
        public float CurrentTempT => temp.CurrentT;
        public float CurrentT => final.CurrentT;

        public Vector3 MinTempPos => temp.minPos;
        public Vector3 MinTempDir => temp.minDir;

        public Vector3 MaxTempPos => temp.maxPos;
        public Vector3 MaxTempDir => temp.maxDir;


        public Vector3 StartPos => final.position + final.deltaPos;
        public Vector3 StartDir => NormalizeXZ(final.dirRotation * final.direction) * final.dirRatio;
        public Vector3 OriginalPos => final.position;
        public Vector3 OriginalDir => final.direction;


        public Vector3 EndPos => final.maxPos;
        public Vector3 EndDir => final.maxDir;

        public Vector3 TempPos => temp.position + temp.deltaPos;
        public Vector3 TempDir => NormalizeXZ(temp.dirRotation * temp.direction) * temp.dirRatio;

        public Vector3 MarkerPos
        {
            get
            {
                var width = SegmentData.Width;
                if (width >= SegmentEndData.MinNarrowWidth)
                    return StartPos;
                else
                    return StartPos + StartDir.Turn90(Type == SideType.Right).MakeFlatNormalized() * (SegmentEndData.MinNarrowWidth - width) / 2f;
            }
        }

        public bool IsMinBorderT => temp.IsMinBorderT;
        public bool IsMaxBorderT => temp.IsMaxBorderT;
        public bool IsShort => temp.IsShort;

        public SegmentSide(SegmentEndData segmentData, SideType type)
        {
            Type = type;
            SegmentData = segmentData;

            temp = new DataStruct(this);
            final = new DataStruct(this);
        }

        public void SetTrajectory(ITrajectory trajectory)
        {
            temp.Set(trajectory);
        }

        public void CalculateMain()
        {
            var nodeData = SegmentData.NodeData;

            var t = Mathf.Clamp(temp.rawT, temp.minT + (nodeData.IsMiddleNode || SegmentData.IsNodeLess ? 0f : temp.DeltaT), temp.maxT - temp.DeltaT);
            var position = temp.rawTrajectory.Position(t);
            var direction = temp.rawTrajectory.Tangent(t).normalized;
            temp.deltaPos = Vector3.zero;
            temp.dirRotation = Quaternion.identity;
            temp.dirRatio = 1f;

            switch (SegmentData.Mode)
            {
                case Mode.Flat:
                    {
                        position.y = SegmentData.NodeId.GetNode().m_position.y;
                        direction = direction.MakeFlatNormalized();

                        if (nodeData.IsEndNode)
                            temp.dirRatio *= SegmentData.Stretch;
                    }
                    break;
                case Mode.Slope:
                    {
                        if (nodeData.Style.SupportDeltaHeight != SupportOption.None)
                            temp.deltaPos.y = PosDelta.y;

                        if (nodeData.Style.SupportSlope != SupportOption.None)
                            temp.dirRotation = Quaternion.AngleAxis(SegmentData.SlopeAngle, direction.MakeFlat().Turn90(true));

                        if (nodeData.Style.SupportTwist != SupportOption.None)
                        {
                            var ratio = Mathf.Sin(SegmentData.TwistAngle * Mathf.Deg2Rad);
                            if (nodeData.Style.SupportStretch != SupportOption.None)
                                ratio *= SegmentData.Stretch;

                            temp.deltaPos.y += (Type == SideType.Left ? -1 : 1) * SegmentData.Id.GetSegment().Info.m_halfWidth * ratio;
                        }

                        if (nodeData.IsEndNode)
                            temp.dirRatio *= SegmentData.Stretch;
                    }
                    break;
                case Mode.FreeForm:
                    {
                        var angle = direction.AbsoluteAngle();
                        temp.deltaPos += Quaternion.AngleAxis(-angle * Mathf.Rad2Deg, Vector3.up) * PosDelta;

                        var deltaDir = Type switch
                        {
                            SideType.Left => SegmentData.LeftDirDelta,
                            SideType.Right => SegmentData.RightDirDelta,
                            _ => Vector3.zero,
                        };
                        if (deltaDir.x != 0f)
                            temp.dirRotation *= Quaternion.AngleAxis(deltaDir.x, Vector3.up);
                        if (deltaDir.y != 0f)
                            temp.dirRotation *= Quaternion.AngleAxis(deltaDir.y, direction.MakeFlat().Turn90(true));
                        if (deltaDir.z != 0f)
                            temp.dirRatio *= deltaDir.z;
                    }
                    break;
            }

            temp.position = position;
            temp.direction = direction;
        }

        public void CalculateNotMain(BezierTrajectory left, BezierTrajectory right)
        {
            var nodeData = SegmentData.NodeData;

            var t = Mathf.Clamp(temp.rawT, temp.minT + (nodeData.IsMiddleNode || SegmentData.IsNodeLess ? 0f : temp.DeltaT), temp.maxT - temp.DeltaT);
            var position = temp.rawTrajectory.Position(t);
            var direction = temp.rawTrajectory.Tangent(t).normalized;
            temp.deltaPos = Vector3.zero;
            temp.dirRotation = Quaternion.identity;
            temp.dirRatio = 1f;

            switch (SegmentData.Mode)
            {
                case Mode.Flat:
                    {
                        position.y = SegmentData.NodeId.GetNode().m_position.y;
                        direction = direction.MakeFlatNormalized();

                        if (nodeData.IsEndNode)
                            temp.dirRatio *= SegmentData.Stretch;
                    }
                    break;
                case Mode.Slope:
                    {
                        if (nodeData.Style.SupportDeltaHeight != SupportOption.None && SegmentData.FollowSlope == false)
                            temp.deltaPos.y = PosDelta.y;

                        GetClosest(left, right, position, out var closestPos, out var closestDir, out var closestT);

                        var normal = closestDir.MakeFlat().Turn90(true);
                        var twist = Mathf.Lerp(-nodeData.FirstMainSegmentEnd.TwistAngle, nodeData.SecondMainSegmentEnd.TwistAngle, closestT);
                        normal.y = normal.magnitude * Mathf.Sin(twist * Mathf.Deg2Rad);

                        var plane = new Plane();
                        plane.Set3Points(closestPos, closestPos + closestDir, closestPos + normal);
                        plane.Raycast(new Ray(position, Vector3.up), out var rayT);
                        position += Vector3.up * rayT;

                        var point = position + direction;
                        plane.Raycast(new Ray(point, Vector3.up), out rayT);
                        point += Vector3.up * rayT;
                        direction = (point - position).normalized;

                        if (nodeData.IsEndNode)
                            temp.dirRatio *= SegmentData.Stretch;
                    }
                    break;
            }

            temp.position = position;
            temp.direction = direction;
        }

        private void GetClosest(BezierTrajectory left, BezierTrajectory right, Vector3 position, out Vector3 closestPos, out Vector3 closestDir, out float t)
        {
            left.Trajectory.ClosestPositionAndDirection(position, out var leftClosestPos, out var leftClosestDir, out var leftT);
            right.Trajectory.ClosestPositionAndDirection(position, out var rightClosestPos, out var rightClosestDir, out var rightT);

            if ((leftClosestPos - position).sqrMagnitude < (rightClosestPos - position).sqrMagnitude)
            {
                closestPos = leftClosestPos;
                closestDir = leftClosestDir;
                t = leftT;
            }
            else
            {
                closestPos = rightClosestPos;
                closestDir = rightClosestDir;
                t = rightT;
            }
        }

        public void AfterCalculate()
        {
            final = temp;
        }

        public float FromMainT(float t) => temp.rawTrajectory.FromPartT(1, t);
        public float FromAdditionalT(float t) => temp.rawTrajectory.FromPartT(0, t);

        public float ToMainT(float t) => temp.rawTrajectory.ToPartT(1, t);
        public float ToAdditionalT(float t) => temp.rawTrajectory.ToPartT(0, t);

        public Vector3 FromAbsoluteDeltaPos(Vector3 deltaPos)
        {
            var angle = OriginalDir.AbsoluteAngle();
            deltaPos = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.up) * deltaPos;
            return deltaPos;
        }
        public Vector3 ToAbsoluteDeltaPos(Vector3 deltaPos)
        {
            var angle = OriginalDir.AbsoluteAngle();
            deltaPos = Quaternion.AngleAxis(-angle * Mathf.Rad2Deg, Vector3.up) * deltaPos;
            return deltaPos;
        }

        public static void FixMiddle(SegmentSide first, SegmentSide second)
        {
            var fixPosition = (first.temp.position + second.temp.position) / 2f;
            first.temp.position = fixPosition;
            second.temp.position = fixPosition;

            var fixDirection = NormalizeXZ(first.temp.direction - second.temp.direction);

            var firstFixDirection = fixDirection;
            firstFixDirection.y = first.temp.direction.y;
            first.temp.direction = firstFixDirection;

            var secondFixDirection = -fixDirection;
            secondFixDirection.y = second.temp.direction.y;
            second.temp.direction = secondFixDirection;
        }
        public static void FixBend(SegmentSide left, SegmentSide right)
        {
            var isLeft = left.SegmentData.NodeData.IsTwoRoads && left.SegmentData.Mode != Mode.Flat;
            var isRight = right.SegmentData.NodeData.IsTwoRoads && right.SegmentData.Mode != Mode.Flat;

            if (isLeft == isRight)
                return;
            else if (isLeft)
            {
                var bezier = new BezierTrajectory(left.MainTrajectory.StartPosition, left.MainTrajectory.StartDirection, right.temp.position, right.temp.direction, new BezierTrajectory.Data(true, true, true));
                bezier.GetHitPosition(new Segment3(left.temp.position, left.temp.position + Vector3.up), out _, out var t, out _);
                if (t > 0)
                {
                    left.temp.position = bezier.Position(t);
                    left.temp.direction = bezier.Tangent(t).normalized;
                }
            }
            else
            {
                var bezier = new BezierTrajectory(right.MainTrajectory.StartPosition, right.MainTrajectory.StartDirection, left.temp.position, left.temp.direction, new BezierTrajectory.Data(true, true, true));
                bezier.GetHitPosition(new Segment3(right.temp.position, right.temp.position + Vector3.up), out _, out var t, out _);
                if (t > 0)
                {
                    right.temp.position = bezier.Position(t);
                    right.temp.direction = bezier.Tangent(t).normalized;
                }
            }
        }

        public void RenderGuides(OverlayData dataAllow, OverlayData dataForbidden, OverlayData dataDefault)
        {
            var deltaT = 0.2f / final.rawTrajectory.Length;
            if (final.minT == 0f)
            {
                if (final.rawT >= deltaT)
                    final.rawTrajectory.Cut(0f, final.rawT).Render(dataAllow);
            }
            else
            {
                dataForbidden.CutEnd = true;
                dataAllow.CutStart = true;

                var t = Math.Min(final.rawT, final.minT);
                if (t >= final.DeltaT)
                    final.rawTrajectory.Cut(0f, t).Render(dataForbidden);

                if (final.rawT - final.minT >= 0.2f / final.rawTrajectory.Length)
                    final.rawTrajectory.Cut(final.minT, final.rawT).Render(dataAllow);
            }
            dataDefault.Color ??= CommonColors.Purple;
            final.rawTrajectory.Position(final.defaultT).RenderCircle(dataDefault);
        }
        public void Render(OverlayData centerData, OverlayData circleData)
        {
            RenderCenter(centerData);
            RenderCircle(circleData);
        }
        public void RenderCenter(OverlayData data)
        {
            var markerPosition = MarkerPos;
            if ((markerPosition - final.position).sqrMagnitude > 0.25f)
            {
                var color = data.Color.HasValue ? ((Color32)data.Color.Value).SetAlpha(128) : CommonColors.White128;
                new StraightTrajectory(markerPosition, final.position).Render(new OverlayData(data.CameraInfo) { Color = color });
            }
            markerPosition.RenderCircle(data, data.Width ?? SegmentEndData.CornerCenterRadius * 2, 0f);
        }
        public void RenderCircle(OverlayData data)
        {
            MarkerPos.RenderCircle(data, SegmentEndData.CornerCircleRadius * 2 + 0.5f, SegmentEndData.CornerCircleRadius * 2 - 0.5f);
        }

        public override string ToString() => $"{Type}: {nameof(final.rawT)}={final.rawT}; {nameof(final.minT)}={final.minT}; {nameof(final.maxT)}={final.maxT}; {nameof(final.position)}={final.position};";
    }

    public enum SideType : byte
    {
        Left,
        Right
    }
    public static class SideTypeExtension
    {
        public static SideType Invert(this SideType side) => side == SideType.Left ? SideType.Right : SideType.Left;
    }
}
