using ModsCommon.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static ModsCommon.UI.ComponentStyle;
using static ModsCommon.Utilities.CommonTextures;

namespace NodeController.UI
{
    public static class UIStyle
    {
        public static Color32 PropertyNormal => DarkPrimaryColor45;
        public static Color32 PropertyHovered => DarkPrimaryColor55;
        public static Color32 PropertyPressed => DarkPrimaryColor60;
        public static Color32 PropertyFocused => NormalBlue;

        public static Color32 TextDisabled => DarkPrimaryColor70;

        public static Color32 PopupBackground => DarkPrimaryColor15;
        public static Color32 PopupEntitySelected => NormalBlue;
        public static Color32 PopupEntityHovered => DarkPrimaryColor50;
        public static Color32 PopupEntityPressed => DarkPrimaryColor60;

        public static ControlStyle Default { get; } = new ControlStyle()
        {
            TextField = new TextFieldStyle()
            {
                BgAtlas = Atlas,
                FgAtlas = Atlas,

                BgSprites = new SpriteSet(FieldSingle, FieldSingle, FieldSingle, FieldSingle, BorderSmall),
                BgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyHovered, PropertyNormal, PropertyNormal),

                FgSprites = new SpriteSet(default, default, default, BorderSmall, default),
                FgColors = new ColorSet(default, default, default, PropertyFocused, default),

                TextColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, TextDisabled),

                SelectionSprite = Empty,
                SelectionColor = PropertyFocused,
            },
            Segmented = new SegmentedStyle()
            {
                Single = GetSegmentedStyle(FieldSingle, BorderSmall),
                Left = GetSegmentedStyle(FieldLeft, FieldBorderLeft),
                Middle = GetSegmentedStyle(FieldMiddle, FieldBorderMiddle),
                Right = GetSegmentedStyle(FieldRight, FieldBorderRight),
            },
            Button = new ButtonStyle()
            {
                BgAtlas = Atlas,
                FgAtlas = Atlas,

                BgSprites = new SpriteSet(FieldSingle, FieldSingle, FieldSingle, FieldSingle, BorderSmall),
                BgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyPressed, PropertyNormal, PropertyNormal),
                SelBgColors = new ColorSet(),

                FgSprites = default,
                FgColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, Color.black),
                SelFgColors = default,

                TextColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, Color.black),
                SelTextColors = default,
            },
            DropDown = new DropDownStyle()
            {
                BgAtlas = Atlas,
                FgAtlas = Atlas,

                AllBgSprites = new SpriteSet(FieldSingle, FieldSingle, FieldSingle, FieldSingle, BorderSmall),
                BgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyHovered, PropertyNormal, PropertyNormal),
                SelBgColors = PropertyFocused,

                FgSprites = new SpriteSet(VectorDown, VectorDown, VectorDown, VectorDown, default),
                FgColors = Color.white,

                AllTextColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, Color.black),


                PopupAtlas = Atlas,
                PopupSprite = FieldSingle,
                PopupColor = PopupBackground,
                PopupItemsPadding = new RectOffset(4, 4, 4, 4),


                EntityAtlas = Atlas,

                EntitySprites = new SpriteSet(default, FieldSingle, FieldSingle, default, default),
                EntitySelSprites = FieldSingle,

                EntityColors = new ColorSet(default, PopupEntityHovered, PopupEntityPressed, default, default),
                EntitySelColors = PopupEntitySelected,
            },
            Label = new LabelStyle()
            {
                NormalTextColor = TextDisabled,
                DisabledTextColor = TextDisabled,
            },
        };
        private static ButtonStyle GetSegmentedStyle(string background, string border)
        {
            return new ButtonStyle()
            {
                BgAtlas = Atlas,
                FgAtlas = Atlas,

                BgSprites = new SpriteSet(background, background, background, background, border),
                BgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyHovered, PropertyFocused, PropertyNormal),
                SelBgSprites = background,
                SelBgColors = PropertyFocused,

                FgColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, PropertyNormal),
                SelFgColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, Color.white),

                TextColors = Color.white,
                SelTextColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, Color.white),
            };
        }
    }
}
