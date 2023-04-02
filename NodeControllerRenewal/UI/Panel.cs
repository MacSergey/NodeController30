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
        private bool _showHidden;

        public NodeData Data { get; private set; }

        private static Color32 DefaultColor => new Color32(36, 40, 40, 255);
        private static Color32 ErrorColor => ComponentStyle.ErrorNormalColor;

        public NodeControllerPanel()
        {
            Atlas = CommonTextures.Atlas;
            BackgroundSprite = CommonTextures.PanelBig;
            BgColors = ComponentStyle.PanelColor;
            name = nameof(NodeControllerPanel);

            PauseLayout(() =>
            {
                AutoLayout = AutoLayout.Vertical;
                AutoChildrenHorizontally = AutoLayoutChildren.Fit;
                AutoChildrenVertically = AutoLayoutChildren.Fit;

                Header = ComponentPool.Get<PanelHeader>(this);
                Header.Target = this;
                Header.BackgroundSprite = "ButtonWhite";
                Header.BgColors = DefaultColor;
                Header.Init(HeaderHeight);

                Content = ComponentPool.Get<PropertyGroupPanel>(this);
                Content.minimumSize = new Vector2(300f, 0f);
                Content.BgColors = DefaultColor;
                Content.PaddingBottom = 7;
                Content.eventSizeChanged += (UIComponent component, Vector2 value) => size = value;
            });
        }

        public void SetData(NodeData data)
        {
            _showHidden = false;

            if ((Data = data) != null)
                SetPanel();
            else
                ResetPanel();
        }
        public void SetPanel()
        {
            PauseLayout(() =>
            {
                ResetPanel();

                Header.Text = Data.Title;
                RefreshHeader();

                var width = (Data.Style.TotalSupport == SupportOption.All ? Mathf.Max((Data.SegmentCount + 1) * 55f + 120f, 300f) : 300f) + 30f;
                Header.width = width;
                Content.width = width;

                Content.PauseLayout(() =>
                {
                    AddNodeTypeProperty();
                    FillProperties();
                });
            });
        }
        private void ResetPanel()
        {
            Content.PauseLayout(() =>
            {
                ComponentPool.Free(TypeProperty);
                ClearProperties();
            });
        }

        private void FillProperties() => Properties = Data.Style.GetUIComponents(Content, GetShowHidden, SetShowHidden);
        private void ClearProperties()
        {
            foreach (var property in Content.components.ToArray())
            {
                if (property != TypeProperty)
                    ComponentPool.Free(property);
            }

            Properties.Clear();
        }

        private void AddNodeTypeProperty()
        {
            TypeProperty = ComponentPool.Get<NodeTypePropertyPanel>(Content);
            TypeProperty.Label = NodeController.Localize.Option_Type;
            TypeProperty.SetStyle(UIStyle.Default);
            TypeProperty.Init(Data.IsPossibleType);
            TypeProperty.UseWheel = true;
            TypeProperty.WheelTip = true;
            TypeProperty.SelectedObject = Data.Type;
            TypeProperty.OnSelectObjectChanged += (value) =>
            {
                Data.Type = value;

                Content.PauseLayout(() =>
                {
                    ClearProperties();
                    FillProperties();
                    RefreshHeader();
                });
            };
        }

        private bool GetShowHidden() => _showHidden;
        private void SetShowHidden(bool show) => _showHidden = show;
        public void RefreshHeader() => Header.Refresh();
        public override void RefreshPanel()
        {
            RefreshHeader();
            Data.Style.RefreshUIComponents(Content, GetShowHidden, SetShowHidden);
        }

        public override void Update()
        {
            base.Update();
            Content.BgColors = Data == null || (Data.State & State.Fail) == 0 ? DefaultColor : ErrorColor;
            Header.BgColors = Data == null || Data.Mode != Mode.FreeForm ? DefaultColor : CommonColors.Orange;
        }
    }
    public class PanelHeader : HeaderMoveablePanel<PanelHeaderContent>
    {
        private HeaderButtonInfo<HeaderButton> MakeStraight { get; set; }

        private HeaderButtonInfo<HeaderButton> CalculateShiftNearby { get; set; }
        private HeaderButtonInfo<HeaderButton> CalculateShiftIntersections { get; set; }
        private HeaderButtonInfo<HeaderButton> SetShiftIntersections { get; set; }

        private HeaderButtonInfo<HeaderButton> CalculateTwistNearby { get; set; }
        private HeaderButtonInfo<HeaderButton> CalculateTwistIntersections { get; set; }
        private HeaderButtonInfo<HeaderButton> SetTwistIntersections { get; set; }

        protected override void FillContent()
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
        public void Init(float height) => base.Init(height);

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
