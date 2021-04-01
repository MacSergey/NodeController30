namespace NodeController.Util {
    using System;
    using UnityEngine;
    using ColossalFramework.UI;
    using KianCommons;

    public static class TextureUtils {
        public delegate Texture2D TProcessor(Texture2D tex);
        public delegate Texture2D TProcessor2(Texture2D tex, Texture2D tex2);
        internal static int ID_Defuse => NetManager.instance.ID_MainTex;
        internal static int ID_APRMap => NetManager.instance.ID_APRMap;
        internal static int ID_XYSMap => NetManager.instance.ID_XYSMap;
        internal static string getTexName(int id) {
            if (id == ID_Defuse) return "_MainTex";
            if (id == ID_APRMap) return "_APRMap";
            if (id == ID_XYSMap) return "_XYSMap";
            throw new Exception("Bad Texture ID");
        }
        internal static int[] texIDs => new int[] { ID_Defuse, ID_APRMap, ID_XYSMap };

        public static UITextureAtlas GetAtlas(string name) {
            UITextureAtlas[] atlases = Resources.FindObjectsOfTypeAll(typeof(UITextureAtlas)) as UITextureAtlas[];
            foreach(var atlas in atlases) {
                if (atlas.name == name)
                    return atlas;
            }
            return null;
            
        }

        /// <summary>
        /// reteurns a copy of the texture with the differenc that: mipmap=false, linear=false, readable=true;
        /// </summary>
        public static Texture2D GetReadableCopy(this Texture2D tex, bool linear = false) {
            Assertion.Assert(tex != null, "tex!=null");
            Assertion.Assert(tex is Texture2D, $"tex is Texture2D");
            Texture2D ret = tex.MakeReadable(linear);
            ret.name = tex.name;
            ret.anisoLevel = tex.anisoLevel;
            ret.filterMode = tex.filterMode;
            return ret;
        }

        public static Texture2D MakeReadable(this Texture texture, bool linear) {
            RenderTextureReadWrite RW_mode = linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.Default;
            RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RW_mode);
            Graphics.Blit(texture, rt);
            texture = rt.ToTexture2D();
            RenderTexture.ReleaseTemporary(rt);
            return texture as Texture2D;
        }

        public static bool IsReadable(this Texture2D texture) {
            try {
                texture.GetPixel(0, 0);
                return true;
            }
            catch {
                return false;
            }
        }

        public static Texture2D TryMakeReadable(this Texture2D texture) {
            if (texture.IsReadable())
                return texture;
            else
                return texture.MakeReadable();
        }

        public static void Finalize(this Texture2D texture, bool lod = false) {
            texture.Compress(true);
            if (lod) texture.Apply();
            else texture.Apply(true, true);
        }
    }
}
