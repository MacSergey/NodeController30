using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeController
{
    public class LoadingExtension : BaseLoadingExtension<Mod> 
    {
        protected override void OnLoad()
        {
            SingletonMod<Mod>.Instance.ShowLoadWarning();
            base.OnLoad();
        }
    }
}
