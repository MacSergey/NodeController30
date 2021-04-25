using ColossalFramework;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeController.Utilities
{
    public class NodeControllerShortcut : BaseShortcut<Mod>
    {
        public NodeControllerShortcut(string name, string labelKey, InputKey key, Action action = null) : base(name, labelKey, key, action) { }
    }
}
