using ColossalFramework.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public static string CursorEdit => nameof(CursorEdit);
        public static string CursorError => nameof(CursorError);
        public static string CursorInsert => nameof(CursorInsert);
        public static string CursorCrossing => nameof(CursorCrossing);
        public static string CursorMove => nameof(CursorMove);
        public static string CursorSearch => nameof(CursorSearch);

        private static Dictionary<string, TextureHelper.SpriteParamsGetter> Files { get; } = new Dictionary<string, TextureHelper.SpriteParamsGetter>
        {
            {nameof(Button), Button},
            {nameof(Cursor), Cursor},
        };

        static NodeControllerTextures()
        {
            Atlas = TextureHelper.CreateAtlas(nameof(NodeMarkup), Files);
        }

        private static UITextureAtlas.SpriteInfo[] Button(int texWidth, int texHeight, Rect rect) => TextureHelper.GetSpritesInfo(texWidth, texHeight, rect, 31, 31, ButtonNormal, ButtonActive, ButtonHover, Icon, IconActive, IconHover).ToArray();
        private static UITextureAtlas.SpriteInfo[] Cursor(int texWidth, int texHeight, Rect rect) => TextureHelper.GetSpritesInfo(texWidth, texHeight, rect, 31, 31, CursorEdit, CursorError, CursorInsert, CursorCrossing, CursorMove, CursorSearch).ToArray();
    }
}
