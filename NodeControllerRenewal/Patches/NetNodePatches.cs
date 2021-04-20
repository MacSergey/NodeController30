using HarmonyLib;
using ModsCommon.Utilities;
using NodeController.Utilities;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace NodeController.Patches
{
    public static class NetNodePatches
    {
        public static void RefreshJunctionDataPrefix(ushort nodeID, ref Vector3 centerPos)
        {
            if (Manager.Instance[nodeID] is NodeData data)
                centerPos = data.Position;
        }

        public static void RefreshJunctionDataPostfix(ushort nodeID, ref RenderManager.Instance data)
        {
            if (Manager.Instance[nodeID] is NodeData blendData && blendData.ShouldRenderCenteralCrossingTexture)
                data.m_dataVector1.w = 0.01f;
        }

        public static IEnumerable<CodeInstruction> RefreshJunctionDataTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var minCornerOffsetField = AccessTools.Field(typeof(NetInfo), nameof(NetInfo.m_minCornerOffset));
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Ldfld && instruction.operand == minCornerOffsetField)
                {
                    yield return original.GetLDArg("nodeID");
                    yield return new CodeInstruction(OpCodes.Ldloc_3);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetNodePatches), nameof(GetMinCornerOffset)));
                }
            }
        }
        private static float GetMinCornerOffset(float cornerOffset, ushort nodeId, ushort segmentId) => Manager.Instance[nodeId, segmentId]?.Offset ?? cornerOffset;

        public static IEnumerable<CodeInstruction> RenderInstanceTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var getSegmentMethod = AccessTools.Method(typeof(NetNode), nameof(NetNode.GetSegment));
            var checkRenderDistanceMethod = AccessTools.Method(typeof(RenderManager.CameraInfo), nameof(RenderManager.CameraInfo.CheckRenderDistance));
            var nodeMeshField = AccessTools.Field(typeof(NetInfo.Node), nameof(NetInfo.Node.m_nodeMesh));
            var nodeMaterialField = AccessTools.Field(typeof(NetInfo.Node), nameof(NetInfo.Node.m_nodeMaterial));

            var prev = default(CodeInstruction);
            var segmentLocal = default(LocalBuilder);
            var renderCount = 0;

            foreach (var instruction in instructions)
            {
                yield return instruction;

                if (prev != null && prev.opcode == OpCodes.Call)
                {
                    if (prev.operand == getSegmentMethod)
                        segmentLocal = instruction.operand as LocalBuilder;

                    else if (prev.operand == checkRenderDistanceMethod)
                    {
                        renderCount += 1;

                        if (renderCount == 2)
                        {
                            yield return original.GetLDArg("nodeID");
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetNodePatches), nameof(ShouldContinueMedian)));
                            yield return new CodeInstruction(OpCodes.Or);
                        }
                    }
                }
                else if (renderCount == 2 && instruction.opcode == OpCodes.Ldfld)
                {
                    if (instruction.operand == nodeMeshField)
                    {
                        yield return original.GetLDArg("nodeID");
                        yield return new CodeInstruction(OpCodes.Ldloc_S, segmentLocal);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetNodePatches), nameof(NetNodePatches.CalculateMesh)));
                    }
                    else if (instruction.operand == nodeMaterialField)
                    {
                        yield return original.GetLDArg("nodeID");
                        yield return new CodeInstruction(OpCodes.Ldloc_S, segmentLocal);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetNodePatches), nameof(NetNodePatches.CalculateMaterial)));
                    }
                }

                prev = instruction;
            }
        }

        private static bool ShouldContinueMedian(ushort nodeID) => Manager.Instance[nodeID] is NodeData data && data.Type == NodeStyleType.Stretch;
        private static Material CalculateMaterial(Material material, ushort nodeId, ushort segmentId) => ShouldContinueMedian(nodeId) ? MaterialUtilities.ContinuesMedian(material, segmentId.GetSegment().Info, false) : material;
        private static Mesh CalculateMesh(Mesh mesh, ushort nodeId, ushort segmentId) => ShouldContinueMedian(nodeId) ? MaterialUtilities.ContinuesMedian(mesh, segmentId.GetSegment().Info) : mesh;
    }
}
