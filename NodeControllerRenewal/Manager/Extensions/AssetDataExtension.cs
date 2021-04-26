using ModsCommon;
using ModsCommon.Utilities;
using System.Collections.Generic;
using System.Xml.Linq;

namespace NodeController
{
    public class AssetDataExtension : BaseIntersectionAssetDataExtension<Mod, AssetDataExtension, ObjectsMap>
    {
        public const string NC_ID = "NodeController_V1.0";

        protected override string DataId { get; } = $"{nameof(NodeController)}.Data";
        protected override string MapId { get; } = $"{nameof(NodeController)}.Map";

        protected override ObjectsMap CreateMap(bool isSimple) => new ObjectsMap(isSimple);
        protected override XElement GetConfig() => SingletonManager<Manager>.Instance.ToXml();

        protected override void PlaceAsset(XElement config, ObjectsMap map)
        {
            foreach (var items in map.Values)
            {
                if (items.Type == ObjectId.NodeType)
                {
                    ref var node = ref items.Node.GetNode();
                    node.CalculateNode(items.Node);
                }
            }
            SingletonManager<Manager>.Instance.FromXml(config, map);
        }
    }
}
