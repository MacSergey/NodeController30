using ColossalFramework.UI;
using ModsCommon.UI;
using NodeController.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeController.UI
{
    public class NodeControllerButton : NetToolButton<Mod, NodeControllerTool>
    {
        protected override Vector2 ButtonPosition => new Vector3(94, 38);
        protected override UITextureAtlas Atlas => NodeControllerTextures.Atlas;

        protected override string NormalBgSprite => NodeControllerTextures.ButtonNormal;
        protected override string HoveredBgSprite => NodeControllerTextures.ButtonHover;
        protected override string PressedBgSprite => NodeControllerTextures.ButtonHover;
        protected override string FocusedBgSprite => NodeControllerTextures.ButtonActive;
        protected override string NormalFgSprite => NodeControllerTextures.Icon;
        protected override string HoveredFgSprite => NodeControllerTextures.IconHover;
        protected override string PressedFgSprite => NodeControllerTextures.Icon;
        protected override string FocusedFgSprite => NodeControllerTextures.Icon;
    }
}
