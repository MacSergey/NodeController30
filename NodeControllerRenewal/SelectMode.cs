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

namespace NodeController
{
    public class SelectToolMode : BaseSelectToolMode<Mod, NodeControllerTool>, IToolModePanel, IToolMode<ToolModeType>
    {
        public bool ShowPanel => false;
        public ToolModeType Type => ToolModeType.Select;

        public override string GetToolInfo() => IsHoverNode ? $"Node {HoverNode.Id}": (IsHoverSegment ? $"Segment {HoverSegment.Id}" : string.Empty);

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (!IsHoverNode && !IsHoverSegment)
                return;

            var messageBox = MessageBoxBase.ShowModal<OneButtonMessageBox>();
            messageBox.CaptionText = SingletonMod<Mod>.Name;
            messageBox.MessageText = GetToolInfo();
            messageBox.ButtonText = "OK";
        }
    }
}
