using HarmonyLib;
using KianCommons.Plugins;
using ModsCommon;
using NodeController.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TrafficManager.Manager.Impl;
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
            success &= Patch_LoadAssetPanel_OnLoad();

            PatchNetManager(ref success);
            PatchNetNode(ref success);
            PatchNetSegment(ref success);
            PatchNetLane(ref success);
            PatchNetTool(ref success);
            PatchRoadBaseAI(ref success);
            PatchHideCrosswalk(ref success);
            PatchTMPE(ref success);
            PatchSimulationStep(ref success);

            return success;
        }

        private bool Patch_ToolController_Awake()
        {
            var prefix = AccessTools.Method(typeof(NodeControllerTool), nameof(NodeControllerTool.Create));
            return AddPrefix(prefix, typeof(ToolController), "Awake");
        }
        private bool Patch_LoadAssetPanel_OnLoad()
        {
            var postfix = AccessTools.Method(typeof(OnLoadPatch), nameof(OnLoadPatch.LoadAssetPanelOnLoadPostfix));
            return AddPostfix(postfix, typeof(LoadAssetPanel), nameof(LoadAssetPanel.OnLoad));
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
            return AddPrefix(prefix, typeof(NetManager), "ReleaseNodeImplementation", new Type[] { typeof(ushort) });
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
            success &= Patch_NetNode_RefreshJunctionData_Postfix();
            success &= Patch_NetNode_RefreshJunctionData_Transpiler();
            success &= Patch_NetNode_RenderInstance();
        }
        private bool Patch_NetNode_CalculateNode()
        {
            var postfix = AccessTools.Method(typeof(NetNodePatches), nameof(NetNodePatches.CalculateNodePostfix));
            return AddPostfix(postfix, typeof(NetNode), nameof(NetNode.CalculateNode));
        }
        private bool Patch_NetNode_RefreshJunctionData_Postfix()
        {
            var postfix = AccessTools.Method(typeof(NetNodePatches), nameof(NetNodePatches.RefreshJunctionDataPostfix));
            var parameters = new Type[] { typeof(ushort), typeof(int), typeof(ushort), typeof(Vector3), typeof(uint).MakeByRefType(), typeof(RenderManager.Instance).MakeByRefType() };
            return AddPostfix(postfix, typeof(NetNode), "RefreshJunctionData", parameters);
        }
        private bool Patch_NetNode_RefreshJunctionData_Transpiler()
        {
            var transpiler = AccessTools.Method(typeof(NetNodePatches), nameof(NetNodePatches.RefreshJunctionDataTranspiler));
            var parameters = new Type[] { typeof(ushort), typeof(NetInfo), typeof(uint) };
            return AddTranspiler(transpiler, typeof(NetNode), "RefreshJunctionData", parameters);
        }
        private bool Patch_NetNode_RenderInstance()
        {
            var transpiler = AccessTools.Method(typeof(NetNodePatches), nameof(NetNodePatches.RenderInstanceTranspiler));
            var parameters = new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(NetInfo), typeof(int), typeof(NetNode.Flags), typeof(uint).MakeByRefType(), typeof(RenderManager.Instance).MakeByRefType() };
            return AddTranspiler(transpiler, typeof(NetNode), nameof(NetNode.RenderInstance), parameters);
        }

        #endregion

        #region NETSEGMENT
        private void PatchNetSegment(ref bool success)
        {
            success &= Patch_NetSegment_CalculateCorner_Postfix();
            success &= Patch_NetSegment_CalculateCorner_Transpiler();
            success &= Patch_NetSegment_FindDirection();
            success &= Patch_NetSegment_CalculateSegment();
            success &= Patch_NetSegment_RenderInstance();
        }

        private bool Patch_NetSegment_CalculateCorner_Postfix()
        {
            var postfix = AccessTools.Method(typeof(NetSegmentPatches), nameof(NetSegmentPatches.CalculateCornerPostfix));
            var parameters = new Type[] { typeof(ushort), typeof(bool), typeof(bool), typeof(bool), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(bool).MakeByRefType() };
            return AddPostfix(postfix, typeof(NetSegment), nameof(NetSegment.CalculateCorner), parameters);
        }
        private bool Patch_NetSegment_CalculateCorner_Transpiler()
        {
            var transpilar = AccessTools.Method(typeof(NetSegmentPatches), nameof(NetSegmentPatches.CalculateCornerTranspiler));
            var parameters = new Type[] { typeof(NetInfo), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(NetInfo), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(NetInfo), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(ushort), typeof(ushort), typeof(bool), typeof(bool), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(bool).MakeByRefType() };
            return AddTranspiler(transpilar, typeof(NetSegment), nameof(NetSegment.CalculateCorner), parameters);
        }

        private bool Patch_NetSegment_FindDirection()
        {
            var transpilar = AccessTools.Method(typeof(NetSegmentPatches), nameof(NetSegmentPatches.FindDirectionTranspiler));
            return AddTranspiler(transpilar, typeof(NetSegment), nameof(NetSegment.FindDirection));
        }
        private bool Patch_NetSegment_CalculateSegment()
        {
            var postfix = AccessTools.Method(typeof(NetSegmentPatches), nameof(NetSegmentPatches.CalculateSegmentPostfix));
            return AddPostfix(postfix, typeof(NetSegment), nameof(NetSegment.CalculateSegment));
        }
        private bool Patch_NetSegment_RenderInstance()
        {
            var transpilar = AccessTools.Method(typeof(NetSegmentPatches), nameof(NetSegmentPatches.RenderInstanceTranspiler));
            var parameters = new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int), typeof(NetInfo), typeof(RenderManager.Instance).MakeByRefType() };
            return AddTranspiler(transpilar, typeof(NetSegment), nameof(NetSegment.RenderInstance), parameters);
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
            var transpilar = AccessTools.Method(typeof(NetLanePatches), nameof(NetLanePatches.Transpiler));
            return AddTranspiler(transpilar, typeof(NetLane), methodName);
        }

        #endregion

        #region NETTOOL

        private void PatchNetTool(ref bool success)
        {
            success &= Patch_NetTool_MoveMiddleNode();
            success &= Patch_NetTool_SplitSegment();
        }
        private bool Patch_NetTool_MoveMiddleNode()
        {
            var prefix = AccessTools.Method(typeof(NetToolPatch), nameof(NetToolPatch.MoveMiddleNodePrefix));
            var postfix = AccessTools.Method(typeof(NetToolPatch), nameof(NetToolPatch.MoveMiddleNodePostfix));
            return AddPrefix(prefix, typeof(NetTool), "MoveMiddleNode") && AddPostfix(postfix, typeof(NetTool), "MoveMiddleNode");
        }
        private bool Patch_NetTool_SplitSegment()
        {
            var prefix = AccessTools.Method(typeof(NetToolPatch), nameof(NetToolPatch.SplitSegmentPrefix));
            var postfix = AccessTools.Method(typeof(NetToolPatch), nameof(NetToolPatch.SplitSegmentPostfix));
            return AddPrefix(prefix, typeof(NetTool), "SplitSegment") && AddPostfix(postfix, typeof(NetTool), "SplitSegment");
        }

        #endregion

        #region ROADBUSAI

        private void PatchRoadBaseAI(ref bool success)
        {
            success &= Patch_RoadBaseAI_UpdateLanes();
            success &= Patch_RoadBaseAI_UpdateNodeFlags();
        }
        private bool Patch_RoadBaseAI_UpdateLanes()
        {
            var postfix = AccessTools.Method(typeof(RoadBaseAIPatches), nameof(RoadBaseAIPatches.UpdateLanesPostfix));
            return AddPostfix(postfix, typeof(RoadBaseAI), nameof(RoadBaseAI.UpdateLanes));
        }
        private bool Patch_RoadBaseAI_UpdateNodeFlags()
        {
            var postfix = AccessTools.Method(typeof(RoadBaseAIPatches), nameof(RoadBaseAIPatches.UpdateNodeFlagsPostfix));
            return AddPostfix(postfix, typeof(RoadBaseAI), nameof(RoadBaseAI.UpdateNodeFlags));
        }

        #endregion

        #region HIDECROSSWALK

        private void PatchHideCrosswalk(ref bool success)
        {
            if (PluginUtil.GetHideCrossings().IsActive())
            {
                var prefix = AccessTools.Method(typeof(HideCrosswalksPatches), nameof(HideCrosswalksPatches.ShouldHideCrossingPrefix));
                success &= AddPrefix(prefix, typeof(HideCrosswalks.Patches.CalculateMaterialCommons), nameof(HideCrosswalks.Patches.CalculateMaterialCommons.ShouldHideCrossing));
            }
        }

        #endregion

        #region TMPE

        private void PatchTMPE(ref bool success)
        {
            if (PluginUtil.GetTrafficManager().IsActive())
            {
                success &= Patch_TrafficLightManager_CanToggleTrafficLight();
                success &= Patch_JunctionRestrictionsManager_GetDefaultEnteringBlockedJunctionAllowed();
                success &= Patch_JunctionRestrictionsManager_GetDefaultPedestrianCrossingAllowed();
                success &= Patch_JunctionRestrictionsManager_GetDefaultUturnAllowed();
                success &= Patch_JunctionRestrictionsManager_IsEnteringBlockedJunctionAllowedConfigurable();
                success &= Patch_JunctionRestrictionsManager_IsPedestrianCrossingAllowedConfigurable();
                success &= Patch_JunctionRestrictionsManager_IsUturnAllowedConfigurable();
            }
        }
        private bool Patch_TrafficLightManager_CanToggleTrafficLight()
        {
            var prefix = AccessTools.Method(typeof(TMPEPatches), nameof(TMPEPatches.CanToggleTrafficLightPrefix));
            return AddPrefix(prefix, typeof(TrafficLightManager), nameof(TrafficLightManager.CanToggleTrafficLight));
        }
        private bool Patch_JunctionRestrictionsManager_GetDefaultEnteringBlockedJunctionAllowed()
        {
            var prefix = AccessTools.Method(typeof(TMPEPatches), nameof(TMPEPatches.GetDefaultEnteringBlockedJunctionAllowedPrefix));
            return AddPrefix(prefix, typeof(JunctionRestrictionsManager), nameof(JunctionRestrictionsManager.GetDefaultEnteringBlockedJunctionAllowed));
        }
        private bool Patch_JunctionRestrictionsManager_GetDefaultPedestrianCrossingAllowed()
        {
            var prefix = AccessTools.Method(typeof(TMPEPatches), nameof(TMPEPatches.GetDefaultPedestrianCrossingAllowedPrefix));
            return AddPrefix(prefix, typeof(JunctionRestrictionsManager), nameof(JunctionRestrictionsManager.GetDefaultPedestrianCrossingAllowed));
        }
        private bool Patch_JunctionRestrictionsManager_GetDefaultUturnAllowed()
        {
            var prefix = AccessTools.Method(typeof(TMPEPatches), nameof(TMPEPatches.GetDefaultUturnAllowedPrefix));
            return AddPrefix(prefix, typeof(JunctionRestrictionsManager), nameof(JunctionRestrictionsManager.GetDefaultUturnAllowed));
        }
        private bool Patch_JunctionRestrictionsManager_IsEnteringBlockedJunctionAllowedConfigurable()
        {
            var prefix = AccessTools.Method(typeof(TMPEPatches), nameof(TMPEPatches.IsEnteringBlockedJunctionAllowedConfigurablePrefix));
            return AddPrefix(prefix, typeof(JunctionRestrictionsManager), nameof(JunctionRestrictionsManager.IsEnteringBlockedJunctionAllowedConfigurable));
        }
        private bool Patch_JunctionRestrictionsManager_IsPedestrianCrossingAllowedConfigurable()
        {
            var prefix = AccessTools.Method(typeof(TMPEPatches), nameof(TMPEPatches.IsPedestrianCrossingAllowedConfigurablePrefix));
            return AddPrefix(prefix, typeof(JunctionRestrictionsManager), nameof(JunctionRestrictionsManager.IsPedestrianCrossingAllowedConfigurable));
        }
        private bool Patch_JunctionRestrictionsManager_IsUturnAllowedConfigurable()
        {
            var prefix = AccessTools.Method(typeof(TMPEPatches), nameof(TMPEPatches.IsUturnAllowedConfigurablePrefix));
            return AddPrefix(prefix, typeof(JunctionRestrictionsManager), nameof(JunctionRestrictionsManager.IsUturnAllowedConfigurable));
        }

        #endregion

        #region SIMULATIONSTEP

        private void PatchSimulationStep(ref bool success)
        {
            var parameters = new Type[] { typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(Vehicle.Frame).MakeByRefType(), typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(int) };

            success &= Patch_CarAI_SimulationStep(parameters);
            success &= Patch_CarTrailerAI_SimulationStep(parameters);
            success &= Patch_TrainAI_SimulationStep(parameters);
            success &= Patch_TramBaseAI_SimulationStep(parameters);
        }
        private bool Patch_CarAI_SimulationStep(Type[] parameters)
        {
            var prefix = AccessTools.Method(typeof(SimulationStepPatches), nameof(SimulationStepPatches.CarAISimulationStepPostfix));
            var transpiler = AccessTools.Method(typeof(SimulationStepPatches), nameof(SimulationStepPatches.CarAISimulationStepTranspiler));
            return AddPrefix(prefix, typeof(CarAI), nameof(CarAI.SimulationStep), parameters) && AddTranspiler(transpiler, typeof(CarAI), nameof(CarAI.SimulationStep), parameters);
        }
        private bool Patch_CarTrailerAI_SimulationStep(Type[] parameters)
        {
            var prefix = AccessTools.Method(typeof(SimulationStepPatches), nameof(SimulationStepPatches.CarTrailerAISimulationStepPostfix));
            var transpiler = AccessTools.Method(typeof(SimulationStepPatches), nameof(SimulationStepPatches.CarTrailerAISimulationStepTranspiler));
            return AddPrefix(prefix, typeof(CarTrailerAI), nameof(CarTrailerAI.SimulationStep), parameters) && AddTranspiler(transpiler, typeof(CarTrailerAI), nameof(CarTrailerAI.SimulationStep), parameters);
        }
        private bool Patch_TrainAI_SimulationStep(Type[] parameters)
        {
            var prefix = AccessTools.Method(typeof(SimulationStepPatches), nameof(SimulationStepPatches.TrainAISimulationStepPostfix));
            AddPrefix(prefix, typeof(TrafficManager.Custom.AI.CustomTrainAI), nameof(TrafficManager.Custom.AI.CustomTrainAI.SimulationStep), parameters);
            return AddPrefix(prefix, typeof(TrainAI), nameof(TrainAI.SimulationStep), parameters);
        }
        private bool Patch_TramBaseAI_SimulationStep(Type[] parameters)
        {
            var prefix = AccessTools.Method(typeof(SimulationStepPatches), nameof(SimulationStepPatches.TramBaseAISimulationStepPostfix));
            AddPrefix(prefix, typeof(TrafficManager.Custom.AI.CustomTramBaseAI), nameof(TrafficManager.Custom.AI.CustomTramBaseAI.SimulationStep), parameters);
            return AddPrefix(prefix, typeof(TramBaseAI), nameof(TramBaseAI.SimulationStep), parameters);
        }

        #endregion
    }
}
