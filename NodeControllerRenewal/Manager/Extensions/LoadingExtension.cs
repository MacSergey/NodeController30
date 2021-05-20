using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeController.UI;
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
            if (SingletonItem<SerializableDataExtension>.Instance.WasImported)
                MessageBox.Show<BackwardСompatibilityMessageBox>();

            base.OnLoad();
        }
        protected override void OnUnload()
        {
            base.OnUnload();
            SingletonManager<Manager>.Destroy();
        }
    }
}
