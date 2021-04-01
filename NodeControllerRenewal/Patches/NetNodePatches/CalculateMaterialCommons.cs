using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NodeController.Patches
{
    using System;
    using UnityEngine;
    using KianCommons;
    using static KianCommons.Patches.TranspilerUtils;
    using Util;

    public static class CalculateMaterialCommons
    {
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
                //todo use datamatrix for direct connect.
            }
            return material;
        }

        public static Mesh CalculateMesh(Mesh mesh, ushort nodeID, ushort segmentID)
        {
            if (ShouldContinueMedian(nodeID, segmentID))
            {
                NetInfo netInfo = segmentID.ToSegment().Info;
                mesh = MaterialUtils.ContinuesMedian(mesh, netInfo, false);
            }
            return mesh;
        }

        static Type[] Args { get; } = new[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(int), typeof(Camera), typeof(int), typeof(MaterialPropertyBlock) };

        static MethodInfo DrawMesh => typeof(Graphics).GetMethod("DrawMesh", Args);
        static FieldInfo NodeMaterial => typeof(NetInfo.Node).GetField("m_nodeMaterial");
        static FieldInfo NodeMesh => typeof(NetInfo.Node).GetField("m_nodeMesh");
        static MethodInfo mCalculateMaterial => typeof(CalculateMaterialCommons).GetMethod("CalculateMaterial");
        static MethodInfo mCalculateMesh => typeof(CalculateMaterialCommons).GetMethod("CalculateMesh");
        static MethodInfo CheckRenderDistance => typeof(RenderManager.CameraInfo).GetMethod("CheckRenderDistance");
        static MethodInfo mShouldContinueMedian => typeof(CalculateMaterialCommons).GetMethod("ShouldContinueMedian");
        static MethodInfo GetSegment => typeof(NetNode).GetMethod("GetSegment");

        public static void PatchCheckFlags(List<CodeInstruction> codes, int occurance, MethodInfo method)
        {
            Assertion.Assert(DrawMesh != null, "mDrawMesh!=null failed");
            Assertion.Assert(NodeMaterial != null, "fNodeMaterial!=null failed");
            Assertion.Assert(NodeMesh != null, "fNodeMesh!=null failed");
            Assertion.Assert(mCalculateMaterial != null, "mCalculateMaterial!=null failed");
            Assertion.Assert(mCalculateMesh != null, "mCalculateMesh!=null failed");
            Assertion.Assert(CheckRenderDistance != null, "mCheckRenderDistance!=null failed");
            Assertion.Assert(mShouldContinueMedian != null, "mShouldContinueMedian!=null failed");

            int index = 0;
            // returns the position of First DrawMesh after index.
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Call, DrawMesh), index, counter: occurance);
            Assertion.Assert(index != 0, "index!=0");


            // find ldfld node.m_nodeMaterial
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Ldfld, NodeMaterial), index, dir: -1);
            int insertIndex3 = index + 1;

            // fine node.m_NodeMesh
            /*  IL_07ac: ldloc.s      node_V_16
             *  IL_07ae: ldfld        class [UnityEngine]UnityEngine.Mesh NetInfo/Node::m_nodeMesh
             */
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Ldfld, NodeMesh), index, dir: -1);
            int insertIndex2 = index + 1;

            // find: if (cameraInfo.CheckRenderDistance(data.m_position, node.m_lodRenderDistance))
            /* IL_0627: callvirt instance bool RenderManager CameraInfo::CheckRenderDistance(Vector3, float32)
             * IL_062c brfalse      IL_07e2 */
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Callvirt, CheckRenderDistance), index, dir: -1);
            int insertIndex1 = index + 1; // at this point boloean is in stack

            CodeInstruction LDArg_NodeID = GetLDArg(method, "nodeID"); // push nodeID into stack
            CodeInstruction LDLoc_segmentID = BuildSegnentLDLocFromPrevSTLoc(codes, index, counter: 1); // push segmentID into stack

            { // Insert material = CalculateMaterial(material, nodeID, segmentID)
                var newInstructions = new[] {
                    LDArg_NodeID,
                    LDLoc_segmentID,
                    new CodeInstruction(OpCodes.Call, mCalculateMaterial), // call Material CalculateMaterial(material, nodeID, segmentID).
                };
                InsertInstructions(codes, newInstructions, insertIndex3);
            }

            { // Insert material = CalculateMesh(mesh, nodeID, segmentID)
                var newInstructions = new[] {
                    LDArg_NodeID,
                    LDLoc_segmentID,
                    new CodeInstruction(OpCodes.Call, mCalculateMesh), // call Mesh CalculateMesh(mesh, nodeID, segmentID).
                };
                InsertInstructions(codes, newInstructions, insertIndex2);
            }

            { // Insert ShouldHideCrossing(nodeID, segmentID)
                var newInstructions = new[]{
                    LDArg_NodeID,
                    LDLoc_segmentID,
                    new CodeInstruction(OpCodes.Call, mShouldContinueMedian), // call Material mShouldHideCrossing(nodeID, segmentID).
                    new CodeInstruction(OpCodes.Or) };

                InsertInstructions(codes, newInstructions, insertIndex1);
            }


        }

        public static CodeInstruction BuildSegnentLDLocFromPrevSTLoc(List<CodeInstruction> codes, int index, int counter = 1)
        {
            Assertion.Assert(GetSegment != null, "mGetSegment!=null");
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Call, GetSegment), index, counter: counter, dir: -1);

            var code = codes[index + 1];
            Assertion.Assert(code.IsStloc(), $"IsStLoc(code) | code={code}");
            return code.BuildLdLocFromStLoc();
        }
    }
}
