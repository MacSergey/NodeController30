using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeController.Utilities;
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
        private void SetDefaultPosition()
        {
            SingletonMod<Mod>.Logger.Debug($"Set default panel position");
            absolutePosition = DefaultPosition;
        }

        public void SetData(NodeData data)
        {
            if ((Data = data) != null)
                UpdatePanel();
            else
                ResetPanel();
        }
        public void UpdatePanel()
        {
            ResetPanel();
            Content.width = Data.Style.TotalSupport == SupportOption.All ? Mathf.Max((Data.SegmentCount + 1) * 55f + 100f, 300f) : 300f;
            FillProperties();
        }
        private void ResetPanel()
        {
            Content.StopLayout();

            foreach (var property in Content.components.ToArray())
                ComponentPool.Free(property);

            Content.StartLayout();
        }

        private void FillProperties()
        {
            Content.StopLayout();

            var header = ComponentPool.Get<PanelHeader>(Content);
            header.Text = Data.Title;
            header.Init();
            GetNodeTypeProperty();
            Data.Style.GetUIComponents(Content);

            Content.StartLayout();
        }
        private NodeTypePropertyPanel GetNodeTypeProperty()
        {
            var typeProperty = ComponentPool.Get<NodeTypePropertyPanel>(Content);
            typeProperty.Text = NodeController.Localize.Option_Type;
            typeProperty.Init(Data.IsPossibleType);
            typeProperty.SelectedObject = Data.Type;
            typeProperty.OnSelectObjectChanged += (value) =>
            {
                Data.Type = value;
                UpdatePanel();
            };

            return typeProperty;
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

        private void SetMakeStraightEnabled() => MakeStraight.isVisible = SingletonTool<NodeControllerTool>.Instance.Data.Style.SupportOffset.IsSet(SupportOption.Individually);
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
