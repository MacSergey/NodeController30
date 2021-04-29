using ColossalFramework.UI;
using HarmonyLib;
using ICities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeController.Patches;
using NodeController.UI;
using NodeController.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection.Emit;
using TrafficManager.Manager.Impl;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ColossalFramework.Plugins.PluginManager;

namespace NodeController
{
    public class Mod : BasePatcherMod<Mod>
    {
        #region PROPERTIES

        public override string WorkshopUrl => "https://steamcommunity.com/sharedfiles/filedetails/?id=2462845270";
        public override string BetaWorkshopUrl => "https://steamcommunity.com/sharedfiles/filedetails/?id=2462845270";
        public override string NameRaw => "Node Controller Renewal";
        public override string Description => !IsBeta ? Localize.Mod_Description : Localize.Mod_DescriptionBeta;
        public override List<Version> Versions => new List<Version>()
        {
            new Version("1.0")
        };

        protected override string IdRaw => nameof(NodeController);
        public override CultureInfo Culture
        {
            get => Localize.Culture;
            protected set => Localize.Culture = value;
        }
#if BETA
        public override bool IsBeta => true;
#else
        protected override bool ModIsBeta => false;
#endif

        #endregion

        #region BASIC

        public override void OnLoadedError()
        {
            var messageBox = MessageBoxBase.ShowModal<TwoButtonMessageBox>();
            messageBox.CaptionText = SingletonMod<Mod>.Instance.Name;
            messageBox.MessageText = Localize.Mod_LoaledWithErrors;
            messageBox.Button1Text = ModLocalize<Mod>.Ok;
            messageBox.Button2Text = Localize.Mod_Support;
            messageBox.OnButton2Click = OpenWorkshop;
        }
        protected override void GetSettings(UIHelperBase helper)
        {
            var settings = new Settings();
            settings.OnSettingsUI(helper);
        }
        public override string GetLocalizeString(string str, CultureInfo culture = null) => Localize.ResourceManager.GetString(str, culture ?? Culture);

        #endregion

        #region PATCHER

        protected override bool PatchProcess()
        {
            var success = true;

            //success &= AddTool<NodeControllerTool>();
            //success &= AddNetToolButton<NodeControllerButton>();
            //success &= ToolOnEscape<NodeControllerTool>();
            //success &= AssetDataExtensionFix<AssetDataExtension>();

            success &= AddTool();
            success &= AddNetToolButton();
            success &= ToolOnEscape();
            success &= AssetDataExtensionFix();
            success &= AssetDataLoad();

            PatchNetManager(ref success);
            PatchNetNode(ref success);
            PatchNetSegment(ref success);
            PatchNetLane(ref success);
            PatchRoadBaseAI(ref success);
            PatchSimulationStep(ref success);

            if (DependencyUtilities.HideCrossings is null)
                Logger.Debug("Hide Crosswalks not exist, skip patches");
            else
                PatchHideCrosswalk(ref success);

            if (DependencyUtilities.TrafficManager is null)
                Logger.Debug("TMPE not exist, skip patches");
            else
                PatchTMPE(ref success);

            return success;
        }

        private bool AddTool()
        {
            return AddTranspiler(typeof(Mod), nameof(Mod.ToolControllerAwakeTranspiler), typeof(ToolController), "Awake");
        }
        public static IEnumerable<CodeInstruction> ToolControllerAwakeTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions) => ToolControllerAwakeTranspiler<NodeControllerTool>(generator, instructions);

        private bool AddNetToolButton()
        {
            return AddPostfix(typeof(Mod), nameof(Mod.GeneratedScrollPanelCreateOptionPanelPostfix), typeof(GeneratedScrollPanel), "CreateOptionPanel");
        }
        public static void GeneratedScrollPanelCreateOptionPanelPostfix(string templateName, ref OptionPanelBase __result) => GeneratedScrollPanelCreateOptionPanelPostfix<NodeControllerButton>(templateName, ref __result);

