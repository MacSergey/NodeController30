using ColossalFramework.UI;
using ModsCommon.UI;
using System;
using System.Collections.Generic;

namespace NodeController
{
    public interface INetworkData
    {
        public string Title { get; }
        bool IsDefault { get; }

        public float Offset { get; set; }
        public float Shift { get; set; }
        public float RotateAngle { get; set; }
        public float SlopeAngle { get; set; }
        public float TwistAngle { get; set; }

        void Update();
        void Calculate();
        void ResetToDefault();
        void RefreshAndUpdate();

        public void GetUIComponents(UIComponent parent, Action refresh);
    }

    public interface INetworkData<T>
    {
        T Clone();
    }
}
