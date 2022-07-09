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

        protected override string NormalBgSprite => NodeControllerTextures.ActivationButtonNormal;
        protected override string HoveredBgSprite => NodeControllerTextures.ActivationButtonHover;
        protected override string PressedBgSprite => NodeControllerTextures.ActivationButtonHover;
        protected override string FocusedBgSprite => NodeControllerTextures.ActivationButtonActive;
        protected override string NormalFgSprite => NodeControllerTextures.ActivationButtonIconNormal;
        protected override string HoveredFgSprite => NodeControllerTextures.ActivationButtonIconHover;
        protected override string PressedFgSprite => NodeControllerTextures.ActivationButtonIconNormal;
        protected override string FocusedFgSprite => NodeControllerTextures.ActivationButtonIconNormal;
    }
}
