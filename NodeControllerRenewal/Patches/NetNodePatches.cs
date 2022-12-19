using HarmonyLib;
using ModsCommon;
using ModsCommon.Utilities;
using NodeController.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace NodeController.Patches
{
    public static class NetNodePatches
    {
        public static void CalculateNodePostfix(ushort nodeID)
        {
            if (SingletonManager<Manager>.Instance.TryGetNodeData(nodeID, out var data))
            {
                data.SetFlags();
                data.UpdateSegmentEnds();
            }
        }
        public static IEnumerable<CodeInstruction> ReplaceNodePositionTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var positionField = AccessTools.Field(typeof(NetNode), nameof(NetNode.m_position));
            var positionLocal = generator.DeclareLocal(typeof(Vector3));

            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, positionField);
            yield return new CodeInstruction(original.GetLDArg("nodeID"));
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetNodePatches), nameof(NetNodePatches.GetCentrePosition)));
            yield return new CodeInstruction(OpCodes.Stloc_S, positionLocal);

            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldfld && instruction.operand == positionField)
                {
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, positionLocal);
                }
                else
                    yield return instruction;
            }
        }

        private static Vector3 GetCentrePosition(Vector3 defaultPosition, ushort nodeId)
        {
            if (SingletonManager<Manager>.Instance.TryGetNodeData(nodeId, out var data))
                return data.GetPosition();
            else
                return defaultPosition;
        }

        public static void RefreshJunctionDataPrefix(ushort nodeID, int segmentIndex, ref Vector3 centerPos)
        {
            if (SingletonManager<Manager>.Instance.TryGetNodeData(nodeID, out var data))
            {
                centerPos = data.GetPosition(segmentIndex);
            }
        }

        public static void RefreshJunctionDataPostfix(ushort nodeID, ref RenderManager.Instance data)
        {
            if (SingletonManager<Manager>.Instance.TryGetNodeData(nodeID, out var blendData) && blendData.ShouldRenderCenteralCrossingTexture)
            {
                data.m_dataVector1.w = 0.01f;
            }
        }

        public static IEnumerable<CodeInstruction> RenderInstanceTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var getSegmentMethod = AccessTools.Method(typeof(NetNode), nameof(NetNode.GetSegment));
            var checkRenderDistanceMethod = AccessTools.Method(typeof(RenderManager.CameraInfo), nameof(RenderManager.CameraInfo.CheckRenderDistance));
            var nodeMeshField = AccessTools.Field(typeof(NetInfo.Node), nameof(NetInfo.Node.m_nodeMesh));
            var nodeMaterialField = AccessTools.Field(typeof(NetInfo.Node), nameof(NetInfo.Node.m_nodeMaterial));
            var initializedField = AccessTools.Field(typeof(RenderManager.Instance), nameof(RenderManager.Instance.m_initialized));

            var prev = default(CodeInstruction);
            var segmentLocal = default(LocalBuilder);
            var renderCount = 0;
            var needCount = 2;

            foreach (var instruction in instructions)
            {
                yield return instruction;

                if (prev != null && prev.opcode == OpCodes.Call && prev.operand == getSegmentMethod)
                    segmentLocal = instruction.operand as LocalBuilder;
                else if (instruction != null && instruction.opcode == OpCodes.Callvirt && instruction.operand == checkRenderDistanceMethod)
                {
                    renderCount += 1;

                    if (renderCount == needCount)
                    {
                        yield return original.GetLDArg("nodeID");
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetNodePatches), nameof(NetNodePatches.ShouldContinueMedian)));
                        yield return new CodeInstruction(OpCodes.Or);
                    }
                }
                else if (renderCount == needCount && instruction.opcode == OpCodes.Ldfld)
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
                else if (prev != null && prev.opcode == OpCodes.Ldfld && prev.operand == initializedField && instruction.opcode == OpCodes.Brfalse)
                {
                    yield return original.GetLDArg("nodeID");
                    yield return original.GetLDArg("data");
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetNodePatches), nameof(NetNodePatches.IsNodeLess)));
                    yield return instruction;
                }

                prev = instruction;
            }
        }

        public static bool IsNodeLess(ushort nodeID, ref RenderManager.Instance data)
        {
            if (SingletonManager<Manager>.Instance.TryGetNodeData(nodeID, out var nodeData))
            {
                {
                    var segmentId = nodeID.GetNode().GetSegment(data.m_dataInt0 >> 4);
                    if (nodeData.TryGetSegment(segmentId, out var segmentData) && segmentData.ForceNodeLess == true)
                        return false;
                }
                {
                    var segmentId = nodeID.GetNode().GetSegment(data.m_dataInt0 & 7);
                    if (nodeData.TryGetSegment(segmentId, out var segmentData) && segmentData.ForceNodeLess == true)
                        return false;
                }
            }

            return true;
        }
        private static bool ShouldContinueMedian(ushort nodeID) => SingletonManager<Manager>.Instance.TryGetNodeData(nodeID, out var data) && data.Type == NodeStyleType.Stretch;
        private static Material CalculateMaterial(Material material, ushort nodeId, ushort segmentId) => ShouldContinueMedian(nodeId) ? MaterialUtilities.ContinuesMedian(material, segmentId.GetSegment().Info, false) : material;
        private static Mesh CalculateMesh(Mesh mesh, ushort nodeId, ushort segmentId) => ShouldContinueMedian(nodeId) ? MaterialUtilities.ContinuesMedian(mesh, segmentId.GetSegment().Info) : mesh;
    }
}
