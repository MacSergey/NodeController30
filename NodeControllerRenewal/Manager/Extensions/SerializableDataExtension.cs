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

namespace NodeController
{
    public class SerializableDataExtension : BaseSerializableDataExtension<SerializableDataExtension, Mod>
    {
        private const string DATA_ID0 = "RoadTransitionManager_V1.0";
        private const string DATA_ID1 = "NodeController_V1.0";
        private const string DATA_ID = "NodeController_V2.0";

        protected override string Id => nameof(NodeController);
        public bool WasImported { get; private set; }

        protected override XElement GetSaveData() => SingletonManager<Manager>.Instance.ToXml();
        protected override void SetLoadData(XElement config) => SingletonManager<Manager>.Instance.FromXml(config, new NetObjectsMap());

        public override void OnLoadData()
        {
            if (serializableDataManager.LoadData(DATA_ID) is byte[] data)
            {
                SingletonMod<Mod>.Logger.Debug($"Import NC2 data");

                WasImported = true;
                var state = Backward—ompatibility.Loader.Load<Backward—ompatibility.NCState>(data);
                var config = state.ToXml();
                SetLoadData(config);
            }
            else
            {
                WasImported = false;
                base.OnLoadData();
            }
        }
        public override void OnSaveData()
        {
            base.OnSaveData();

            serializableDataManager.EraseData(DATA_ID);
            serializableDataManager.EraseData(DATA_ID1);
            serializableDataManager.EraseData(DATA_ID0);
        }
    }
}
