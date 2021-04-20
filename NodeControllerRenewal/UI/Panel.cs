using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ModsCommon.UI;
using ColossalFramework.UI;
using ModsCommon;
using UnityEngine;
using ModsCommon.Utilities;

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
        public INetworkData Data { get; private set; }

        public NodeControllerPanel()
        {
            Content = ComponentPool.Get<PropertyGroupPanel>(this);
            Content.autoLayoutDirection = LayoutDirection.Vertical;
            Content.autoFitChildrenVertically = true;
            Content.eventSizeChanged += (UIComponent component, Vector2 value) => size = value;
        }

        public override void Awake()
        {
            base.Awake();

            Content.width = 300f;
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

        public void SetData(INetworkData data)
        {
            if ((Data = data) != null)
                UpdatePanel();
            else
                ResetPanel();
        }
        public void UpdatePanel()
        {
            ResetPanel();
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
            Data.GetUIComponents(Content, UpdatePanel);

            Content.StartLayout();
        }
    }
    public class PanelHeader : HeaderMoveablePanel<BaseHeaderContent> 
    {
        protected override float DefaultHeight => 40f;
    }
}
