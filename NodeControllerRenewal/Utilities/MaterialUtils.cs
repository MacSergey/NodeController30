using System;
using System.Linq;
using UnityEngine;

// TODO check out material.MainTextureScale
// regarding weird nodes, what if we return a copy of the material?
// Loading screens Mod owner wrote this about LODs: https://steamcommunity.com/workshop/filedetails/discussion/667342976/1636416951459546732/
namespace NodeController.Util
{
    using static TextureUtils;
    public static class MaterialUtils
    {
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

        public static NetInfo.Segment GetSegment(NetInfo info, int textureID)
        {
            foreach (var segmentInfo in info.m_segments ?? Enumerable.Empty<NetInfo.Segment>())
            {
                if (segmentInfo.m_segmentMaterial.TryGetTexture2D(textureID) != null)
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

            var segment = GetSegment(info, ID_APRMap);
            var segMaterial = segment.m_material;

            material = new Material(material);

            if (segMaterial?.TryGetTexture2D(ID_Defuse) is Texture2D defuse)
                material.SetTexture(ID_Defuse, defuse);

            if (segMaterial?.TryGetTexture2D(ID_APRMap) is Texture2D apr)
                material.SetTexture(ID_APRMap, apr);

            if (segMaterial?.TryGetTexture2D(ID_XYSMap) is Texture2D xys)
                material.SetTexture(ID_XYSMap, xys);

            return material;
        }

        public static Mesh ContinuesMedian(Mesh mesh, NetInfo info)
        {
            if (mesh == null)
                throw new ArgumentNullException("mesh");
            if (info == null)
                throw new ArgumentNullException("info");

            return GetSegment(info, ID_APRMap)?.m_mesh ?? mesh;
        }
    }
}

