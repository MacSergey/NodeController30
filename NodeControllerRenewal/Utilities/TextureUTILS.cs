namespace NodeController.Util
{
    using System;
    using UnityEngine;
    using ColossalFramework.UI;
    using KianCommons;

    public static class TextureUtils
    {
        public delegate Texture2D TProcessor(Texture2D tex);
        public delegate Texture2D TProcessor2(Texture2D tex, Texture2D tex2);
        internal static int ID_Defuse => NetManager.instance.ID_MainTex;
        internal static int ID_APRMap => NetManager.instance.ID_APRMap;
        internal static int ID_XYSMap => NetManager.instance.ID_XYSMap;
    }
}
