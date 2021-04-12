
using HarmonyLib;
using ICities;
using KianCommons;
using ModsCommon;
using NodeController.GUI;
using NodeController.Patches;
using NodeController.UI;
using NodeController.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using TrafficManager.Manager.Impl;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NodeController
{
    public class Mod : BasePatcherMod<Mod>
    {
        #region PROPERTIES

        public override string WorkshopUrl => throw new NotImplementedException();
        public override string NameRaw => "Node Controller Renewal";
        public override string Description => string.Empty;
        public override List<Version> Versions => new List<Version>();

        protected override string IdRaw => nameof(NodeController);
        public override bool IsBeta => true;
        protected override string Locale => string.Empty;

        public static SimulationManager.UpdateMode UpdateMode => SimulationManager.instance.m_metaData.m_updateMode;
        public static LoadMode Mode => (LoadMode)UpdateMode;
        public static bool Loaded { get; private set; }
        public static string Scene => SceneManager.GetActiveScene().name;

        #endregion

        #region BASIC

        public override void OnEnabled()
        {
            base.OnEnabled();

            Loaded = false;

            LoadingManager.instance.m_simulationDataReady += SimulationDataReady; // load/update data
            if (SceneManager.GetActiveScene().name != "IntroScreen" && SceneManager.GetActiveScene().name != "Startup")
                SimulationDataReady();
        }

        public override void OnDisabled()
        {
            base.OnDisabled();

            LoadingManager.instance.m_simulationDataReady -= SimulationDataReady;
        }
        public void SimulationDataReady()
        {
            try
            {
                if (Scene == "ThemeEditor")
                    return;

                GUI.Settings.GameConfig ??= Mode switch
                {
                    LoadMode.NewGameFromScenario or LoadMode.LoadScenario or LoadMode.LoadMap => GameConfigT.LoadGameDefault,// no NC or old NC
                    _ => GameConfigT.NewGameDefault,
                };

                Loaded = true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected override void GetSettings(UIHelperBase helper) => GUI.Settings.OnSettingsUI(helper);

        #endregion

        #region PATCHER

        protected override bool PatchProcess()
        {
            var success = true;

            success &= AddTool<NodeControllerTool>();
            success &= AddNetToolButton<NodeControllerButton>();
            success &= ToolOnEscape<NodeControllerTool>();
            //success &= AssetDataExtensionFix<AssetDataExtension>();

            PatchNetManager(ref success);
            PatchNetNode(ref success);
            PatchNetSegment(ref success);
            PatchNetLane(ref success);
            PatchRoadBaseAI(ref success);
            PatchHideCrosswalk(ref success);
            PatchTMPE(ref success);
            PatchSimulationStep(ref success);

            return success;
        }

        #region NETMANAGER

        private void PatchNetManager(ref bool success)
        {
            success &= Patch_NetManager_ReleaseNodeImplementation();
            success &= Patch_NetManager_UpdateNode();
            success &= Patch_NetManager_SimulationStepImpl();
        }

        private bool Patch_NetManager_ReleaseNodeImplementation()
        {
            return AddPrefix(typeof(Manager), nameof(Manager.ReleaseNodeImplementationPrefix), typeof(NetManager), "ReleaseNodeImplementation", new Type[] { typeof(ushort) });
        }
        private bool Patch_NetManager_UpdateNode()
        {
            var parameters = new Type[] { typeof(ushort), typeof(ushort), typeof(int) };
            return AddPostfix(typeof(Manager), nameof(Manager.NetManagerUpdateNodePostfix), typeof(NetManager), nameof(NetManager.UpdateNode), parameters);
        }
        private bool Patch_NetManager_SimulationStepImpl()
        {
            return AddPostfix(typeof(Manager), nameof(Manager.NetManagerSimulationStepImplPostfix), typeof(NetManager), "SimulationStepImpl");
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
            try { var type = typeof(HideCrosswalks.Patches.CalculateMaterialCommons); }
            catch
            {
                Logger.Debug("Hide Crosswalks not exist, skip patches");
                return;
            }

            success &= AddPrefix(typeof(ExternalModPatches), nameof(ExternalModPatches.ShouldHideCrossingPrefix), typeof(HideCrosswalks.Patches.CalculateMaterialCommons), nameof(HideCrosswalks.Patches.CalculateMaterialCommons.ShouldHideCrossing));
        }

        #endregion

        #region TMPE

        private void PatchTMPE(ref bool success)
        {
            try { var type = typeof(TrafficLightManager); }
            catch
            {
                Logger.Debug("TMPE not exist, skip patches");
                return;
            }

            success &= Patch_TrafficLightManager_CanToggleTrafficLight();
            success &= Patch_JunctionRestrictionsManager_GetDefaultEnteringBlockedJunctionAllowed();
            success &= Patch_JunctionRestrictionsManager_GetDefaultPedestrianCrossingAllowed();
            success &= Patch_JunctionRestrictionsManager_GetDefaultUturnAllowed();
            success &= Patch_JunctionRestrictionsManager_IsEnteringBlockedJunctionAllowedConfigurable();
            success &= Patch_JunctionRestrictionsManager_IsPedestrianCrossingAllowedConfigurable();
            success &= Patch_JunctionRestrictionsManager_IsUturnAllowedConfigurable();
        }
        private bool Patch_TrafficLightManager_CanToggleTrafficLight()
        {
            return AddPrefix(typeof(ExternalModPatches), nameof(ExternalModPatches.CanToggleTrafficLightPrefix), typeof(TrafficLightManager), nameof(TrafficLightManager.CanToggleTrafficLight));
        }
        private bool Patch_JunctionRestrictionsManager_GetDefaultEnteringBlockedJunctionAllowed()
        {
            return AddPrefix(typeof(ExternalModPatches), nameof(ExternalModPatches.GetDefaultEnteringBlockedJunctionAllowedPrefix), typeof(JunctionRestrictionsManager), nameof(JunctionRestrictionsManager.GetDefaultEnteringBlockedJunctionAllowed));
        }
        private bool Patch_JunctionRestrictionsManager_GetDefaultPedestrianCrossingAllowed()
        {
            return AddPrefix(typeof(ExternalModPatches), nameof(ExternalModPatches.GetDefaultPedestrianCrossingAllowedPrefix), typeof(JunctionRestrictionsManager), nameof(JunctionRestrictionsManager.GetDefaultPedestrianCrossingAllowed));
        }
        private bool Patch_JunctionRestrictionsManager_GetDefaultUturnAllowed()
        {
            return AddPrefix(typeof(ExternalModPatches), nameof(ExternalModPatches.GetDefaultUturnAllowedPrefix), typeof(JunctionRestrictionsManager), nameof(JunctionRestrictionsManager.GetDefaultUturnAllowed));
        }
        private bool Patch_JunctionRestrictionsManager_IsEnteringBlockedJunctionAllowedConfigurable()
        {
            return AddPrefix(typeof(ExternalModPatches), nameof(ExternalModPatches.IsEnteringBlockedJunctionAllowedConfigurablePrefix), typeof(JunctionRestrictionsManager), nameof(JunctionRestrictionsManager.IsEnteringBlockedJunctionAllowedConfigurable));
        }
        private bool Patch_JunctionRestrictionsManager_IsPedestrianCrossingAllowedConfigurable()
        {
            return AddPrefix(typeof(ExternalModPatches), nameof(ExternalModPatches.IsPedestrianCrossingAllowedConfigurablePrefix), typeof(JunctionRestrictionsManager), nameof(JunctionRestrictionsManager.IsPedestrianCrossingAllowedConfigurable));
        }
        private bool Patch_JunctionRestrictionsManager_IsUturnAllowedConfigurable()
        {
            return AddPrefix(typeof(ExternalModPatches), nameof(ExternalModPatches.IsUturnAllowedConfigurablePrefix), typeof(JunctionRestrictionsManager), nameof(JunctionRestrictionsManager.IsUturnAllowedConfigurable));
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

            try { var type = typeof(TrafficLightManager); }
            catch
            {
                Logger.Debug("TMPE not exist, skip patches");
                return;
            }

            success &= Patch_TMPE_CustomTrainAI_CustomSimulationStep(parameters);
            success &= Patch_TMPE_CustomTramBaseAI_CustomSimulationStep(parameters);
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

        #endregion
    }
}
