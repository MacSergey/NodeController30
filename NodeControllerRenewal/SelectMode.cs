using ColossalFramework.Math;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ToolBase;
using ColossalFramework.UI;
using ColossalFramework;
using ModsCommon;

namespace NodeController30
{
    public class SelectToolMode : BaseSelectToolMode, IToolModePanel, IToolMode<ToolModeType>
    {
        public bool ShowPanel => false;
        public ToolModeType Type => ToolModeType.Select;

        protected new NodeControllerTool Tool => NodeControllerTool.Instance;

        public override string GetToolInfo() => IsHoverNode ? $"Node {HoverNode.Id}": (IsHoverSegment ? $"Segment {HoverSegment.Id}" : string.Empty);

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (!IsHoverNode && !IsHoverSegment)
                return;

            var messageBox = MessageBoxBase.ShowModal<OneButtonMessageBox>();
            messageBox.CaptionText = Mod.ShortName;
            messageBox.MessageText = GetToolInfo();
            messageBox.ButtonText = "OK";
        }
    }
}