        protected bool ToolOnEscape()
        {
            return AddTranspiler(typeof(Mod), nameof(Mod.GameKeyShortcutsEscapeTranspiler), typeof(GameKeyShortcuts), "Escape");
        }
        protected static IEnumerable<CodeInstruction> GameKeyShortcutsEscapeTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions) => GameKeyShortcutsEscapeTranspiler<NodeControllerTool>(generator, instructions);

        private bool AssetDataExtensionFix()
        {
            return AddPostfix(typeof(Mod), nameof(Mod.LoadAssetPanelOnLoadPostfix), typeof(LoadAssetPanel), nameof(LoadAssetPanel.OnLoad));
        }
        private static void LoadAssetPanelOnLoadPostfix(LoadAssetPanel __instance, UIListBox ___m_SaveList) => AssetDataExtension.LoadAssetPanelOnLoadPostfix(__instance, ___m_SaveList);

        private bool AssetDataLoad()
        {
            return AddTranspiler(typeof(Mod), nameof(Mod.BuildingDecorationLoadPathsTranspiler), typeof(BuildingDecoration), nameof(BuildingDecoration.LoadPaths));
        }
        private static IEnumerable<CodeInstruction> BuildingDecorationLoadPathsTranspiler(IEnumerable<CodeInstruction> instructions) => AssetDataExtension.BuildingDecorationLoadPathsTranspiler(instructions);

        #region NETMANAGER

        private void PatchNetManager(ref bool success)
        {
            success &= Patch_NetManager_ReleaseNodeImplementation();
            success &= Patch_NetManager_SimulationStepImpl();
            success &= Patch_NetManager_UpdateSegment();
        }

        private bool Patch_NetManager_ReleaseNodeImplementation()
        {
            var parameters = new Type[] { typeof(ushort), typeof(NetNode).MakeByRefType() };
            return AddPrefix(typeof(Manager), nameof(Manager.ReleaseNodeImplementationPrefix), typeof(NetManager), "ReleaseNodeImplementation", parameters);
        }
        private bool Patch_NetManager_SimulationStepImpl()
        {
            return AddTranspiler(typeof(NetManagerPatches), nameof(NetManagerPatches.SimulationStepImplTranspiler), typeof(NetManager), "SimulationStepImpl");
        }
        private bool Patch_NetManager_UpdateSegment()
        {
            var parameters = new Type[] { typeof(ushort), typeof(ushort), typeof(int) };
            return AddTranspiler(typeof(NetManagerPatches), nameof(NetManagerPatches.UpdateSegmentTranspiler), typeof(NetManager), nameof(NetManager.UpdateSegment), parameters);
        }

        #endregion

        #region NETNODE

        private void PatchNetNode(ref bool success)
        {
            var junctionParams1 = new Type[] { typeof(ushort), typeof(NetInfo), typeof(uint) };
            var junctionParams2 = new Type[] { typeof(ushort), typeof(int), typeof(int), typeof(NetInfo), typeof(NetInfo), typeof(ushort), typeof(ushort), typeof(uint).MakeByRefType(), typeof(RenderManager.Instance).MakeByRefType() };
            var junctionParams3 = new Type[] { typeof(ushort), typeof(int), typeof(ushort), typeof(Vector3), typeof(uint).MakeByRefType(), typeof(RenderManager.Instance).MakeByRefType() };

            success &= Patch_NetNode_Position_Transpiler("RefreshBendData");
            success &= Patch_NetNode_Position_Transpiler("RefreshEndData");
            success &= Patch_NetNode_Position_Transpiler("RefreshJunctionData", junctionParams1);
            success &= Patch_NetNode_Position_Transpiler("RefreshJunctionData", junctionParams2);
            success &= Patch_NetNode_Position_Transpiler("RefreshJunctionData", junctionParams3);
            success &= Patch_NetNode_Position_Transpiler(nameof(NetNode.TerrainUpdated));

            success &= Patch_NetNode_RefreshJunctionData_Prefix(junctionParams3);
            success &= Patch_NetNode_RefreshJunctionData_Postfix(junctionParams3);
            success &= Patch_NetNode_RenderInstance();
        }
        private bool Patch_NetNode_Position_Transpiler(string methodName, Type[] parameters = null)
        {
            return AddTranspiler(typeof(NetNodePatches), nameof(NetNodePatches.ReplaceNodePositionTranspiler), typeof(NetNode), methodName, parameters);
        }
        private bool Patch_NetNode_RefreshJunctionData_Prefix(Type[] parameters)
        {
            return AddPrefix(typeof(NetNodePatches), nameof(NetNodePatches.RefreshJunctionDataPrefix), typeof(NetNode), "RefreshJunctionData", parameters);
        }
        private bool Patch_NetNode_RefreshJunctionData_Postfix(Type[] parameters)
        {
            return AddPostfix(typeof(NetNodePatches), nameof(NetNodePatches.RefreshJunctionDataPostfix), typeof(NetNode), "RefreshJunctionData", parameters);
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
            success &= Patch_NetSegment_CalculateCorner_Prefix();
            success &= Patch_NetSegment_FindDirection();
        }

        private bool Patch_NetSegment_CalculateCorner_Prefix()
        {
            var parameters = new Type[] { typeof(NetInfo), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(NetInfo), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(NetInfo), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(ushort), typeof(ushort), typeof(bool), typeof(bool), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(bool).MakeByRefType() };
            return AddPrefix(typeof(NetSegmentPatches), nameof(NetSegmentPatches.CalculateCornerPrefix), typeof(NetSegment), nameof(NetSegment.CalculateCorner), parameters);
        }
        private bool Patch_NetSegment_FindDirection()
        {
            return AddTranspiler(typeof(NetSegmentPatches), nameof(NetSegmentPatches.FindDirectionTranspiler), typeof(NetSegment), nameof(NetSegment.FindDirection));
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

        private bool Patch_NetLane_PopulateGroupData()
        {
            return AddTranspiler(typeof(NetLanePatches), nameof(NetLanePatches.NetLanePopulateGroupDataTranspiler), typeof(NetLane), nameof(NetLane.PopulateGroupData));
        }
        private bool Patch_NetLane_RefreshInstance()
        {
            return AddTranspiler(typeof(NetLanePatches), nameof(NetLanePatches.NetLaneRefreshInstanceTranspiler), typeof(NetLane), nameof(NetLane.RefreshInstance));
        }
        private bool Patch_NetLane_RenderInstance()
        {
            return AddTranspiler(typeof(NetLanePatches), nameof(NetLanePatches.NetLaneRenderInstanceTranspiler), typeof(NetLane), nameof(NetLane.RenderInstance));
        }
        private bool Patch_NetLane_RenderDestroyedInstance()
        {
            return AddTranspiler(typeof(NetLanePatches), nameof(NetLanePatches.NetLaneRenderDestroyedInstanceTranspiler), typeof(NetLane), nameof(NetLane.RenderDestroyedInstance));
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

        #region SIMULATIONSTEP

        private void PatchSimulationStep(ref bool success)
        {
            var parameters = new Type[] { typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(Vehicle.Frame).MakeByRefType(), typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(int) };

            success &= Patch_CarAI_SimulationStep_Transpiler(parameters);
            success &= Patch_CarTrailerAI_SimulationStep_Transpiler(parameters);
            success &= Patch_TrainAI_SimulationStep(parameters);
            success &= Patch_TramBaseAI_SimulationStep(parameters);
        }
        private bool Patch_CarAI_SimulationStep_Transpiler(Type[] parameters)
        {
            return AddTranspiler(typeof(SimulationStepPatches), nameof(SimulationStepPatches.SimulationStepTranspiler), typeof(CarAI), nameof(CarAI.SimulationStep), parameters);
        }
        private bool Patch_CarTrailerAI_SimulationStep_Transpiler(Type[] parameters)
        {
            return AddTranspiler(typeof(SimulationStepPatches), nameof(SimulationStepPatches.SimulationStepTrailerTranspiler), typeof(CarTrailerAI), nameof(CarTrailerAI.SimulationStep), parameters);
        }
        private bool Patch_TrainAI_SimulationStep(Type[] parameters)
        {
            return AddTranspiler(typeof(SimulationStepPatches), nameof(SimulationStepPatches.SimulationStepTranspiler), typeof(TrainAI), nameof(TrainAI.SimulationStep), parameters);
        }
        private bool Patch_TMPE_CustomTrainAI_CustomSimulationStep(Type[] parameters)
        {
            return AddTranspiler(typeof(SimulationStepPatches), nameof(SimulationStepPatches.SimulationStepTranspiler), typeof(TrafficManager.Custom.AI.CustomTrainAI), nameof(TrafficManager.Custom.AI.CustomTrainAI.CustomSimulationStep), parameters);
        }
        private bool Patch_TramBaseAI_SimulationStep(Type[] parameters)
        {
            return AddTranspiler(typeof(SimulationStepPatches), nameof(SimulationStepPatches.SimulationStepTranspiler), typeof(TramBaseAI), nameof(TramBaseAI.SimulationStep), parameters);
        }
        private bool Patch_TMPE_CustomTramBaseAI_CustomSimulationStep(Type[] parameters)
        {
            return AddTranspiler(typeof(SimulationStepPatches), nameof(SimulationStepPatches.SimulationStepTranspiler), typeof(TrafficManager.Custom.AI.CustomTramBaseAI), nameof(TrafficManager.Custom.AI.CustomTramBaseAI.CustomSimulationStep), parameters);
        }

        #endregion

        #region HIDECROSSWALK

        private void PatchHideCrosswalk(ref bool success)
        {
            success &= AddPrefix(typeof(ExternalModPatches), nameof(ExternalModPatches.ShouldHideCrossingPrefix), typeof(HideCrosswalks.Patches.CalculateMaterialCommons), nameof(HideCrosswalks.Patches.CalculateMaterialCommons.ShouldHideCrossing));
        }

        #endregion

        #region TMPE

        private void PatchTMPE(ref bool success)
        {
            success &= Patch_TrafficLightManager_CanToggleTrafficLight();
            success &= Patch_JunctionRestrictionsManager_GetDefaultEnteringBlockedJunctionAllowed();
            success &= Patch_JunctionRestrictionsManager_GetDefaultPedestrianCrossingAllowed();
            success &= Patch_JunctionRestrictionsManager_GetDefaultUturnAllowed();
            success &= Patch_JunctionRestrictionsManager_IsEnteringBlockedJunctionAllowedConfigurable();
            success &= Patch_JunctionRestrictionsManager_IsPedestrianCrossingAllowedConfigurable();
            success &= Patch_JunctionRestrictionsManager_IsUturnAllowedConfigurable();

            var parameters = new Type[] { typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(Vehicle.Frame).MakeByRefType(), typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(int) };
            success &= Patch_TMPE_CustomTrainAI_CustomSimulationStep(parameters);
            success &= Patch_TMPE_CustomTramBaseAI_CustomSimulationStep(parameters);
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

        #endregion
    }
}
