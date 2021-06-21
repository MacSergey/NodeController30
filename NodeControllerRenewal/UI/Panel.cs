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
    public class NodeControllerPanel : CustomUIPanel
    {
        public static void CreatePanel()
        {
            SingletonMod<Mod>.Logger.Debug($"Create panel");
            SingletonItem<NodeControllerPanel>.Instance = UIView.GetAView().AddUIComponent(typeof(NodeControllerPanel)) as NodeControllerPanel;
            SingletonMod<Mod>.Logger.Debug($"Panel created");
        }
        public static void RemovePanel()
        {
            SingletonMod<Mod>.Logger.Debug($"Remove panel");
            if (SingletonItem<NodeControllerPanel>.Instance is NodeControllerPanel panel)
            {
                panel.Hide();
                Destroy(panel);
                SingletonItem<NodeControllerPanel>.Instance = null;
                SingletonMod<Mod>.Logger.Debug($"Panel removed");
            }
        }
        private static Vector2 DefaultPosition { get; } = new Vector2(100f, 100f);

        public bool Active
        {
            get => enabled && isVisible;
            set
            {
                enabled = value;
                isVisible = value;
            }
        }
        public bool IsHover => (isVisible && this.IsHover(SingletonTool<NodeControllerTool>.Instance.MousePosition)) || components.Any(c => c.isVisible && c.IsHover(SingletonTool<NodeControllerTool>.Instance.MousePosition));

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

        public override void Awake()
        {
            base.Awake();
            Active = false;
        }
        public override void Start()
        {
            base.Start();
            SetDefaultPosition();
        }
        public override void OnEnable()
        {
            base.OnEnable();

            if (absolutePosition.x < 0 || absolutePosition.y < 0)
                SetDefaultPosition();
        }
        protected override void OnVisibilityChanged()
        {
            base.OnVisibilityChanged();

            if (isVisible)
                RefreshPanel();
        }
        private void SetDefaultPosition()
        {
            SingletonMod<Mod>.Logger.Debug($"Set default panel position");
            absolutePosition = DefaultPosition;
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
        public void RefreshPanel()
        {
            RefreshHeader();

            foreach (var property in Properties.OfType<IOptionPanel>())
                property.Refresh();
        }
    }
    public class PanelHeader : HeaderMoveablePanel<PanelHeaderContent>
    {
        protected override float DefaultHeight => 40f;
    }
    public class PanelHeaderContent : BaseHeaderContent
    {
        private HeaderButtonInfo<BasePanelHeaderButton> MakeStraight { get; set; }
        private HeaderButtonInfo<BasePanelHeaderButton> CalculateShiftNearby { get; set; }
        private HeaderButtonInfo<BasePanelHeaderButton> CalculateShiftIntersections { get; set; }
        private HeaderButtonInfo<BasePanelHeaderButton> SetShiftIntersections { get; set; }

        protected override IEnumerable<IHeaderButtonInfo> GetInfos()
        {
            yield return new HeaderButtonInfo<BasePanelHeaderButton>(HeaderButtonState.Main, NodeControllerTextures.Atlas, NodeControllerTextures.KeepDefault, NodeController.Localize.Option_KeepDefault, OnKeepDefault);
            yield return new HeaderButtonInfo<BasePanelHeaderButton>(HeaderButtonState.Main, NodeControllerTextures.Atlas, NodeControllerTextures.ResetToDefault, NodeController.Localize.Option_ResetToDefault, OnResetToDefault);

            MakeStraight = new HeaderButtonInfo<BasePanelHeaderButton>(HeaderButtonState.Main, NodeControllerTextures.Atlas, NodeControllerTextures.MakeStraight, NodeController.Localize.Option_MakeStraightEnds, OnMakeStraightClick);
            yield return MakeStraight;

            CalculateShiftNearby = new HeaderButtonInfo<BasePanelHeaderButton>(HeaderButtonState.Additional, NodeControllerTextures.Atlas, NodeControllerTextures.CalculateShiftNearby, "Calculate shift by nearby", OnCalculateShiftByNearbyClick);
            yield return CalculateShiftNearby;

            CalculateShiftIntersections = new HeaderButtonInfo<BasePanelHeaderButton>(HeaderButtonState.Additional, NodeControllerTextures.Atlas, NodeControllerTextures.CalculateShiftIntersections, "Calculate shift by intersections", OnCalculateShiftByIntersectionsClick);
            yield return CalculateShiftIntersections;

            SetShiftIntersections = new HeaderButtonInfo<BasePanelHeaderButton>(HeaderButtonState.Additional, NodeControllerTextures.Atlas, string.Empty, "Set shift between intersections", OnSetShiftBetweenIntersectionsClick);
            yield return SetShiftIntersections;
        }

        private void OnKeepDefault() => SingletonTool<NodeControllerTool>.Instance.SetKeepDefaults();
        private void OnResetToDefault() => SingletonTool<NodeControllerTool>.Instance.ResetToDefault();
        private void OnMakeStraightClick() => SingletonTool<NodeControllerTool>.Instance.MakeStraightEnds();
        private void OnCalculateShiftByNearbyClick() => SingletonTool<NodeControllerTool>.Instance.CalculateShiftByNearby();
        private void OnCalculateShiftByIntersectionsClick() => SingletonTool<NodeControllerTool>.Instance.CalculateShiftByIntersections();
        private void OnSetShiftBetweenIntersectionsClick() => SingletonTool<NodeControllerTool>.Instance.SetShiftBetweenIntersections();

        public override void Refresh()
        {
            SetMakeStraightEnabled();
            base.Refresh();
        }

        private void SetMakeStraightEnabled()
        {
            if (SingletonTool<NodeControllerTool>.Instance.Data is NodeData data)
            {
                MakeStraight.Visible = data.Style.SupportOffset.IsSet(SupportOption.Individually);
                var shiftVisible = data.IsTwoRoads && data.IsSameRoad;
                CalculateShiftNearby.Visible = shiftVisible;
                CalculateShiftIntersections.Visible = shiftVisible;
                SetShiftIntersections.Visible = shiftVisible;
            }
            else
            {
                MakeStraight.Visible = false;
                CalculateShiftNearby.Visible = false;
                CalculateShiftIntersections.Visible = false;
                SetShiftIntersections.Visible = false;
            }
        }
    }
}
