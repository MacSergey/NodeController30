using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ModsCommon.UI;
using ColossalFramework.UI;
using ModsCommon;
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

        public bool Active
        {
            get => enabled && isVisible;
            set
            {
                enabled = value;
                isVisible = value;
            }
        }

        private PropertyGroupPanel Content { get; set; }
        public INetworkData Data { get; private set; }
        private List<EditorItem> DataProperties { get; } = new List<EditorItem>();

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

        public void SetData(INetworkData data)
        {
            if ((Data = data) != null)
                UpdatePanel();
            else
                ResetPanel();
        }
        private void UpdatePanel()
        {
            ClearDataProperties();
            FillProperties();
        }
        private void ResetPanel()
        {
            Content.StopLayout();

            foreach (var property in Content.components.ToArray())
                ComponentPool.Free(property);

            DataProperties.Clear();

            Content.StartLayout();
        }

        private void FillProperties()
        {
            Content.StopLayout();

            var header = ComponentPool.Get<TextProperty>(Content);
            header.Text = Data.Title;
            DataProperties.Add(header);
            DataProperties.AddRange(Data.GetUIComponents(Content, UpdatePanel));

            Content.StartLayout();
        }

        private void ClearDataProperties()
        {
            Content.StopLayout();

            foreach (var property in DataProperties)
                ComponentPool.Free(property);

            DataProperties.Clear();

            Content.StartLayout();
        }
    }
}
