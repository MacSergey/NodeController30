using ColossalFramework.UI;
using ModsCommon.UI;
using System.Collections.Generic;

namespace NodeController
{
    public interface INetworkData
    {
        public string Title { get; }
        bool IsDefault { get; }

        void Update();
        void Calculate();
        void ResetToDefault();
        void RefreshAndUpdate();

        public List<EditorItem> GetUIComponents(UIComponent parent);
    }

    public interface INetworkData<T>
    {
        T Clone();
    }
}
