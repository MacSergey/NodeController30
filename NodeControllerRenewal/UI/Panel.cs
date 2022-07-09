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
            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeControllerTextures.Atlas, NodeControllerTextures.KeepDefaultHeaderButton, NodeController.Localize.Option_KeepDefault, EditNodeToolMode.ResetOffsetShortcut));
            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeControllerTextures.Atlas, NodeControllerTextures.ResetToDefaultHeaderButton, NodeController.Localize.Option_ResetToDefault, EditNodeToolMode.ResetToDefaultShortcut));

            MakeStraight = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, NodeControllerTextures.Atlas, NodeControllerTextures.MakeStraightHeaderButton, NodeController.Localize.Option_MakeStraightEnds, EditNodeToolMode.MakeStraightEndsShortcut);
            Content.AddButton(MakeStraight);

            CalculateShiftNearby = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, NodeControllerTextures.Atlas, NodeControllerTextures.CalculateShiftNearbyHeaderButton, NodeController.Localize.Option_CalculateShiftByNearby, EditNodeToolMode.CalculateShiftByNearbyShortcut);
            Content.AddButton(CalculateShiftNearby);

            CalculateShiftIntersections = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, NodeControllerTextures.Atlas, NodeControllerTextures.CalculateShiftIntersectionsHeaderButton, NodeController.Localize.Option_CalculateShiftByIntersections, EditNodeToolMode.CalculateShiftByIntersectionsShortcut);
            Content.AddButton(CalculateShiftIntersections);

            SetShiftIntersections = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, NodeControllerTextures.Atlas, NodeControllerTextures.SetShiftBetweenIntersectionsHeaderButton, NodeController.Localize.Option_SetShiftBetweenIntersections, EditNodeToolMode.SetShiftBetweenIntersectionsShortcut);
            Content.AddButton(SetShiftIntersections);

            CalculateTwistNearby = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, NodeControllerTextures.Atlas, NodeControllerTextures.CalculateTwistNearbyHeaderButton, NodeController.Localize.Option_CalculateTwistByNearby, EditNodeToolMode.CalculateTwistByNearbyShortcut);
            Content.AddButton(CalculateTwistNearby);

            CalculateTwistIntersections = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, NodeControllerTextures.Atlas, NodeControllerTextures.CalculateTwistIntersectionsHeaderButton, NodeController.Localize.Option_CalculateTwistByIntersections, EditNodeToolMode.CalculateTwistByIntersectionsShortcut);
            Content.AddButton(CalculateTwistIntersections);

            SetTwistIntersections = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, NodeControllerTextures.Atlas, NodeControllerTextures.SetTwistBetweenIntersectionsHeaderButton, NodeController.Localize.Option_SetTwistBetweenIntersections, EditNodeToolMode.SetTwistBetweenIntersectionsShortcut);
            Content.AddButton(SetTwistIntersections);
        }

        public override void Refresh()
        {
            if (SingletonTool<NodeControllerTool>.Instance.Data is NodeData data)
            {
                MakeStraight.Visible = data.Style.SupportOffset.IsSet(SupportOption.Individually);

                var shiftVisible = data.IsTwoRoads && data.IsSameRoad;
                CalculateShiftNearby.Visible = shiftVisible;
                CalculateShiftIntersections.Visible = shiftVisible;
                SetShiftIntersections.Visible = shiftVisible;

                var twistVisible = data.IsTwoRoads;
                CalculateTwistNearby.Visible = twistVisible;
                CalculateTwistIntersections.Visible = twistVisible;
                SetTwistIntersections.Visible = twistVisible;
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
