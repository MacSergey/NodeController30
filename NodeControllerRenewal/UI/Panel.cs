using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ModsCommon.UI;
using ColossalFramework.UI;
using ModsCommon;

namespace NodeController.UI
{
    public class NodeControllerPanel : PropertyGroupPanel
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

        public INetworkData Data { get; private set; }
        private List<EditorItem> DataProperties { get; } = new List<EditorItem>();

        public NodeControllerPanel()
        {
            autoLayoutDirection = LayoutDirection.Vertical;
            autoFitChildrenVertically = true;
        }

        public override void Awake()
        {
            base.Awake();

            width = 300f;
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
            ClearDataProperties();
        }

        private void FillProperties()
        {
            StopLayout();

            var header = ComponentPool.Get<TextProperty>(this);
            header.Text = Data.Title;
            DataProperties.Add(header);

            StartLayout();
        }

        private void ClearDataProperties()
        {
            foreach (var property in DataProperties)
                ComponentPool.Free(property);

            DataProperties.Clear();
        }
    }
}
