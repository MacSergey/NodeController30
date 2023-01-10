using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeController.UI;
using NodeController.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace NodeController
{
    public enum NodeStyleType
    {
        [Description(nameof(Localize.NodeStyle_Middle))]
        Middle,

        [Description(nameof(Localize.NodeStyle_Bend))]
        Bend,

        [Description(nameof(Localize.NodeStyle_Stretch))]
        Stretch,

        [Description(nameof(Localize.NodeStyle_Crossing))]
        Crossing,

        [Description(nameof(Localize.NodeStyle_UTurn))]
        UTurn,

        [Description(nameof(Localize.NodeStyle_Custom))]
        Custom,

        [Description(nameof(Localize.NodeStyle_End))]
        End,
    }

    public class MiddleNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.Middle;
        public override SupportOption SupportSlope => Data.SegmentEndDatas.All(TouchablePredicate) ? SupportOption.All : SupportOption.None;
        private SupportOption Support
        {
            get
            {
                if (!Data.SegmentEndDatas.All(TouchablePredicate))
                    return SupportOption.None;
                else if (Data.SegmentEndDatas.Any(IsDecorationPredicate))
                    return SupportOption.All;
                else
                    return SupportOption.Group;
            }
        }
        public override SupportOption SupportTwist => Support;
        public override SupportOption SupportShift => Support;
        public override SupportOption SupportStretch => Support;

        public override Mode DefaultMode => Mode.Slope;
        public override bool NeedFixDirection => false;

        public MiddleNode(NodeData data) : base(data) { }

        public override float GetSlope() => (Data.FirstMainSegmentEnd.SlopeAngle - Data.SecondMainSegmentEnd.SlopeAngle) / 2;
        public override void SetSlope(float value)
        {
            Data.FirstMainSegmentEnd.SlopeAngle = value;
            Data.SecondMainSegmentEnd.SlopeAngle = -value;
        }

        public override float GetTwist() => (Data.FirstMainSegmentEnd.TwistAngle - Data.SecondMainSegmentEnd.TwistAngle) / 2;
        public override void SetTwist(float value)
        {
            Data.FirstMainSegmentEnd.TwistAngle = value;
            Data.SecondMainSegmentEnd.TwistAngle = -value;
        }

        public override float GetStretch() => (Data.FirstMainSegmentEnd.Stretch + Data.SecondMainSegmentEnd.Stretch) / 2;
        public override void SetStretch(float value)
        {
            Data.FirstMainSegmentEnd.Stretch = value;
            Data.SecondMainSegmentEnd.Stretch = value;
        }
    }
    public class BendNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.Bend;

        public override float DefaultOffset => 8f;
        public override float AdditionalOffset => 2f;

        public override SupportOption SupportOffset => SupportOption.All;
        public override SupportOption SupportRotate => SupportOption.All;
        public override SupportOption SupportSlope => SupportOption.All;
        public override SupportOption SupportTwist => SupportOption.All;
        public override SupportOption SupportShift => SupportOption.All;
        public override SupportOption SupportStretch => SupportOption.All;
        public override SupportOption SupportMode => SupportOption.Group;
        public override SupportOption SupportDeltaHeight => SupportOption.All;
        public override bool IsMoveable => true;

        public BendNode(NodeData data) : base(data) { }

        public override float GetTwist() => (Data.FirstMainSegmentEnd.TwistAngle - Data.SecondMainSegmentEnd.TwistAngle) / 2;
        public override void SetTwist(float value)
        {
            Data.FirstMainSegmentEnd.TwistAngle = value;
            Data.SecondMainSegmentEnd.TwistAngle = -value;
        }
    }
    public class StretchNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.Stretch;

        public override float DefaultOffset => 8f;
        public override float AdditionalOffset => 2f;

        public override SupportOption SupportOffset => SupportOption.All;
        public override SupportOption SupportRotate => SupportOption.All;
        public override SupportOption SupportSlope => SupportOption.All;
        public override SupportOption SupportTwist => SupportOption.All;
        public override SupportOption SupportShift => SupportOption.All;
        public override SupportOption SupportStretch => SupportOption.All;
        public override SupportOption SupportMode => SupportOption.Group;
        public override bool IsMoveable => true;

        public StretchNode(NodeData data) : base(data) { }

        public override float GetTwist() => (Data.FirstMainSegmentEnd.TwistAngle - Data.SecondMainSegmentEnd.TwistAngle) / 2;
        public override void SetTwist(float value)
        {
            Data.FirstMainSegmentEnd.TwistAngle = value;
            Data.SecondMainSegmentEnd.TwistAngle = -value;
        }
    }
    public class CrossingNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.Crossing;
        public override float DefaultOffset => 2f;

        public override SupportOption SupportShift => SupportOption.Group;
        public override SupportOption SupportTwist => SupportOption.Group;
        public override SupportOption SupportStretch => SupportOption.Group;
        public override SupportOption SupportMarking => SupportOption.All;
        public override SupportOption SupportMode => SupportOption.Group;
        public override bool OnlyKeepDefault => true;
        public override bool SupportTrafficLights => true;
        public override bool NeedFixDirection => false;

        public CrossingNode(NodeData data) : base(data) { }

        public override float GetTwist() => (Data.FirstMainSegmentEnd.TwistAngle - Data.SecondMainSegmentEnd.TwistAngle) / 2;
        public override void SetTwist(float value)
        {
            Data.FirstMainSegmentEnd.TwistAngle = value;
            Data.SecondMainSegmentEnd.TwistAngle = -value;
        }
    }
    public class UTurnNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.UTurn;
        public override float DefaultOffset => 8f;
        public override bool DefaultNoMarking => true;

        public override SupportOption SupportShift => SupportOption.Group;
        public override SupportOption SupportTwist => SupportOption.Group;
        public override SupportOption SupportStretch => SupportOption.Group;
        public override SupportOption SupportMarking => SupportOption.All;
        public override SupportOption SupportMode => SupportOption.Group;
        public override bool SupportTrafficLights => true;
        public override bool OnlyKeepDefault => true;
        public override bool NeedFixDirection => false;

        public UTurnNode(NodeData data) : base(data) { }

        public override float GetTwist() => (Data.FirstMainSegmentEnd.TwistAngle - Data.SecondMainSegmentEnd.TwistAngle) / 2;
        public override void SetTwist(float value)
        {
            Data.FirstMainSegmentEnd.TwistAngle = value;
            Data.SecondMainSegmentEnd.TwistAngle = -value;
        }
    }
    public class EndNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.End;

        public override SupportOption SupportOffset => SupportOption.None;
        public override SupportOption SupportRotate => SupportOption.Group;
        public override SupportOption SupportShift => SupportOption.Group;
        public override SupportOption SupportSlope => SupportOption.Group;
        public override SupportOption SupportTwist => SupportOption.Group;
        public override SupportOption SupportStretch => SupportOption.Group;
        public override SupportOption SupportMode => SupportOption.Group;
        public override bool SupportTrafficLights => true;
        public override bool IsMoveable => true;

        public EndNode(NodeData data) : base(data) { }
    }
    public class CustomNode : NodeStyle
    {
        public override NodeStyleType Type => NodeStyleType.Custom;

        public override float DefaultOffset => 8f;
        public override float AdditionalOffset => 2f;

        public override SupportOption SupportOffset => SupportOption.All;
        public override SupportOption SupportShift => SupportOption.All;
        public override SupportOption SupportRotate => SupportOption.All;
        public override SupportOption SupportSlope => SupportOption.All;
        public override SupportOption SupportTwist => SupportOption.All;
        public override SupportOption SupportStretch => SupportOption.All;
        public override SupportOption SupportMarking => SupportOption.All;
        public override SupportOption SupportMode => SupportOption.Group;
        public override SupportOption SupportCollision => SupportOption.All;
        public override SupportOption SupportForceNodeless => SupportOption.All;
        public override SupportOption SupportDeltaHeight => SupportOption.All;
        public override SupportOption SupportFollowMainSlope => SupportOption.All;
        public override bool IsMoveable => true;
        public override bool SupportTrafficLights => true;

        public CustomNode(NodeData data) : base(data) { }

        public override float GetTwist()
        {
            if (Data.IsDecoration | Data.Mode == Mode.FreeForm)
                return base.GetTwist();
            else
            {
                var first = Data.FirstMainSegmentEnd;
                var second = Data.SecondMainSegmentEnd;

                if (first.IsUntouchable)
                    return second.IsUntouchable ? 0f : second.TwistAngle;
                else if (second.IsUntouchable)
                    return first.IsUntouchable ? 0f : first.TwistAngle;
                else
                    return (first.TwistAngle - second.TwistAngle) / 2;
            }
        }
        public override void SetTwist(float value)
        {
            if (Data.IsDecoration | Data.Mode == Mode.FreeForm)
                base.SetTwist(value);
            else
            {
                var first = Data.FirstMainSegmentEnd;
                var second = Data.SecondMainSegmentEnd;

                if (!first.IsUntouchable && !second.IsUntouchable)
                {
                    first.TwistAngle = value;
                    second.TwistAngle = -value;
                }
                else
                {
                    if (!first.IsUntouchable)
                        first.TwistAngle = value;
                    if (!second.IsUntouchable)
                        second.TwistAngle = value;
                }
            }
        }
    }
}
