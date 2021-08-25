using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using NodeController.Utilities;
using UnityEngine;

namespace NodeController.UI
{
    public class NodeControllerButton : UUINetToolButton<Mod, NodeControllerTool>
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
