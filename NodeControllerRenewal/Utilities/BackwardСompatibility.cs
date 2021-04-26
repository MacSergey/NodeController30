using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Linq;
using UnityEngine;

namespace NodeController.BackwardСompatibility
{
    public static class Loader
    {
        public static T Load<T>(byte[] data)
            where T : class
        {
            var surrogateSelector = new SurrogateSelector();
            surrogateSelector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), new Vector3Surrogate());

            var formatter = new BinaryFormatter()
            {
                Binder = new BackwardСompatibilityBinder(),
                SurrogateSelector = surrogateSelector,
            };

            using var memoryStream = new MemoryStream(data);
            return formatter.Deserialize(memoryStream) as T;
        }
    }

    public class BackwardСompatibilityBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            if (assemblyName == "NodeController")
            {
                switch (typeName)
                {
                    case "NodeController.LifeCycle.NCState": return typeof(NCState);
                    case "NodeController.GUI.GameConfigT": return typeof(GameConfigT);
                    case "NodeController.SegmentEndManager": return typeof(SegmentEndManager);
                    case "NodeController.NodeManager": return typeof(NodeManager);
                    case "NodeController.NodeData": return typeof(NodeData);
                    case "NodeController.NodeData[]": return typeof(NodeData[]);
                    case "NodeController.NodeTypeT": return typeof(NodeStyleType);
                    case "NodeController.SegmentEndData+CornerData": return typeof(CornerData);
                    case "NodeController.SegmentEndData": return typeof(SegmentEndData);
                    case "NodeController.SegmentEndData[]": return typeof(SegmentEndData[]);
                    case "KianCommons.Math.Vector3Serializable": return typeof(Vector3);
                }
            }

            var type = Type.GetType($"{typeName}, {assemblyName}");
            return type;
        }
    }

    [Serializable]
    public class NCState : ISerializable, IToXml
    {
        public SegmentEndManager SegmentEndManager { get; }
        public NodeManager NodeManager { get; }
        public Version Version { get; }

        public string XmlSection => throw new NotImplementedException();

        public NCState(SerializationInfo info, StreamingContext context)
        {
            Version = new Version(info.GetString("Version"));

            if (info.GetValue("NodeManagerData", typeof(byte[])) is byte[] nodeManagerData)
                NodeManager = Loader.Load<NodeManager>(nodeManagerData);

            if (info.GetValue("SegmentEndManagerData", typeof(byte[])) is byte[] segmentEndManagerData)
                SegmentEndManager = Loader.Load<SegmentEndManager>(segmentEndManagerData);
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context) { }

        public XElement ToXml()
        {
            var config = new XElement(nameof(NodeController));
            config.AddAttr("V", Version);

            var segmentsBuffer = SegmentEndManager.Buffer.GroupBy(i => i.NodeId).ToDictionary(i => i.Key, i => i.ToArray());

            foreach (var node in NodeManager.Buffer)
            {
                var nodeConfig = node.ToXml();
                config.Add(nodeConfig);
                if (segmentsBuffer.TryGetValue(node.Id, out var segments))
                {
                    foreach (var segment in segments)
                        nodeConfig.Add(segment.ToXml());
                }
            }

            return config;
        }
    }
    [Serializable]
    public class GameConfigT : ISerializable
    {
        public GameConfigT(SerializationInfo info, StreamingContext context) { }
        public void GetObjectData(SerializationInfo info, StreamingContext context) { }
    }
    [Serializable]
    public class NodeManager : ISerializable
    {
        public NodeData[] Buffer;
        public NodeManager(SerializationInfo info, StreamingContext context)
        {
            var buffer = (NodeData[])info.GetValue("buffer", typeof(NodeData[]));
            Buffer = buffer.Where(i => i != null).ToArray();
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context) { }
    }
    [Serializable]
    public class SegmentEndManager : ISerializable
    {
        public SegmentEndData[] Buffer;
        public SegmentEndManager(SerializationInfo info, StreamingContext context)
        {
            var buffer = (SegmentEndData[])info.GetValue("buffer", typeof(SegmentEndData[]));
            Buffer = buffer.Where(i => i != null).ToArray();
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context) { }
    }
    [Serializable]
    public class NodeData : ISerializable, IToXml
    {
        public ushort Id;
        public NodeStyleType NodeType;
        public NodeData(SerializationInfo info, StreamingContext context)
        {
            Id = info.GetUInt16("NodeID");
            NodeType = (NodeStyleType)info.GetValue("NodeType", typeof(NodeStyleType));
        }

        public string XmlSection => NodeController.NodeData.XmlName;

        public void GetObjectData(SerializationInfo info, StreamingContext context) { }

        public XElement ToXml()
        {
            var config = new XElement(NodeController.NodeData.XmlName);
            config.AddAttr(nameof(Id), Id);
            config.AddAttr("T", (int)NodeType);
            return config;
        }
        public override string ToString() => $"Node #{Id}";
    }
    [Serializable]
    public class SegmentEndData : ISerializable, IToXml
    {
        public ushort Id;
        public ushort NodeId;
        public CornerData LeftCorner;
        public CornerData RightCorner;
        public float SlopeAngle;
        public float TwistAngle;
        public float Stretch;
        public bool IsSlope;
        public bool NoMarkings;
        public SegmentEndData(SerializationInfo info, StreamingContext context)
        {
            Id = info.GetUInt16("SegmentID");
            NodeId = info.GetUInt16("NodeID");
            LeftCorner = (CornerData)info.GetValue("LeftCorner", typeof(CornerData));
            RightCorner = (CornerData)info.GetValue("RightCorner", typeof(CornerData));
            SlopeAngle = info.GetSingle("DeltaSlopeAngleDeg");
            TwistAngle = info.GetSingle("EmbankmentAngleDeg");
            Stretch = info.GetSingle("Stretch");
            IsSlope = !info.GetBoolean("FlatJunctions");
            NoMarkings = info.GetBoolean("NoMarkings");
        }

        public string XmlSection => NodeController.SegmentEndData.XmlName;

        public void GetObjectData(SerializationInfo info, StreamingContext context) { }

        public XElement ToXml()
        {
            var config = new XElement(XmlSection);

            config.AddAttr(nameof(Id), Id);
            config.AddAttr("SA", SlopeAngle);
            config.AddAttr("TA", TwistAngle);
            config.AddAttr("S", (LeftCorner.DeltaPos.x - RightCorner.DeltaPos.x) / 2f);
            config.AddAttr("NM", NoMarkings ? 1 : 0);
            config.AddAttr("IS", IsSlope ? 1 : 0);

            var leftOffset = LeftCorner.Offset + LeftCorner.DeltaPos.z;
            var rightOffset = RightCorner.Offset + RightCorner.DeltaPos.z;
            if (leftOffset != 0f || rightOffset != 0f)
                config.AddAttr("O", (leftOffset + rightOffset) / 2f);
            else
                config.AddAttr("KD", 1);

            if (Id.GetSegment().Info is NetInfo info)
            {
                var width = info.m_halfWidth * 2;
                var deltaWidth = LeftCorner.DeltaPos.x + RightCorner.DeltaPos.x;
                var stretch = (1 + Stretch * 0.01f) * ((width + deltaWidth) / width);
                config.AddAttr("ST", stretch);

                var tan = (leftOffset - rightOffset) / (width * stretch);
                config.AddAttr("RA", Mathf.Atan(tan) * Mathf.Rad2Deg);
            }

            return config;
        }
        public override string ToString() => $"Segment #{Id}; Node #{NodeId}";
    }
    [Serializable]
    public class CornerData : ISerializable
    {
        public float Offset;
        public Vector3 DeltaPos;
        public CornerData(SerializationInfo info, StreamingContext context)
        {
            Offset = info.GetSingle("Offset");
            DeltaPos = (Vector3)info.GetValue("DeltaPos", typeof(Vector3));
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context) { }
    }
    public class Vector3Surrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            var vector = (Vector3)obj;
            vector.x = info.GetSingle(nameof(Vector3.x));
            vector.y = info.GetSingle(nameof(Vector3.y));
            vector.z = info.GetSingle(nameof(Vector3.z));
            return vector;
        }
    }
}
