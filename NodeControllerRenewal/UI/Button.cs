using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using NodeController.Utilities;
using UnityEngine;
using static NodeController.Utilities.NodeControllerTextures;

namespace NodeController.UI
{
    public class NodeControllerButton : UUINetToolButton<Mod, NodeControllerTool>
    {
        protected override Vector2 ButtonPosition => new Vector3(94, 38);
        protected override UITextureAtlas DefaultAtlas => NodeControllerTextures.Atlas;
        protected override SpriteSet DefaultBgSprite => new SpriteSet(ActivationButtonNormal, ActivationButtonHover, ActivationButtonHover, ActivationButtonActive, string.Empty);
        protected override SpriteSet DefaultFgSprite => new SpriteSet(ActivationButtonIconNormal, ActivationButtonIconHover, ActivationButtonIconNormal, ActivationButtonIconNormal, string.Empty);
    }
}
