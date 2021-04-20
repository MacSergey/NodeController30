using ColossalFramework;
using HarmonyLib;
using KianCommons;
using KianCommons.Patches;
using ModsCommon.Utilities;
using NodeController.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using TranspilerUtilities = KianCommons.Patches.TranspilerUtilities;

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
                    yield return new CodeInstruction(original.GetLDArg("nodeID"));
                    yield return new CodeInstruction(OpCodes.Ldloc_3);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetNodePatches), nameof(GetMinCornerOffset)));
                }
            }
        }
        private static float GetMinCornerOffset(float cornerOffset, ushort nodeId, ushort segmentId) => Manager.Instance[nodeId, segmentId]?.Offset ?? cornerOffset;

        public static IEnumerable<CodeInstruction> RenderInstanceTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var codes = instructions.ToList();

            var index = 0;
            var drawMeshMethod = AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawMesh), new[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(int), typeof(Camera), typeof(int), typeof(MaterialPropertyBlock) });
            index = TranspilerUtilities.SearchInstruction(codes, new CodeInstruction(OpCodes.Call, drawMeshMethod), index, counter: 2);

            var nodeMaterialField = AccessTools.Field(typeof(NetInfo.Node), nameof(NetInfo.Node.m_nodeMaterial));
            index = TranspilerUtilities.SearchInstruction(codes, new CodeInstruction(OpCodes.Ldfld, nodeMaterialField), index, dir: -1);
            int insertIndex3 = index + 1;

            var nodeMeshField = AccessTools.Field(typeof(NetInfo.Node), nameof(NetInfo.Node.m_nodeMesh));
            index = TranspilerUtilities.SearchInstruction(codes, new CodeInstruction(OpCodes.Ldfld, nodeMeshField), index, dir: -1);
            int insertIndex2 = index + 1;

            var checkRenderDistanceMethod = AccessTools.Method(typeof(RenderManager.CameraInfo), nameof(RenderManager.CameraInfo.CheckRenderDistance));
            index = TranspilerUtilities.SearchInstruction(codes, new CodeInstruction(OpCodes.Callvirt, checkRenderDistanceMethod), index, dir: -1);
            int insertIndex1 = index + 1; // at this point boloean is in stack

            var ldLocNodeID = original.GetLDArg("nodeID");
            var ldLocSegmentID = BuildSegnentLDLocFromPrevSTLoc(codes, index, counter: 1);

            var calculateMaterialInstructions = new[]
            {
                ldLocNodeID,
                ldLocSegmentID,
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetNodePatches), nameof(CalculateMaterial))),
            };
            TranspilerUtilities.InsertInstructions(codes, calculateMaterialInstructions, insertIndex3);

            var calculateMeshInstructions = new[]
            {
                ldLocNodeID,
                ldLocSegmentID,
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetNodePatches), nameof(CalculateMesh))),
            };
            TranspilerUtilities.InsertInstructions(codes, calculateMeshInstructions, insertIndex2);

            var shouldContinueMedianInstructions = new[]
            {
                ldLocNodeID,
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetNodePatches), nameof(ShouldContinueMedian))),
                new CodeInstruction(OpCodes.Or)
            };

            TranspilerUtilities.InsertInstructions(codes, shouldContinueMedianInstructions, insertIndex1);

            return codes;
        }
        private static CodeInstruction BuildSegnentLDLocFromPrevSTLoc(List<CodeInstruction> codes, int index, int counter = 1)
        {
            var getSegmentMethod = AccessTools.Method(typeof(NetNode), nameof(NetNode.GetSegment));
            index = TranspilerUtilities.SearchInstruction(codes, new CodeInstruction(OpCodes.Call, getSegmentMethod), index, counter: counter, dir: -1);
            return codes[index + 1].BuildLdLocFromStLoc();
        }
        private static bool ShouldContinueMedian(ushort nodeID) => Manager.Instance[nodeID] is NodeData data && data.Type == NodeStyleType.Stretch;
        private static Material CalculateMaterial(Material material, ushort nodeId, ushort segmentId) => ShouldContinueMedian(nodeId) ? MaterialUtilities.ContinuesMedian(material, segmentId.GetSegment().Info, false) : material;
        private static Mesh CalculateMesh(Mesh mesh, ushort nodeId, ushort segmentId) => ShouldContinueMedian(nodeId) ? MaterialUtilities.ContinuesMedian(mesh, segmentId.GetSegment().Info) : mesh;
    }
}
