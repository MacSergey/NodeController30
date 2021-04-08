using ColossalFramework;
using HarmonyLib;
using KianCommons;
using KianCommons.Patches;
using NodeController.Util;
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
            NodeManager.Instance.OnBeforeCalculateNodePatch(nodeID); // invalid/unsupported nodes are set to null.
            NodeData nodeData = NodeManager.Instance.buffer[nodeID];
            ref NetNode node = ref nodeID.ToNode();

            if (nodeData == null || nodeData.SegmentCount != 2)
                return;
            if (node.m_flags.IsFlagSet(NetNode.Flags.Outside))
                return;

            if (nodeData.NeedsTransitionFlag)
                node.m_flags |= NetNode.Flags.Transition;
            else
                node.m_flags &= ~NetNode.Flags.Transition;

            if (nodeData.IsMiddleNode)
            {
                node.m_flags &= ~(NetNode.Flags.Junction | NetNode.Flags.AsymForward | NetNode.Flags.AsymBackward);
                node.m_flags |= NetNode.Flags.Middle;
            }

            if (nodeData.IsBendNode)
            {
                node.m_flags &= ~(NetNode.Flags.Junction | NetNode.Flags.Middle);
                node.m_flags |= NetNode.Flags.Bend; // TODO set asymForward and asymBackward
            }

            if (nodeData.IsJunctionNode)
            {
                node.m_flags |= NetNode.Flags.Junction;
                node.m_flags &= ~(NetNode.Flags.Middle | NetNode.Flags.AsymForward | NetNode.Flags.AsymBackward | NetNode.Flags.Bend | NetNode.Flags.End);
            }

            node.m_flags &= ~NetNode.Flags.Moveable;
        }

        public static void RefreshJunctionDataPostfix(ref NetNode __instance, ref RenderManager.Instance data)
        {
            ushort nodeID = __instance.GetID();
            if (NodeManager.Instance.buffer[nodeID] is not NodeData blendData)
                return;

            if (blendData.ShouldRenderCenteralCrossingTexture)
            {
                // puts crossings in the center.
                data.m_dataVector1.w = 0.01f;
            }
        }

        public static IEnumerable<CodeInstruction> RefreshJunctionDataTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var codes = instructions.ToCodeList();

            var ldarg_nodeID = TranspilerUtils.GetLDArg(original, "nodeID");
            var ldarg_segmentID = BuildSegmentLDLocFromSTLoc(codes);
            var call_GetMinCornerOffset = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetNodePatches), nameof(GetMinCornerOffset)));

            var minCornerOffsetField = AccessTools.Field(typeof(NetInfo), nameof(NetInfo.m_minCornerOffset));
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Ldfld && instruction.operand == minCornerOffsetField)
                {
                    yield return ldarg_nodeID;
                    yield return ldarg_segmentID;
                    yield return call_GetMinCornerOffset;
                }
            }
        }

        public static CodeInstruction BuildSegmentLDLocFromSTLoc(List<CodeInstruction> codes, int startIndex = 0, int count = 1)
        {
            var GetSegmentMethod = AccessTools.Method(typeof(NetNode), nameof(NetNode.GetSegment));
            int index = codes.Search(c => c.Calls(GetSegmentMethod), startIndex, count);
            index = codes.Search(c => c.IsStloc(), index);
            return codes[index].BuildLdLocFromStLoc();
        }
        public static float GetMinCornerOffset(float cornerOffset, ushort nodeID, ushort segmentID)
        {
            var segmentData = SegmentEndManager.Instance.GetAt(segmentID, nodeID);
            return segmentData?.Offset ?? cornerOffset;
        }

        public static IEnumerable<CodeInstruction> RenderInstanceTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
        {
            var codes = TranspilerUtils.ToCodeList(instructions);
            PatchCheckFlags(codes, occurance: 2, method);

            return codes;
        }
        public static void PatchCheckFlags(List<CodeInstruction> codes, int occurance, MethodBase method)
        {
            int index = 0;
            var drawMeshMethod = AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawMesh), new[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(int), typeof(Camera), typeof(int), typeof(MaterialPropertyBlock) });
            index = TranspilerUtils.SearchInstruction(codes, new CodeInstruction(OpCodes.Call, drawMeshMethod), index, counter: occurance);

            var nodeMaterialField = AccessTools.Field(typeof(NetInfo.Node), nameof(NetInfo.Node.m_nodeMaterial));
            index = TranspilerUtils.SearchInstruction(codes, new CodeInstruction(OpCodes.Ldfld, nodeMaterialField), index, dir: -1);
            int insertIndex3 = index + 1;

            var nodeMeshField = AccessTools.Field(typeof(NetInfo.Node), nameof(NetInfo.Node.m_nodeMesh));
            index = TranspilerUtils.SearchInstruction(codes, new CodeInstruction(OpCodes.Ldfld, nodeMeshField), index, dir: -1);
            int insertIndex2 = index + 1;

            var checkRenderDistanceMethod = AccessTools.Method(typeof(RenderManager.CameraInfo), nameof(RenderManager.CameraInfo.CheckRenderDistance));
            index = TranspilerUtils.SearchInstruction(codes, new CodeInstruction(OpCodes.Callvirt, checkRenderDistanceMethod), index, dir: -1);
            int insertIndex1 = index + 1; // at this point boloean is in stack

            var LDArg_NodeID = TranspilerUtils.GetLDArg(method, "nodeID");
            var LDLoc_segmentID = BuildSegnentLDLocFromPrevSTLoc(codes, index, counter: 1);

            {
                var newInstructions = new[]
                {
                    LDArg_NodeID,
                    LDLoc_segmentID,
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetNodePatches), nameof(CalculateMaterial))),
                };
                TranspilerUtils.InsertInstructions(codes, newInstructions, insertIndex3);
            }

            {
                var newInstructions = new[]
                {
                    LDArg_NodeID,
                    LDLoc_segmentID,
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetNodePatches), nameof(CalculateMesh))),
                };
                TranspilerUtils.InsertInstructions(codes, newInstructions, insertIndex2);
            }

            {
                var newInstructions = new[]
                {
                    LDArg_NodeID,
                    LDLoc_segmentID,
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetNodePatches), nameof(ShouldContinueMedian))),
                    new CodeInstruction(OpCodes.Or)
                };

                TranspilerUtils.InsertInstructions(codes, newInstructions, insertIndex1);
            }
        }

        public static CodeInstruction BuildSegnentLDLocFromPrevSTLoc(List<CodeInstruction> codes, int index, int counter = 1)
        {
            var getSegmentMethod = AccessTools.Method(typeof(NetNode), nameof(NetNode.GetSegment));
            index = TranspilerUtils.SearchInstruction(codes, new CodeInstruction(OpCodes.Call, getSegmentMethod), index, counter: counter, dir: -1);
            return codes[index + 1].BuildLdLocFromStLoc();
        }
        public static bool ShouldContinueMedian(ushort nodeID, ushort segmentID)
        {
            var data = NodeManager.Instance.buffer[nodeID];
            return data != null && data.NodeType == NodeTypeT.Stretch;
        }
        public static Material CalculateMaterial(Material material, ushort nodeID, ushort segmentID)
        {
            if (ShouldContinueMedian(nodeID, segmentID))
            {
                NetInfo netInfo = segmentID.ToSegment().Info;
                material = MaterialUtils.ContinuesMedian(material, netInfo, false);
            }
            return material;
        }
        public static Mesh CalculateMesh(Mesh mesh, ushort nodeID, ushort segmentID)
        {
            if (ShouldContinueMedian(nodeID, segmentID))
            {
                NetInfo netInfo = segmentID.ToSegment().Info;
                mesh = MaterialUtils.ContinuesMedian(mesh, netInfo);
            }
            return mesh;
        }
    }
}
