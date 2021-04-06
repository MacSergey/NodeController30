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
            if(IsHoverNode)
            {
                var data = NodeManager.Instance.GetOrCreate(HoverNode.Id);
                Tool.SetData(data);
                Tool.SetDefaultMode();
            }
            //else if(IsHoverSegment)
            //{
            //    var data = SegmentEndManager.Instance.GetOrCreate(HoverSegment.Id, );
            //    Tool.SetData(data);
            //    Tool.SetDefaultMode();
            //}

            //if (!IsHoverNode && !IsHoverSegment)
            //    return;

            //var messageBox = MessageBoxBase.ShowModal<OneButtonMessageBox>();
            //messageBox.CaptionText = SingletonMod<Mod>.NameRaw;
            //messageBox.MessageText = GetToolInfo();
            //messageBox.ButtonText = "OK";
        }
    }
    public class EditToolMode : NodeControllerToolMode
    {
        public override ToolModeType Type => ToolModeType.Edit;
        public override void OnSecondaryMouseClicked()
        {
            Tool.SetData(null);
            Tool.SetMode(ToolModeType.Select);
        }
    }
}
