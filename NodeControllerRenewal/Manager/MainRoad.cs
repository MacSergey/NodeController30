using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeController.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;
using static ModsCommon.Utilities.VectorUtilsExtensions;

namespace NodeController
{
    public class MainRoad : IToXml
    {
        public static string XmlName => "MR";
        public string XmlSection => XmlName;

        public ushort First { get; set; }
        public ushort Second { get; set; }
        public bool Auto { get; set; } = true;

        public IEnumerable<ushort> Segments
        {
            get
            {
                if (First != 0)
                    yield return First;
                if (Second != 0)
                    yield return Second;
            }
        }

        public MainRoad(bool auto = true)
        {
            Auto = auto;
        }
        public MainRoad(ushort first, ushort second) : this(false)
        {
            First = first;
            Second = second;
        }

        public void Update(NodeData data)
        {
            switch (data.SegmentCount)
            {
                case 0:
                    First = 0;
                    Second = 0;
                    break;
                case 1:
                    First = data.SegmentEndDatas.First().Id;
                    Second = 0;
                    break;
                default:
                    if (Auto)
                        SetAutoPair(data);
                    else
                    {
                        var firstExist = data.TryGetSegment(First, out var firstEnd);
                        var secondExist = data.TryGetSegment(Second, out var secondEnd);

                        if (!firstExist && !secondExist)
                        {
                            SetAutoPair(data);
                            Auto = true;
                        }
                        else if (!firstExist)
                            SetPair(GetPairs(data, secondEnd));
                        else if (!secondExist)
                            SetPair(GetPairs(data, firstEnd));
                    }
                    break;
            }
        }
        private void SetPair(IEnumerable<RoadPair> pairs)
        {
            var pair = pairs.Aggregate((i, j) => i.Weight > j.Weight ? i : j);
            First = pair.First;
            Second = pair.Second;
        }
        private void SetAutoPair(NodeData data) => SetPair(GetPairs(data));

        private IEnumerable<RoadPair> GetPairs(NodeData data)
        {
            var ends = data.SegmentEndDatas.ToArray();
            for (var i = 0; i < ends.Length; i += 1)
            {
                for (var j = i + 1; j < ends.Length; j += 1)
                    yield return RoadPair.Get(ends[i], ends[j]);
            }
        }
        private IEnumerable<RoadPair> GetPairs(NodeData data, SegmentEndData segmentEnd)
        {
            foreach (var endData in data.SegmentEndDatas)
            {
                if (endData != segmentEnd)
                    yield return RoadPair.Get(segmentEnd, endData);
            }
        }

        public bool IsMain(ushort segmentId) => segmentId != 0 && (segmentId == First || segmentId == Second);
        public void Replace(ushort from, ushort to)
        {
            if (First == from)
                First = to;
            else if (Second == from)
                Second = to;
        }

        public XElement ToXml()
        {
            var config = new XElement(XmlSection);

            config.AddAttr("F", First);
            config.AddAttr("S", Second);
            config.AddAttr("A", Auto ? 1 : 0);

            return config;
        }

        public void FromXml(XElement config, NetObjectsMap map)
        {
            First = Get("F", config, map);
            Second = Get("S", config, map);
            Auto = config.GetAttrValue("A", 0) == 1;

            static ushort Get(string name, XElement config, NetObjectsMap map)
            {
                var id = config.GetAttrValue<ushort>(name, 0);
                return map.TryGetSegment(id, out var targetId) ? targetId : id;
            }
        }

        public override string ToString() => $"{First}-{Second}";
    }
    public struct RoadPair
    {
        public ushort First;
        public ushort Second;
        public float Weight;

        public static RoadPair Get(SegmentEndData first, SegmentEndData second)
        {
            var cos = Mathf.Clamp(NormalizeDotXZ(first.RawSegmentBezier.StartDirection, second.RawSegmentBezier.StartDirection), -1f, 1f);
            var pow = Mathf.Acos(cos) / Mathf.PI * 2 - 1;
            return new RoadPair()
            {
                First = first?.Id ?? 0,
                Second = second?.Id ?? 0,
                Weight = Mathf.Pow(first.Weight * second.Weight, pow),
            };
        }

        public override string ToString() => $"{First}-{Second}:{Weight}";
    }
}
