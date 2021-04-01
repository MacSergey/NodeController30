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
            return AddPrefix(typeof(NodeControllerTool), nameof(NodeControllerTool.Create), typeof(ToolController), "Awake");
        }
        private bool Patch_LoadAssetPanel_OnLoad()
        {
            return AddPostfix(typeof(OnLoadPatch), nameof(OnLoadPatch.LoadAssetPanelOnLoadPostfix), typeof(LoadAssetPanel), nameof(LoadAssetPanel.OnLoad));
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
            return AddPostfix(typeof(NetManagerPatches), nameof(NetManagerPatches.CreateSegmentPostfix), typeof(NetManager), nameof(NetManager.CreateSegment));
        }
        private bool Patch_NetManager_ReleaseNodeImplementation()
        {
            return AddPrefix(typeof(NetManagerPatches), nameof(NetManagerPatches.ReleaseNodeImplementationPrefix), typeof(NetManager), "ReleaseNodeImplementation", new Type[] { typeof(ushort) });
        }
        private bool Patch_NetManager_ReleaseSegmentImplementation()
        {
            var parameters = new[] { typeof(ushort), typeof(NetSegment).MakeByRefType(), typeof(bool) };
            return AddPrefix(typeof(NetManagerPatches), nameof(NetManagerPatches.ReleaseSegmentImplementationPrefix), typeof(NetManager), "ReleaseSegmentImplementation", parameters);
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
            return AddPostfix(typeof(NetNodePatches), nameof(NetNodePatches.CalculateNodePostfix), typeof(NetNode), nameof(NetNode.CalculateNode));
        }
        private bool Patch_NetNode_RefreshJunctionData_Postfix()
        {
            var parameters = new Type[] { typeof(ushort), typeof(int), typeof(ushort), typeof(Vector3), typeof(uint).MakeByRefType(), typeof(RenderManager.Instance).MakeByRefType() };
            return AddPostfix(typeof(NetNodePatches), nameof(NetNodePatches.RefreshJunctionDataPostfix), typeof(NetNode), "RefreshJunctionData", parameters);
        }
        private bool Patch_NetNode_RefreshJunctionData_Transpiler()
        {
            var parameters = new Type[] { typeof(ushort), typeof(NetInfo), typeof(uint) };
            return AddTranspiler(typeof(NetNodePatches), nameof(NetNodePatches.RefreshJunctionDataTranspiler), typeof(NetNode), "RefreshJunctionData", parameters);
        }
        private bool Patch_NetNode_RenderInstance()
        {
            var parameters = new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(NetInfo), typeof(int), typeof(NetNode.Flags), typeof(uint).MakeByRefType(), typeof(RenderManager.Instance).MakeByRefType() };
            return AddTranspiler(typeof(NetNodePatches), nameof(NetNodePatches.RenderInstanceTranspiler), typeof(NetNode), nameof(NetNode.RenderInstance), parameters);
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
            var parameters = new Type[] { typeof(ushort), typeof(bool), typeof(bool), typeof(bool), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(bool).MakeByRefType() };
            return AddPostfix(typeof(NetSegmentPatches), nameof(NetSegmentPatches.CalculateCornerPostfix), typeof(NetSegment), nameof(NetSegment.CalculateCorner), parameters);
        }
        private bool Patch_NetSegment_CalculateCorner_Transpiler()
        {
            var parameters = new Type[] { typeof(NetInfo), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(NetInfo), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(NetInfo), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(ushort), typeof(ushort), typeof(bool), typeof(bool), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(bool).MakeByRefType() };
            return AddTranspiler(typeof(NetSegmentPatches), nameof(NetSegmentPatches.CalculateCornerTranspiler), typeof(NetSegment), nameof(NetSegment.CalculateCorner), parameters);
        }

        private bool Patch_NetSegment_FindDirection()
        {
            return AddTranspiler(typeof(NetSegmentPatches), nameof(NetSegmentPatches.FindDirectionTranspiler), typeof(NetSegment), nameof(NetSegment.FindDirection));
        }
        private bool Patch_NetSegment_CalculateSegment()
        {
            return AddPostfix(typeof(NetSegmentPatches), nameof(NetSegmentPatches.CalculateSegmentPostfix), typeof(NetSegment), nameof(NetSegment.CalculateSegment));
        }
        private bool Patch_NetSegment_RenderInstance()
        {
            var parameters = new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int), typeof(NetInfo), typeof(RenderManager.Instance).MakeByRefType() };
            return AddTranspiler(typeof(NetSegmentPatches), nameof(NetSegmentPatches.RenderInstanceTranspiler), typeof(NetSegment), nameof(NetSegment.RenderInstance), parameters);
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
            return AddTranspiler(typeof(NetLanePatches), nameof(NetLanePatches.Transpiler), typeof(NetLane), methodName);
        }

        #endregion

        #region NETTOOL

        private void PatchNetTool(ref bool success)
        {
            success &= Patch_NetTool_MoveMiddleNode_Prefix();
            success &= Patch_NetTool_MoveMiddleNode_Postfix();
            success &= Patch_NetTool_SplitSegment_Prefix();
            success &= Patch_NetTool_SplitSegment_Postfix();
        }
        private bool Patch_NetTool_MoveMiddleNode_Prefix()
        {
            return AddPrefix(typeof(NetToolPatch), nameof(NetToolPatch.MoveMiddleNodePrefix), typeof(NetTool), "MoveMiddleNode");
        }
        private bool Patch_NetTool_MoveMiddleNode_Postfix()
        {
            return AddPostfix(typeof(NetToolPatch), nameof(NetToolPatch.MoveMiddleNodePostfix), typeof(NetTool), "MoveMiddleNode");
        }
        private bool Patch_NetTool_SplitSegment_Prefix()
        {
            return AddPrefix(typeof(NetToolPatch), nameof(NetToolPatch.SplitSegmentPrefix), typeof(NetTool), "SplitSegment");
        }
        private bool Patch_NetTool_SplitSegment_Postfix()
        {
            return AddPostfix(typeof(NetToolPatch), nameof(NetToolPatch.SplitSegmentPostfix), typeof(NetTool), "SplitSegment");
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
            return AddPostfix(typeof(RoadBaseAIPatches), nameof(RoadBaseAIPatches.UpdateLanesPostfix), typeof(RoadBaseAI), nameof(RoadBaseAI.UpdateLanes));
        }
        private bool Patch_RoadBaseAI_UpdateNodeFlags()
        {
            return AddPostfix(typeof(RoadBaseAIPatches), nameof(RoadBaseAIPatches.UpdateNodeFlagsPostfix), typeof(RoadBaseAI), nameof(RoadBaseAI.UpdateNodeFlags));
        }

        #endregion

        #region HIDECROSSWALK

        private void PatchHideCrosswalk(ref bool success)
        {
            if (PluginUtil.GetHideCrossings().IsActive())
            {
                success &= AddPrefix(typeof(HideCrosswalksPatches), nameof(HideCrosswalksPatches.ShouldHideCrossingPrefix), typeof(HideCrosswalks.Patches.CalculateMaterialCommons), nameof(HideCrosswalks.Patches.CalculateMaterialCommons.ShouldHideCrossing));
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
            return AddPrefix(typeof(TMPEPatches), nameof(TMPEPatches.CanToggleTrafficLightPrefix), typeof(TrafficLightManager), nameof(TrafficLightManager.CanToggleTrafficLight));
        }
        private bool Patch_JunctionRestrictionsManager_GetDefaultEnteringBlockedJunctionAllowed()
        {
            return AddPrefix(typeof(TMPEPatches), nameof(TMPEPatches.GetDefaultEnteringBlockedJunctionAllowedPrefix), typeof(JunctionRestrictionsManager), nameof(JunctionRestrictionsManager.GetDefaultEnteringBlockedJunctionAllowed));
        }
        private bool Patch_JunctionRestrictionsManager_GetDefaultPedestrianCrossingAllowed()
        {
            return AddPrefix(typeof(TMPEPatches), nameof(TMPEPatches.GetDefaultPedestrianCrossingAllowedPrefix), typeof(JunctionRestrictionsManager), nameof(JunctionRestrictionsManager.GetDefaultPedestrianCrossingAllowed));
        }
        private bool Patch_JunctionRestrictionsManager_GetDefaultUturnAllowed()
        {
            return AddPrefix(typeof(TMPEPatches), nameof(TMPEPatches.GetDefaultUturnAllowedPrefix), typeof(JunctionRestrictionsManager), nameof(JunctionRestrictionsManager.GetDefaultUturnAllowed));
        }
        private bool Patch_JunctionRestrictionsManager_IsEnteringBlockedJunctionAllowedConfigurable()
        {
            return AddPrefix(typeof(TMPEPatches), nameof(TMPEPatches.IsEnteringBlockedJunctionAllowedConfigurablePrefix), typeof(JunctionRestrictionsManager), nameof(JunctionRestrictionsManager.IsEnteringBlockedJunctionAllowedConfigurable));
        }
        private bool Patch_JunctionRestrictionsManager_IsPedestrianCrossingAllowedConfigurable()
        {
            return AddPrefix(typeof(TMPEPatches), nameof(TMPEPatches.IsPedestrianCrossingAllowedConfigurablePrefix), typeof(JunctionRestrictionsManager), nameof(JunctionRestrictionsManager.IsPedestrianCrossingAllowedConfigurable));
        }
        private bool Patch_JunctionRestrictionsManager_IsUturnAllowedConfigurable()
        {
            return AddPrefix(typeof(TMPEPatches), nameof(TMPEPatches.IsUturnAllowedConfigurablePrefix), typeof(JunctionRestrictionsManager), nameof(JunctionRestrictionsManager.IsUturnAllowedConfigurable));
        }

        #endregion

        #region SIMULATIONSTEP

        private void PatchSimulationStep(ref bool success)
        {
            var parameters = new Type[] { typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(Vehicle.Frame).MakeByRefType(), typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(int) };

            success &= Patch_CarAI_SimulationStep_Prefix(parameters);
            success &= Patch_CarAI_SimulationStep_Transpiler(parameters);
            success &= Patch_CarTrailerAI_SimulationStep_Prefix(parameters);
            success &= Patch_CarTrailerAI_SimulationStep_Transpiler(parameters);
            success &= Patch_TrainAI_SimulationStep(parameters);
            success &= Patch_TramBaseAI_SimulationStep(parameters);

            if (PluginUtil.GetTrafficManager().IsActive())
            {
                success &= Patch_TMPE_CustomTrainAI_CustomSimulationStep(parameters);
                success &= Patch_TMPE_CustomTramBaseAI_CustomSimulationStep(parameters);
            }
        }
        private bool Patch_CarAI_SimulationStep_Prefix(Type[] parameters)
        {
            return AddPrefix(typeof(SimulationStepPatches), nameof(SimulationStepPatches.CarAISimulationStepPostfix), typeof(CarAI), nameof(CarAI.SimulationStep), parameters);
        }
        private bool Patch_CarAI_SimulationStep_Transpiler(Type[] parameters)
        {
            return AddTranspiler(typeof(SimulationStepPatches), nameof(SimulationStepPatches.CarAISimulationStepTranspiler), typeof(CarAI), nameof(CarAI.SimulationStep), parameters);
        }
        private bool Patch_CarTrailerAI_SimulationStep_Prefix(Type[] parameters)
        {
            return AddPrefix(typeof(SimulationStepPatches), nameof(SimulationStepPatches.CarTrailerAISimulationStepPostfix), typeof(CarTrailerAI), nameof(CarTrailerAI.SimulationStep), parameters);
        }
        private bool Patch_CarTrailerAI_SimulationStep_Transpiler(Type[] parameters)
        {
            return AddTranspiler(typeof(SimulationStepPatches), nameof(SimulationStepPatches.CarTrailerAISimulationStepTranspiler), typeof(CarTrailerAI), nameof(CarTrailerAI.SimulationStep), parameters);
        }
        private bool Patch_TrainAI_SimulationStep(Type[] parameters)
        {
            return AddPrefix(typeof(SimulationStepPatches), nameof(SimulationStepPatches.TrainAISimulationStepPostfix), typeof(TrainAI), nameof(TrainAI.SimulationStep), parameters);
        }
        private bool Patch_TMPE_CustomTrainAI_CustomSimulationStep(Type[] parameters)
        {
            return AddPrefix(typeof(SimulationStepPatches), nameof(SimulationStepPatches.TrainAISimulationStepPostfix), typeof(TrafficManager.Custom.AI.CustomTrainAI), nameof(TrafficManager.Custom.AI.CustomTrainAI.CustomSimulationStep), parameters);
        }
        private bool Patch_TramBaseAI_SimulationStep(Type[] parameters)
        {
            return AddPrefix(typeof(SimulationStepPatches), nameof(SimulationStepPatches.TramBaseAISimulationStepPostfix), typeof(TramBaseAI), nameof(TramBaseAI.SimulationStep), parameters);
        }
        private bool Patch_TMPE_CustomTramBaseAI_CustomSimulationStep(Type[] parameters)
        {
            return AddPrefix(typeof(SimulationStepPatches), nameof(SimulationStepPatches.TramBaseAISimulationStepPostfix), typeof(TrafficManager.Custom.AI.CustomTramBaseAI), nameof(TrafficManager.Custom.AI.CustomTramBaseAI.CustomSimulationStep), parameters);
        }

        #endregion
    }
}
