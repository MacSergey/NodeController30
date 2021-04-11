using ICities;
using KianCommons;
using System;
using System.Collections.Generic;
using HarmonyLib;
using ColossalFramework.UI;
using System.Runtime.CompilerServices;
using NodeController;
using System.Reflection;
using ModsCommon;
using ModsCommon.Utilities;

namespace NodeController
{
    //[Serializable]
    //public class AssetData
    //{
    //    public string VersionString;
    //    public byte[] Records;
    //    public Version Version => new Version(VersionString);

    //    public static AssetData GetAssetData()
    //    {
    //        if (GetRecords() is not object[] records || records.Length == 0)
    //            return null;

    //        return new AssetData
    //        {
    //            Records = SerializationUtil.Serialize(records),
    //            VersionString = typeof(AssetData).VersionOf().ToString(3),
    //        };
    //    }

    //    public static object[] GetRecords()
    //    {
    //        Manager.ValidateAndHeal();
    //        List<object> records = new List<object>();
    //        for (ushort nodeID = 0; nodeID < NetManager.MAX_NODE_COUNT; ++nodeID)
    //        {
    //            if (CopyNode(nodeID) is object record)
    //                records.Add(record);
    //        }
    //        for (ushort segmentID = 0; segmentID < NetManager.MAX_SEGMENT_COUNT; ++segmentID)
    //        {
    //            if (CopySegment(segmentID) is object record)
    //                records.Add(record);
    //        }
    //        return records.ToArray();
    //    }

    //    public static object[] Deserialize(byte[] data)
    //    {
    //        var data2 = SerializationUtil.Deserialize(data, default);
    //        AssetData assetData = data2 as AssetData;

    //        return SerializationUtil.Deserialize(assetData.Records, assetData.Version) is not object[] records || records.Length == 0 ? null : records;
    //    }

    //    public byte[] Serialize() => SerializationUtil.Serialize(this);
    //}

    //public class AssetDataExtension : BaseAssetDataExtension<AssetDataExtension>
    //{
    //    public const string NC_ID = "NodeController_V1.0";

    //    public Dictionary<BuildingInfo, object[]> Asset2Records = new Dictionary<BuildingInfo, object[]>();

    //    public override void OnAssetLoaded(string name, object asset, Dictionary<string, byte[]> userData)
    //    {
    //        //if (asset is BuildingInfo prefab)
    //        //{
    //        //    if (userData != null && userData.TryGetValue(NC_ID, out byte[] data))
    //        //    {
    //        //        SingletonMod<Mod>.Logger.Debug("AssetDataExtension.OnAssetLoaded():  extracted data for " + NC_ID);
    //        //        object[] records = AssetData.Deserialize(data);
    //        //        if (records != null)
    //        //            Asset2Records[prefab] = records;
    //        //        SingletonMod<Mod>.Logger.Debug("AssetDataExtension.OnAssetLoaded(): records=" + records.ToSTR());
    //        //    }
    //        //}
    //    }

    //    public override void OnAssetSaved(string name, object asset, out Dictionary<string, byte[]> userData)
    //    {
    //        SingletonMod<Mod>.Logger.Debug($"AssetDataExtension.OnAssetSaved({name}, {asset}, userData) called");
    //        userData = null;
    //        if (asset is BuildingInfo prefab)
    //        {
    //            SingletonMod<Mod>.Logger.Debug($"AssetDataExtension.OnAssetSaved():  prefab is {prefab}");
    //            var assetData = AssetData.GetAssetData();
    //            if (assetData == null)
    //            {
    //                SingletonMod<Mod>.Logger.Debug("AssetDataExtension.OnAssetSaved(): there were no NC data.");
    //                return;
    //            }

    //            SingletonMod<Mod>.Logger.Debug($"AssetDataExtension.OnAssetSaved(): assetData={assetData}");
    //            userData = new Dictionary<string, byte[]>();
    //            userData.Add(NC_ID, assetData.Serialize());
    //        }
    //    }

    //    public static void PlaceAsset(BuildingInfo info, Dictionary<InstanceID, InstanceID> map)
    //    {
    //        if (SingletonItem<AssetDataExtension>.Instance.Asset2Records.TryGetValue(info, out var records))
    //        {
    //            int exceptionCount = 0;
    //            foreach (object record in records)
    //            {
    //                try
    //                {
    //                    //Paste(record, map);
    //                }
    //                catch (Exception e)
    //                {
    //                    SingletonMod<Mod>.Logger.Error(e);
    //                    exceptionCount++;
    //                }
    //            }
    //        }
    //        else
    //            SingletonMod<Mod>.Logger.Debug("PlaceAsset: records not found");
    //    }

    //    static AssetDataExtension()
    //    {
    //        try
    //        {
    //            RegisterEvent();
    //            SingletonMod<Mod>.Logger.Debug("registered OnNetworksMapped.");
    //        }
    //        catch
    //        {
    //            SingletonMod<Mod>.Logger.Error("[NOT CRITICAL]Could not register OnNetworksMapped. TMPE 11.5.3+ is required for loading intersections with NC data");
    //        }
    //    }

    //    [MethodImpl(MethodImplOptions.NoInlining)]
    //    static void RegisterEvent()
    //    {
    //        TrafficManager.Util.PlaceIntersectionUtil.OnPlaceIntersection += PlaceAsset;
    //    }
    //}
}
