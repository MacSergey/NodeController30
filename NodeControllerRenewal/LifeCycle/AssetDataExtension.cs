using ICities;
using KianCommons;
using System;
using System.Collections.Generic;
using static NodeController.LifeCycle.MoveItIntegration;
using static KianCommons.Assertion;
using KianCommons.Serialization;

namespace NodeController.LifeCycle
{
    using HarmonyLib;
    using ColossalFramework.UI;
    using System.Runtime.CompilerServices;
    using NodeController30;

    // Credits to boformer
    [HarmonyPatch(typeof(LoadAssetPanel), "OnLoad")]
    public static class OnLoadPatch
    {
        /// <summary>
        /// when loading asset from file, IAssetData.OnAssetLoaded() is called for all assets but the one that is loaded from file.
        /// this postfix calls IAssetData.OnAssetLoaded() for asset loaded from file.
        /// </summary>
        public static void Postfix(LoadAssetPanel __instance, UIListBox ___m_SaveList)
        {
            // Taken from LoadAssetPanel.OnLoad
            var selectedIndex = ___m_SaveList.selectedIndex;
            var getListingMetaDataMethod = typeof(LoadSavePanelBase<CustomAssetMetaData>).GetMethod(
                "GetListingMetaData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var listingMetaData = (CustomAssetMetaData)getListingMetaDataMethod.Invoke(__instance, new object[] { selectedIndex });

            // Taken from LoadingManager.LoadCustomContent
            if (listingMetaData.userDataRef != null)
            {
                AssetDataWrapper.UserAssetData userAssetData = listingMetaData.userDataRef.Instantiate() as AssetDataWrapper.UserAssetData;
                if (userAssetData == null)
                {
                    userAssetData = new AssetDataWrapper.UserAssetData();
                }
                AssetDataExtension.Instance.OnAssetLoaded(listingMetaData.name, ToolsModifierControl.toolController.m_editPrefabInfo, userAssetData.Data);
            }
        }
    }

    [Serializable]
    public class AssetData
    {
        public string VersionString;
        public byte[] Records;
        public Version Version => new Version(VersionString);

        public static AssetData GetAssetData()
        {
            var records = GetRecords();
            if (records == null || records.Length == 0)
                return null;
            return new AssetData
            {
                Records = SerializationUtil.Serialize(records),
                VersionString = typeof(AssetData).VersionOf().ToString(3),
            };
        }

        public static object[] GetRecords()
        {
            NodeManager.ValidateAndHeal(false);
            List<object> records = new List<object>();
            for (ushort nodeID = 0; nodeID < NetManager.MAX_NODE_COUNT; ++nodeID)
            {
                object record = CopyNode(nodeID);
                if (record != null)
                    records.Add(record);
            }
            for (ushort segmentID = 0; segmentID < NetManager.MAX_SEGMENT_COUNT; ++segmentID)
            {
                object record = CopySegment(segmentID);
                if (record != null)
                    records.Add(record);
            }
            return records.ToArray();
        }

        public static object[] Deserialize(byte[] data)
        {
            AssertNotNull(data, "data");
            var data2 = SerializationUtil.Deserialize(data, default);
            AssertNotNull(data2, "data2");
            AssetData assetData = data2 as AssetData;
            AssertNotNull(assetData, "assetData");

            var records = SerializationUtil.Deserialize(assetData.Records, assetData.Version) as object[];
            if (records == null || records.Length == 0) return null;
            return records;
        }

        public byte[] Serialize() => SerializationUtil.Serialize(this);
    }

    public class AssetDataExtension : AssetDataExtensionBase
    {
        public const string NC_ID = "NodeController_V1.0";
        //static Building[] buildingBuffer = BuildingManager.instance.m_buildings.m_buffer;

        public static AssetDataExtension Instance;
        public Dictionary<BuildingInfo, object[]> Asset2Records = new Dictionary<BuildingInfo, object[]>();

        public override void OnCreated(IAssetData assetData)
        {
            base.OnCreated(assetData);
            Instance = this;
        }

        public override void OnReleased()
        {
            Instance = null;
        }

        public override void OnAssetLoaded(string name, object asset, Dictionary<string, byte[]> userData)
        {
            //return;
            //Log.Debug($"AssetDataExtension.OnAssetLoaded({name}, {asset}, userData) called");
            if (asset is BuildingInfo prefab)
            {
                //Log.Debug("AssetDataExtension.OnAssetLoaded():  prefab is " + prefab);

                if (userData != null && userData.TryGetValue(NC_ID, out byte[] data))
                {
                    Mod.Logger.Debug("AssetDataExtension.OnAssetLoaded():  extracted data for " + NC_ID);
                    object[] records = AssetData.Deserialize(data);
                    if (records != null)
                        Asset2Records[prefab] = records;
                    Mod.Logger.Debug("AssetDataExtension.OnAssetLoaded(): records=" + records.ToSTR());

                }
            }
        }

        public override void OnAssetSaved(string name, object asset, out Dictionary<string, byte[]> userData)
        {
            Mod.Logger.Debug($"AssetDataExtension.OnAssetSaved({name}, {asset}, userData) called");
            userData = null;
            if (asset is BuildingInfo prefab)
            {
                Mod.Logger.Debug("AssetDataExtension.OnAssetSaved():  prefab is " + prefab);
                var assetData = AssetData.GetAssetData();
                if (assetData == null)
                {
                    Mod.Logger.Debug("AssetDataExtension.OnAssetSaved(): there were no NC data.");
                    return;
                }

                Mod.Logger.Debug("AssetDataExtension.OnAssetSaved(): assetData=" + assetData);
                userData = new Dictionary<string, byte[]>();
                userData.Add(NC_ID, assetData.Serialize());
            }
        }

        public static void PlaceAsset(BuildingInfo info, Dictionary<InstanceID, InstanceID> map)
        {
            if (Instance.Asset2Records.TryGetValue(info, out var records))
            {
                Mod.Logger.Debug("PlaceAsset: records = " + records.ToSTR());
                Mod.Logger.Debug("PlaceAsset: map = " + map.ToSTR());
                int exceptionCount = 0;
                foreach (object record in records)
                {
                    try
                    {
                        Paste(record, map);
                    }
                    catch (Exception e)
                    {
                        Mod.Logger.Error(e);
                        exceptionCount++;
                    }
                }
            }
            else
            {
                Mod.Logger.Debug("PlaceAsset: records not found");
            }
        }

        static AssetDataExtension()
        {
            try
            {
                RegisterEvent();
                Mod.Logger.Debug("registered OnNetworksMapped.");
            }
            catch
            {
                Mod.Logger.Error("[NOT CRITICAL]Could not register OnNetworksMapped. TMPE 11.5.3+ is required for loading intersections with NC data");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void RegisterEvent()
        {
            TrafficManager.Util.PlaceIntersectionUtil.OnPlaceIntersection += PlaceAsset;
        }
    }
}
