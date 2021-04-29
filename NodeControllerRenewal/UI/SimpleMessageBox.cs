using ModsCommon;
using ModsCommon.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeController.UI
{
    public class OkMessageBox : OneButtonMessageBox
    {
        public OkMessageBox()
        {
            ButtonText = ModLocalize<Mod>.Ok;
        }
    }
    public class YesNoMessageBox : TwoButtonMessageBox
    {
        public YesNoMessageBox()
        {
            Button1Text = ModLocalize<Mod>.Yes;
            Button2Text = ModLocalize<Mod>.No;
        }
    }

    public class BackwardСompatibilityMessageBox : OkMessageBox
    {
        public BackwardСompatibilityMessageBox()
        {
            Panel.StopLayout();

            Message.maximumSize = Vector2.zero;

            var warning = AddLabel();
            warning.textColor = Color.red;

            CaptionText = SingletonMod<Mod>.Instance.NameRaw;
            MessageText = NodeController.Localize.Mod_BackwardСompatibilityMessage;
            warning.text = NodeController.Localize.Mod_BackwardСompatibilityWarning;

            Panel.StartLayout();
        }
    }
}
