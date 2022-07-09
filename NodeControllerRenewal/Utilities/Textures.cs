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

        public static string ActivationButtonNormal => nameof(ActivationButtonNormal);
        public static string ActivationButtonActive => nameof(ActivationButtonActive);
        public static string ActivationButtonHover => nameof(ActivationButtonHover);
        public static string ActivationButtonIconNormal => nameof(ActivationButtonIconNormal);
        public static string ActivationButtonIconActive => nameof(ActivationButtonIconActive);
        public static string ActivationButtonIconHover => nameof(ActivationButtonIconHover);

        public static string UUIButtonNormal => nameof(UUIButtonNormal);
        public static string UUIButtonHovered => nameof(UUIButtonHovered);
        public static string UUIButtonPressed => nameof(UUIButtonPressed);

        public static string KeepDefaultHeaderButton => nameof(KeepDefaultHeaderButton);
        public static string ResetToDefaultHeaderButton => nameof(ResetToDefaultHeaderButton);
        public static string MakeStraightHeaderButton => nameof(MakeStraightHeaderButton);
        public static string CalculateShiftNearbyHeaderButton => nameof(CalculateShiftNearbyHeaderButton);
        public static string CalculateShiftIntersectionsHeaderButton => nameof(CalculateShiftIntersectionsHeaderButton);
        public static string SetShiftBetweenIntersectionsHeaderButton => nameof(SetShiftBetweenIntersectionsHeaderButton);
        public static string CalculateTwistNearbyHeaderButton => nameof(CalculateTwistNearbyHeaderButton);
        public static string CalculateTwistIntersectionsHeaderButton => nameof(CalculateTwistIntersectionsHeaderButton);
        public static string SetTwistBetweenIntersectionsHeaderButton => nameof(SetTwistBetweenIntersectionsHeaderButton);

        static NodeControllerTextures()
        {
            var spriteParams = new Dictionary<string, RectOffset>();

            //ActivationButton
            spriteParams[ActivationButtonNormal] = new RectOffset();
            spriteParams[ActivationButtonActive] = new RectOffset();
            spriteParams[ActivationButtonHover] = new RectOffset();
            spriteParams[ActivationButtonIconNormal] = new RectOffset();
            spriteParams[ActivationButtonIconActive] = new RectOffset();
            spriteParams[ActivationButtonIconHover] = new RectOffset();

            //UUIButton
            spriteParams[UUIButtonNormal] = new RectOffset();
            spriteParams[UUIButtonHovered] = new RectOffset();
            spriteParams[UUIButtonPressed] = new RectOffset();

            //HeaderButtons
            spriteParams[KeepDefaultHeaderButton] = new RectOffset();
            spriteParams[ResetToDefaultHeaderButton] = new RectOffset();
            spriteParams[MakeStraightHeaderButton] = new RectOffset();
            spriteParams[CalculateShiftNearbyHeaderButton] = new RectOffset();
            spriteParams[CalculateShiftIntersectionsHeaderButton] = new RectOffset();
            spriteParams[SetShiftBetweenIntersectionsHeaderButton] = new RectOffset();
            spriteParams[CalculateTwistNearbyHeaderButton] = new RectOffset();
            spriteParams[CalculateTwistIntersectionsHeaderButton] = new RectOffset();
            spriteParams[SetTwistBetweenIntersectionsHeaderButton] = new RectOffset();

            Atlas = TextureHelper.CreateAtlas(nameof(NodeController), spriteParams);
        }
    }
}
