using ColossalFramework.UI;
using ModsCommon.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeController.Utilities
{
    public static class NodeControllerTextures
    {
        public static UITextureAtlas Atlas;
        public static Texture2D Texture => Atlas.texture;

        public static string ButtonNormal => nameof(ButtonNormal);
        public static string ButtonActive => nameof(ButtonActive);
        public static string ButtonHover => nameof(ButtonHover);
        public static string Icon => nameof(Icon);
        public static string IconActive => nameof(IconActive);
        public static string IconHover => nameof(IconHover);

        public static string KeepDefault => nameof(KeepDefault);
        public static string ResetToDefault => nameof(ResetToDefault);
        public static string MakeStraight => nameof(MakeStraight);
        public static string CalculateShiftNearby => nameof(CalculateShiftNearby);
        public static string CalculateShiftIntersections => nameof(CalculateShiftIntersections);
        public static string SetShiftBetweenIntersections => nameof(SetShiftBetweenIntersections);

        private static Dictionary<string, TextureHelper.SpriteParamsGetter> Files { get; } = new Dictionary<string, TextureHelper.SpriteParamsGetter>
        {
            {nameof(Button), Button},
            {nameof(HeaderButtons), HeaderButtons},
        };

        static NodeControllerTextures()
        {
            Atlas = TextureHelper.CreateAtlas(nameof(NodeController), Files);
        }

        private static UITextureAtlas.SpriteInfo[] Button(int texWidth, int texHeight, Rect rect) => TextureHelper.GetSpritesInfo(texWidth, texHeight, rect, 31, 31, ButtonNormal, ButtonActive, ButtonHover, Icon, IconActive, IconHover).ToArray();

        private static UITextureAtlas.SpriteInfo[] HeaderButtons(int texWidth, int texHeight, Rect rect) => TextureHelper.GetSpritesInfo(texWidth, texHeight, rect, 25, 25, new RectOffset(4, 4, 4, 4), 2, KeepDefault, MakeStraight, ResetToDefault, CalculateShiftNearby, CalculateShiftIntersections, SetShiftBetweenIntersections).ToArray();
    }
}
