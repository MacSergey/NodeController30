using ColossalFramework.Plugins;
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
using System.Resources;
using TrafficManager.Manager.Impl;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NodeController
{
    public class Mod : BasePatcherMod<Mod>
    {
        #region PROPERTIES

        protected override ulong StableWorkshopId => 2472062376ul;
        protected override ulong BetaWorkshopId => 2462845270ul;

        public override string NameRaw => "Node Controller Renewal";
        public override string Description => !IsBeta ? Localize.Mod_Description : CommonLocalize.Mod_DescriptionBeta;
        public override List<ModVersion> Versions => new List<ModVersion>()
        {
            new ModVersion(new Version("3.3.2"), new DateTime(2022, 9, 24)),
            new ModVersion(new Version("3.3.1"), new DateTime(2022, 9, 14)),
            new ModVersion(new Version("3.3"), new DateTime(2022, 7, 9)),
            new ModVersion(new Version("3.2"), new DateTime(2022, 6, 4)),
            new ModVersion(new Version("3.1.3"), new DateTime(2021, 8, 25)),
            new ModVersion(new Version("3.1.2"), new DateTime(2021, 8, 7)),
            new ModVersion(new Version("3.1.1"), new DateTime(2021, 7, 23)),
            new ModVersion(new Version("3.1"), new DateTime(2021, 7, 6)),
            new ModVersion(new Version("3.0.5"), new DateTime(2021, 6, 8)),
            new ModVersion(new Version("3.0.4"), new DateTime(2021, 5, 29)),
            new ModVersion(new Version("3.0.3"), new DateTime(2021, 5, 19)),
            new ModVersion(new Version("3.0.2"), new DateTime(2021, 5, 17)),
            new ModVersion(new Version("3.0.1"), new DateTime(2021, 5, 11)),
            new ModVersion(new Version("3.0"), new DateTime(2021, 4, 30)),
        };
        protected override Version RequiredGameVersion => new Version(1, 16, 0, 3);

        protected override string IdRaw => nameof(NodeController);
        protected override List<BaseDependencyInfo> DependencyInfos
        {
            get
            {
                var infos = base.DependencyInfos;

                var info = new ConflictDependencyInfo(DependencyState.Disable, DependencyUtilities.NC2Searcher);
                infos.Add(info);

                var crossingInfo = new ConflictDependencyInfo(DependencyState.Unsubscribe, PluginUtilities.GetSearcher("Pedestrian Crossings", 427258853ul));
                infos.Add(crossingInfo);

                return infos;
            }
        }

#if BETA
        public override bool IsBeta => true;
#else
        public override bool IsBeta => false;
#endif
        #endregion
        protected override LocalizeManager LocalizeManager => Localize.LocaleManager;
        protected override bool NeedMonoDevelopImpl => true;
        public override string CrowdinUrl => "https://crowdin.com/translate/node-controller-renewal/50";

        #region BASIC

        protected override void GetSettings(UIHelperBase helper)
        {
            var settings = new Settings();
            settings.OnSettingsUI(helper);
        }
        protected override void SetCulture(CultureInfo culture) => Localize.Culture = culture;

        #endregion

        #region PATCHER

        protected override bool PatchProcess()
        {
            var success = true;

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

            success &= Patch_CitizenAI_GetPathTargetPosition();

            if (DependencyUtilities.TrafficManager is null)
                Logger.Debug("TMPE not exist, skip patches");
            else
            {
                PatchTMPE(ref success);

                if (DependencyUtilities.HideCrossings is null)
                    Logger.Debug("Hide Crosswalks not exist, skip patches");
                else
                    PatchHideCrosswalk(ref success);
            }

            return success;
        }
        private static bool MVPatch(ref bool __result)
        {
            __result = true;
            return false;
        }

        private bool AddTool()
        {
            return AddTranspiler(typeof(Patcher), nameof(Patcher.ToolControllerAwakeTranspiler), typeof(ToolController), "Awake");
        }

        private bool AddNetToolButton()
        {
            return AddPostfix(typeof(Patcher), nameof(Patcher.GeneratedScrollPanelCreateOptionPanelPostfix), typeof(GeneratedScrollPanel), "CreateOptionPanel");
        }

        protected bool ToolOnEscape()
        {
            return AddTranspiler(typeof(Patcher), nameof(Patcher.GameKeyShortcutsEscapeTranspiler), typeof(GameKeyShortcuts), "Escape");
        }

        private bool AssetDataExtensionFix()
        {
            return AddPostfix(typeof(Patcher), nameof(Patcher.LoadAssetPanelOnLoadPostfix), typeof(LoadAssetPanel), nameof(LoadAssetPanel.OnLoad));
        }

        private bool AssetDataLoad()
        {
            return AddTranspiler(typeof(Patcher), nameof(Patcher.BuildingDecorationLoadPathsTranspiler), typeof(BuildingDecoration), nameof(BuildingDecoration.LoadPaths));
        }

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
            success &= Patch_NetNode_Position_Transpiler("RefreshJunctionData", junctionParams2);
            success &= Patch_NetNode_Position_Transpiler("RefreshJunctionData", junctionParams3);
            success &= Patch_NetNode_Position_Transpiler(nameof(NetNode.TerrainUpdated));

            success &= Patch_NetNode_RefreshJunctionData_Prefix(junctionParams3);
            success &= Patch_NetNode_RefreshJunctionData_Postfix(junctionParams3);
            success &= Patch_NetNode_CalculateNode();
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
        private bool Patch_NetNode_CalculateNode()
        {
            return AddPostfix(typeof(NetNodePatches), nameof(NetNodePatches.CalculateNodePostfix), typeof(NetNode), nameof(NetNode.CalculateNode));
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
            return AddTranspiler(typeof(SimulationStepPatches), nameof(SimulationStepPatches.SimulationStepTranspiler), Type.GetType("TrafficManager.Custom.AI.CustomTrainAI"), "CustomSimulationStep", parameters);
        }
        private bool Patch_TramBaseAI_SimulationStep(Type[] parameters)
        {
            return AddTranspiler(typeof(SimulationStepPatches), nameof(SimulationStepPatches.SimulationStepTranspiler), typeof(TramBaseAI), nameof(TramBaseAI.SimulationStep), parameters);
        }
        private bool Patch_TMPE_CustomTramBaseAI_CustomSimulationStep(Type[] parameters)
        {
            return AddTranspiler(typeof(SimulationStepPatches), nameof(SimulationStepPatches.SimulationStepTranspiler), Type.GetType("TrafficManager.Custom.AI.CustomTramBaseAI"), "CustomSimulationStep", parameters);
        }
        private bool Patch_TMPE_TrainAI_SimulationStep2Patch()
        {
            return AddTranspiler(typeof(SimulationStepPatches), nameof(SimulationStepPatches.SimulationStepTranspiler), Type.GetType("TrafficManager.Patch._VehicleAI._TrainAI.SimulationStep2Patch"), "Prefix");
        }

        #endregion

        private bool Patch_CitizenAI_GetPathTargetPosition()
        {
            if (Settings.LongIntersectionFix)
            {
                return AddTranspiler(typeof(CitizenAIPatches), nameof(CitizenAIPatches.GetPathTargetPositionTranspilar), typeof(CitizenAI), "GetPathTargetPosition");
            }
            else
                return true;
        }

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

            if ((Type.GetType("TrafficManager.TrafficManagerMod") ?? Type.GetType("TrafficManager.Lifecycle.TrafficManagerMod")) is Type tmpeMod)
            {
                if (tmpeMod.Assembly.GetName().Version < new Version(11, 5, 4))
                {
                    var parameters = new Type[] { typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(Vehicle.Frame).MakeByRefType(), typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(int) };

                    success &= Patch_TMPE_CustomTrainAI_CustomSimulationStep(parameters);
                    success &= Patch_TMPE_CustomTramBaseAI_CustomSimulationStep(parameters);
                }
                else
                    success &= Patch_TMPE_TrainAI_SimulationStep2Patch();
            }
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

    public static class Patcher
    {
        public static IEnumerable<CodeInstruction> ToolControllerAwakeTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions) => ModsCommon.Patcher.ToolControllerAwakeTranspiler<Mod, NodeControllerTool>(generator, instructions);

        public static void GeneratedScrollPanelCreateOptionPanelPostfix(string templateName, ref OptionPanelBase __result) => ModsCommon.Patcher.GeneratedScrollPanelCreateOptionPanelPostfix<Mod, NodeControllerButton>(templateName, ref __result, ModsCommon.Patcher.RoadsOptionPanel, ModsCommon.Patcher.PathsOptionPanel, ModsCommon.Patcher.QuaysOptionPanel, ModsCommon.Patcher.CanalsOptionPanel, ModsCommon.Patcher.FloodWallsOptionPanel);

        public static IEnumerable<CodeInstruction> GameKeyShortcutsEscapeTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions) => ModsCommon.Patcher.GameKeyShortcutsEscapeTranspiler<Mod, NodeControllerTool>(generator, instructions);

        public static void LoadAssetPanelOnLoadPostfix(LoadAssetPanel __instance, UIListBox ___m_SaveList) => ModsCommon.Patcher.LoadAssetPanelOnLoadPostfix<AssetDataExtension>(__instance, ___m_SaveList);

        public static IEnumerable<CodeInstruction> BuildingDecorationLoadPathsTranspiler(IEnumerable<CodeInstruction> instructions) => ModsCommon.Patcher.BuildingDecorationLoadPathsTranspiler<AssetDataExtension>(instructions);
    }
}
