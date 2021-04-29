using System;
using System.Linq;
using UnityEngine;

// TODO check out material.MainTextureScale
// regarding weird nodes, what if we return a copy of the material?
// Loading screens Mod owner wrote this about LODs: https://steamcommunity.com/workshop/filedetails/discussion/667342976/1636416951459546732/
namespace NodeController.Utilities
{
    public static class MaterialUtilities
    {
        internal static int ID_Defuse => NetManager.instance.ID_MainTex;
        internal static int ID_APRMap => NetManager.instance.ID_APRMap;
        internal static int ID_XYSMap => NetManager.instance.ID_XYSMap;

        public static Texture2D TryGetTexture2D(this Material material, int textureID)
        {
            try
            {
                if (material.HasProperty(textureID) && material.GetTexture(textureID) is Texture2D texture)
                    return texture;
            }
            catch { }
            return null;
        }

        public static NetInfo.Segment GetSegment(NetInfo info)
        {
            foreach (var segmentInfo in info.m_segments ?? Enumerable.Empty<NetInfo.Segment>())
            {
                if (segmentInfo.m_segmentMaterial.TryGetTexture2D(ID_APRMap) != null)
                    return segmentInfo;
            }
            return null;
        }

        public static Material ContinuesMedian(Material material, NetInfo info, bool lod = false)
        {
            if (material == null)
                throw new ArgumentNullException("material");
            if (info == null)
                throw new ArgumentNullException("info");

            if (GetSegment(info) is not NetInfo.Segment segment)
                return material;

            var segmentMaterial = segment.m_material;

            material = new Material(material);

            if (segmentMaterial?.TryGetTexture2D(ID_Defuse) is Texture2D defuse)
                material.SetTexture(ID_Defuse, defuse);

            if (segmentMaterial?.TryGetTexture2D(ID_APRMap) is Texture2D apr)
                material.SetTexture(ID_APRMap, apr);

            if (segmentMaterial?.TryGetTexture2D(ID_XYSMap) is Texture2D xys)
                material.SetTexture(ID_XYSMap, xys);

            return material;
        }

        public static Mesh ContinuesMedian(Mesh mesh, NetInfo info)
        {
            if (mesh == null)
                throw new ArgumentNullException("mesh");
            if (info == null)
                throw new ArgumentNullException("info");

            return GetSegment(info)?.m_mesh ?? mesh;
        }
    }
}

