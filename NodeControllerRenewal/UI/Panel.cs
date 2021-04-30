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
            Content = ComponentPool.Get<PropertyGroupPanel>(this);
            Content.minimumSize = new Vector2(300f, 0f);
            Content.color = new Color32(72, 80, 80, 255);
            Content.autoLayoutDirection = LayoutDirection.Vertical;
            Content.autoFitChildrenVertically = true;
            Content.eventSizeChanged += (UIComponent component, Vector2 value) => size = value;
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
                UpdatePanel();
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
        private void SetPanel()
        {
            Content.StopLayout();

            Content.width = Data.Style.TotalSupport == SupportOption.All ? Mathf.Max((Data.SegmentCount + 1) * 55f + 120f, 300f) : 300f;
            AddHeader();
            AddNodeTypeProperty();

            FillProperties();

            Content.StartLayout();
        }
        private void ResetPanel()
        {
            Content.StopLayout();

            ComponentPool.Free(Header);
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

        private void AddHeader()
        {
            Header = ComponentPool.Get<PanelHeader>(Content);
            Header.Text = Data.Title;
            Header.Target = this;
            Header.Init();
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

                Content.StartLayout();
            };
        }
        public void UpdatePanel()
        {
            foreach (var property in Properties.OfType<IOptionPanel>())
                property.Refresh();
        }
    }
    public class PanelHeader : HeaderMoveablePanel<PanelHeaderContent>
    {
        protected override float DefaultHeight => 40f;
    }
    public class PanelHeaderContent : BasePanelHeaderContent<PanelHeaderButton, AdditionallyHeaderButton>
    {
        private PanelHeaderButton MakeStraight { get; set; }

        protected override void AddButtons()
        {
            AddButton(NodeControllerTextures.Reset, NodeController.Localize.Option_ResetToDefault, OnResetClick);
            MakeStraight = AddButton(NodeControllerTextures.MakeStraight, NodeController.Localize.Option_MakeStraightEnds, OnMakeStraightClick);

            SetMakeStraightEnabled();
        }

        private void OnResetClick(UIComponent component, UIMouseEventParameter eventParam) => SingletonTool<NodeControllerTool>.Instance.ResetToDefault();
        private void OnMakeStraightClick(UIComponent component, UIMouseEventParameter eventParam) => SingletonTool<NodeControllerTool>.Instance.MakeStraightEnds();

        private void SetMakeStraightEnabled()
        {
            if (SingletonTool<NodeControllerTool>.Instance.Data is NodeData data)
                MakeStraight.isVisible = data.Style.SupportOffset.IsSet(SupportOption.Individually);
            else
                MakeStraight.isVisible = false;
        }
    }
    public class PanelHeaderButton : BasePanelHeaderButton
    {
        protected override UITextureAtlas IconAtlas => NodeControllerTextures.Atlas;
    }
    public class AdditionallyHeaderButton : BaseAdditionallyHeaderButton
    {
        protected override UITextureAtlas IconAtlas => NodeControllerTextures.Atlas;
    }
}
