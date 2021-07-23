using ModsCommon;
using ModsCommon.Utilities;
using System.Collections.Generic;
using System.Xml.Linq;

namespace NodeController
{
    public class AssetDataExtension : BaseNetAssetDataExtension<Mod, AssetDataExtension, NetObjectsMap>
    {
        public const string NC_ID = "NodeController_V1.0";

        protected override string DataId { get; } = $"{nameof(NodeController)}.Data";
        protected override string MapId { get; } = $"{nameof(NodeController)}.Map";

        protected override NetObjectsMap CreateMap(bool isSimple) => new NetObjectsMap(isSimple);
        protected override XElement GetConfig() => SingletonManager<Manager>.Instance.ToXml();

        protected override void PlaceAsset(XElement config, NetObjectsMap map) => SingletonManager<Manager>.Instance.FromXml(config, map);
    }
}
