using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeController.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeController.UI
{
    public class NodeControllerPanel : ToolPanel<Mod, NodeControllerTool, NodeControllerPanel>
    {
        private PropertyGroupPanel Content { get; set; }
        private PanelHeader Header { get; set; }
        private NodeTypePropertyPanel TypeProperty { get; set; }
        private List<EditorItem> Properties { get; set; } = new List<EditorItem>();

        public NodeData Data { get; private set; }

        public NodeControllerPanel()
        {
            AddContent();
            AddHeader();
        }
        private void AddContent()
        {
            Content = ComponentPool.Get<PropertyGroupPanel>(this);
            Content.minimumSize = new Vector2(300f, 0f);
            Content.color = new Color32(72, 80, 80, 255);
            Content.autoLayoutDirection = LayoutDirection.Vertical;
            Content.autoFitChildrenVertically = true;
            Content.eventSizeChanged += (UIComponent component, Vector2 value) => size = value;
        }
        private void AddHeader()
        {
            Header = ComponentPool.Get<PanelHeader>(Content);
            Header.Target = this;
            Header.Init();
        }

        public void SetData(NodeData data)
        {
            if ((Data = data) != null)
                SetPanel();
            else
                ResetPanel();
        }
        public void SetPanel()
        {
            Content.StopLayout();

            ResetPanel();

            Content.width = Data.Style.TotalSupport == SupportOption.All ? Mathf.Max((Data.SegmentCount + 1) * 55f + 120f, 300f) : 300f;
            Header.Text = Data.Title;
            RefreshHeader();
            AddNodeTypeProperty();

            FillProperties();

            Content.StartLayout();
        }
        private void ResetPanel()
        {
            Content.StopLayout();

            ComponentPool.Free(TypeProperty);
            ClearProperties();

            Content.StartLayout();
        }

        private void FillProperties() => Properties = Data.Style.GetUIComponents(Content);
        private void ClearProperties()
        {
            foreach (var property in Properties)
                ComponentPool.Free(property);

            Properties.Clear();
        }

        private void AddNodeTypeProperty()
        {
            TypeProperty = ComponentPool.Get<NodeTypePropertyPanel>(Content);
            TypeProperty.Text = NodeController.Localize.Option_Type;
            TypeProperty.Init(Data.IsPossibleType);
            TypeProperty.UseWheel = true;
            TypeProperty.WheelTip = true;
            TypeProperty.SelectedObject = Data.Type;
            TypeProperty.OnSelectObjectChanged += (value) =>
            {
                Data.Type = value;

                Content.StopLayout();

                ClearProperties();
                FillProperties();
                RefreshHeader();

                Content.StartLayout();
            };
        }
        public void RefreshHeader() => Header.Refresh();
        public override void RefreshPanel()
        {
            RefreshHeader();

            foreach (var property in Properties.OfType<IOptionPanel>())
                property.Refresh();
        }
    }
    public class PanelHeader : HeaderMoveablePanel<PanelHeaderContent>
    {
        protected override float DefaultHeight => 40f;

        private HeaderButtonInfo<HeaderButton> MakeStraight { get; set; }

        private HeaderButtonInfo<HeaderButton> CalculateShiftNearby { get; set; }
        private HeaderButtonInfo<HeaderButton> CalculateShiftIntersections { get; set; }
        private HeaderButtonInfo<HeaderButton> SetShiftIntersections { get; set; }

        private HeaderButtonInfo<HeaderButton> CalculateTwistNearby { get; set; }
        private HeaderButtonInfo<HeaderButton> CalculateTwistIntersections { get; set; }
        private HeaderButtonInfo<HeaderButton> SetTwistIntersections { get; set; }

        public PanelHeader()
        {
            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeControllerTextures.Atlas, NodeControllerTextures.KeepDefault, NodeController.Localize.Option_KeepDefault, NodeControllerTool.ResetOffsetShortcut));
            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeControllerTextures.Atlas, NodeControllerTextures.ResetToDefault, NodeController.Localize.Option_ResetToDefault, NodeControllerTool.ResetToDefaultShortcut));

            MakeStraight = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeControllerTextures.Atlas, NodeControllerTextures.MakeStraight, NodeController.Localize.Option_MakeStraightEnds, NodeControllerTool.MakeStraightEndsShortcut);
            Content.AddButton(MakeStraight);

            CalculateShiftNearby = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, NodeControllerTextures.Atlas, NodeControllerTextures.CalculateShiftNearby, NodeController.Localize.Option_CalculateShiftByNearby, NodeControllerTool.CalculateShiftByNearbyShortcut);
            Content.AddButton(CalculateShiftNearby);

            CalculateShiftIntersections = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, NodeControllerTextures.Atlas, NodeControllerTextures.CalculateShiftIntersections, NodeController.Localize.Option_CalculateShiftByIntersections, NodeControllerTool.CalculateShiftByIntersectionsShortcut);
            Content.AddButton(CalculateShiftIntersections);

            SetShiftIntersections = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, NodeControllerTextures.Atlas, NodeControllerTextures.SetShiftBetweenIntersections, NodeController.Localize.Option_SetShiftBetweenIntersections, NodeControllerTool.SetShiftBetweenIntersectionsShortcut);
            Content.AddButton(SetShiftIntersections);

            CalculateTwistNearby = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, NodeControllerTextures.Atlas, NodeControllerTextures.CalculateTwistNearby, NodeController.Localize.Option_CalculateTwistByNearby, NodeControllerTool.CalculateTwistByNearbyShortcut);
            Content.AddButton(CalculateTwistNearby);

            CalculateTwistIntersections = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, NodeControllerTextures.Atlas, NodeControllerTextures.CalculateTwistIntersections, NodeController.Localize.Option_CalculateTwistByIntersections, NodeControllerTool.CalculateTwistByIntersectionsShortcut);
            Content.AddButton(CalculateTwistIntersections);

            SetTwistIntersections = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, NodeControllerTextures.Atlas, NodeControllerTextures.SetTwistBetweenIntersections, NodeController.Localize.Option_SetTwistBetweenIntersections, NodeControllerTool.SetTwistBetweenIntersectionsShortcut);
            Content.AddButton(SetTwistIntersections);
        }

        public override void Refresh()
        {
            if (SingletonTool<NodeControllerTool>.Instance.Data is NodeData data)
            {
                MakeStraight.Visible = data.Style.SupportOffset.IsSet(SupportOption.Individually);
                var valueVisible = data.IsTwoRoads && data.IsSameRoad;

                CalculateShiftNearby.Visible = valueVisible;
                CalculateShiftIntersections.Visible = valueVisible;
                SetShiftIntersections.Visible = valueVisible;

                CalculateTwistNearby.Visible = valueVisible;
                CalculateTwistIntersections.Visible = valueVisible;
                SetTwistIntersections.Visible = valueVisible;
            }
            else
            {
                MakeStraight.Visible = false;

                CalculateShiftNearby.Visible = false;
                CalculateShiftIntersections.Visible = false;
                SetShiftIntersections.Visible = false;

                CalculateTwistNearby.Visible = false;
                CalculateTwistIntersections.Visible = false;
                SetTwistIntersections.Visible = false;
            }

            base.Refresh();
        }
    }
}
