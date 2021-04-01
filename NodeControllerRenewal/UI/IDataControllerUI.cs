namespace NodeController.GUI {
    public enum NetworkTypeT {
        None,
        Node,
        Segment,
        SegmentEnd,
        Lane,
    }

    public interface IDataControllerUI {
        /// <summary>
        /// Apply values to custom data.
        /// may call <see cref="Refresh()"/> or <see cref="RefreshValues()"/>  as appropriate.
        /// </summary>
        void Apply();

        /// <summary>
        /// read values from custom data and refresh GUI elements accordingly.
        /// this will resized/rearrange panel, change the visibility of GUI elements and invalidates them.
        /// this will not update/modify custom data but it does call *Manager.GetOrCreate().
        /// </summary>
        void Refresh();


        /// <summary>
        /// Fast version of <see cref="Refresh()"/>. it only reads values from custom data.
        /// however it does not resize panel, change elements visibility, or invalidate.
        /// </summary>
        void RefreshValues();

        void Reset();

        string HintHotkeys { get; }

        string HintDescription { get; }


    }
}
