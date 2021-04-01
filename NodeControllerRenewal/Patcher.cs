using HarmonyLib;
using ModsCommon;
using NodeController.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeController
{
    public class Patcher : BasePatcher
    {
        public Patcher(BaseMod mod) : base(mod) { }

        protected override bool PatchProcess()
        {
            var success = true;

            success &= Patch_ToolController_Awake();

            PatchNetManager(ref success);
            PatchNetNode(ref success);
            PatchNetSegment(ref success);
            PatchNetLane(ref success);

            return success;
        }

        private bool Patch_ToolController_Awake()
        {
            var prefix = AccessTools.Method(typeof(NodeControllerTool), nameof(NodeControllerTool.Create));
            return AddPrefix(prefix, typeof(ToolController), "Awake");
        }

        #region NETMANAGER

        private void PatchNetManager(ref bool success)
        {
            success &= Patch_NetManager_CreateSegment();
            success &= Patch_NetManager_ReleaseNodeImplementation();
            success &= Patch_NetManager_ReleaseSegmentImplementation();
        }

        private bool Patch_NetManager_CreateSegment()
        {
            var postfix = AccessTools.Method(typeof(NetManagerPatches), nameof(NetManagerPatches.CreateSegmentPostfix));
            return AddPostfix(postfix, typeof(NetManager), nameof(NetManager.CreateSegment));
        }
        private bool Patch_NetManager_ReleaseNodeImplementation()
        {
            var prefix = AccessTools.Method(typeof(NetManagerPatches), nameof(NetManagerPatches.ReleaseNodeImplementationPrefix));
            return AddPrefix(prefix, typeof(NetManager), "ReleaseNodeImplementation");
        }
        private bool Patch_NetManager_ReleaseSegmentImplementation()
        {
            var prefix = AccessTools.Method(typeof(NetManagerPatches), nameof(NetManagerPatches.ReleaseSegmentImplementationPrefix));
            var parameters = new[] { typeof(ushort), typeof(NetSegment).MakeByRefType(), typeof(bool) };
            return AddPrefix(prefix, typeof(NetManager), "ReleaseSegmentImplementation", parameters);
        }

        #endregion

        #region NETNODE

        private void PatchNetNode(ref bool success)
        {
            success &= Patch_NetNode_CalculateNode();
            success &= Patch_NetNode_RefreshJunctionData1();
            success &= Patch_NetNode_RefreshJunctionData2();
            success &= Patch_NetNode_RenderInstance();
        }
        private bool Patch_NetNode_CalculateNode()
        {
            var postfix = AccessTools.Method(typeof(NetNodePatches), nameof(NetNodePatches.CalculateNodePostfix));
            return AddPostfix(postfix, typeof(NetNode), nameof(NetNode.CalculateNode));
        }
        private bool Patch_NetNode_RefreshJunctionData1()
        {
            var postfix = AccessTools.Method(typeof(NetNodePatches), nameof(NetNodePatches.RefreshJunctionDataPostfix));
            var parameters = new Type[] { typeof(ushort), typeof(int), typeof(ushort), typeof(Vector3), typeof(uint).MakeByRefType(), typeof(RenderManager.Instance).MakeByRefType() };
            return AddPostfix(postfix, typeof(NetNode), "RefreshJunctionData", parameters);
        }
        private bool Patch_NetNode_RefreshJunctionData2()
        {
            var transpiler = AccessTools.Method(typeof(NetNodePatches), nameof(NetNodePatches.RefreshJunctionDataPostfix));
            var parameters = new Type[] { typeof(ushort), typeof(NetInfo), typeof(uint) };
            return AddTranspiler(transpiler, typeof(NetNode), "RefreshJunctionData", parameters);
        }
        private bool Patch_NetNode_RenderInstance()
        {
            var postfix = AccessTools.Method(typeof(NetNodePatches), nameof(NetNodePatches.RenderInstanceTranspiler));
            var parameters = new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(NetInfo), typeof(int), typeof(NetNode.Flags), typeof(uint).MakeByRefType(), typeof(RenderManager.Instance).MakeByRefType()};
            return AddPostfix(postfix, typeof(NetNode), nameof(NetNode.CalculateNode), parameters);
        }

        #endregion

        #region NETSEGMENT
        private void PatchNetSegment(ref bool success)
        {
            success &= Patch_NetSegment_CalculateCorner();
            success &= Patch_NetSegment_FindDirection();
        }

        private bool Patch_NetSegment_CalculateCorner()
        {
            var postfix = AccessTools.Method(typeof(CalculateCornerPatch2), nameof(CalculateCornerPatch2.Postfix));
            return AddPostfix(postfix, typeof(NetSegment), nameof(NetSegment.CalculateCorner));
        }

        private bool Patch_NetSegment_FindDirection()
        {
            var transpilar = AccessTools.Method(typeof(FlatJunctionsCommons), nameof(FlatJunctionsCommons.Transpiler));
            return AddTranspiler(transpilar, typeof(NetSegment), nameof(NetSegment.FindDirection));
        }

        #endregion

        #region NETLANES

        private void PatchNetLane(ref bool success)
        {
            success &= Patch_NetLane_PopulateGroupData();
            success &= Patch_NetLane_RefreshInstance();
            success &= Patch_NetLane_RenderDestroyedInstance();
            success &= Patch_NetLane_RenderInstance();
        }

        private bool Patch_NetLane_PopulateGroupData() => Patch_NetLane(nameof(NetLane.PopulateGroupData));
        private bool Patch_NetLane_RefreshInstance() => Patch_NetLane(nameof(NetLane.RefreshInstance));
        private bool Patch_NetLane_RenderDestroyedInstance() => Patch_NetLane(nameof(NetLane.RenderDestroyedInstance));
        private bool Patch_NetLane_RenderInstance() => Patch_NetLane(nameof(NetLane.RenderInstance));
        private bool Patch_NetLane(string methodName)
        {
            var transpilar = AccessTools.Method(typeof(NetLanePatches), nameof(NetLanePatches.Patch));
            return AddTranspiler(transpilar, typeof(NetLane), methodName);
        }

        #endregion




    }
}
