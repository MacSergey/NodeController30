using KianCommons;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using JetBrains.Annotations;

namespace NodeController.Patches
{
    [UsedImplicitly]
    [HarmonyPatch]
    static class RefreshJunctionData
    {
        [UsedImplicitly]
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(
            typeof(NetNode),
            "RefreshJunctionData",
            new Type[] {
                typeof(ushort),
                typeof(int),
                typeof(ushort),
                typeof(Vector3),
                typeof(uint).MakeByRefType(),
                typeof(RenderManager.Instance).MakeByRefType()
            });
        }

        [UsedImplicitly]
        static void Postfix(ref NetNode __instance, ref RenderManager.Instance data)
        {
            ushort nodeID = __instance.GetID();
            NodeData blendData = NodeManager.Instance.buffer[nodeID];
            if (blendData == null)
                return;

            if (blendData.ShouldRenderCenteralCrossingTexture())
            {
                // puts crossings in the center.
                data.m_dataVector1.w = 0.01f;
            }
        }
    }
}