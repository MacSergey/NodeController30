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
                MessageBox.Show<BackwardCompatibilityMessageBox>();

            if (Manager.HasErrors)
            {
                var messageBox = MessageBox.Show<ErrorSupportMessageBox>();
                messageBox.Init<Mod>();
                messageBox.MessageText = Manager.Errors > 0 ? string.Format(Localize.Mod_LoadFailed, Manager.Errors) : Localize.Mod_LoadFailedAll;
            }

            base.OnLoad();
        }
        protected override void OnUnload()
        {
            base.OnUnload();
            SingletonManager<Manager>.Destroy();
        }
    }
}
