using ModsCommon;
using ModsCommon.Utilities;
using MoveItIntegration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace NodeController.Utilities
{
    public class MoveItIntegrationFactory : IMoveItIntegrationFactory
    {
        public MoveItIntegrationBase GetInstance() => new MoveItIntegration();
    }
    public class MoveItIntegration : MoveItIntegrationBase
    {
        public override string ID => "CS.macsergey.NodeControllerRenewal";

        public override Version DataVersion => new Version(3, 0);

        public override object Copy(InstanceID sourceInstanceID)
        {
            if (SingletonManager<Manager>.Instance[sourceInstanceID.NetNode] is NodeData data)
                return data.ToXml();
            else
                return null;
        }
        public override void Paste(InstanceID targetInstanceID, object record, Dictionary<InstanceID, InstanceID> sourceMap)
        {
            if (record is not XElement config || targetInstanceID.NetNode == 0)
                return;

            if (SingletonManager<Manager>.Instance[targetInstanceID.NetNode, true] is NodeData data)
            {
                var map = new ObjectsMap();
                map.FromDictionary(sourceMap);
                data.FromXml(config, map);
            }
        }

        public override string Encode64(object record) => record == null ? null : EncodeUtil.BinaryEncode64(record?.ToString());
        public override object Decode64(string record, Version dataVersion)
        {
            if (record == null || record.Length == 0)
                return null;

            using StringReader input = new StringReader((string)EncodeUtil.BinaryDecode64(record));
            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
            {
                IgnoreWhitespace = true,
                ProhibitDtd = false,
                XmlResolver = null
            };
            using XmlReader reader = XmlReader.Create(input, xmlReaderSettings);
            return XElement.Load(reader, LoadOptions.None);
        }
    }
}
