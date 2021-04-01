namespace NodeController {
    public interface INetworkData {
        /// <summary>
        /// marks the network for update in the next simulation step.
        /// once this node is updated, its recalculated.
        /// if the network is default/unsupported/invalid, manager will remove customisation.
        /// </summary>
        void Update();

        /// <summary>
        /// Respond to external changes:
        ///  - calculate new default values. (required by <see cref="IsDefault()"/> and <see cref="ResetToDefault()"/>)
        ///  - refresh node type, values.
        ///  external changes includes:
        ///  - segment added/remvoed
        ///  - MoveIT moves segment/node.
        ///
        /// Call this:
        /// - after initialization
        /// - after CS has calcualted node but before custom modifications has been made
        ///
        /// Note: this does not mark network for update but rather responds to network update.
        /// </summary>
        void Calculate();

        bool IsDefault();

        void ResetToDefault();

        /// <summary>
        /// Refreshes node state then marks the node for update.
        /// After major changes node must be refreshed before update so that the values are corrected. for example if node changes uturn corner offset must be a
        /// before calling update.
        /// its possible to call panel.Refresh() right after calling this method. in that case:
        ///  - RefreshAndUpdate() prepairs node state prepairing it for panel.Refresh()
        ///  - panel.Refresh() show/hide/enable/disable elements based on the current node state.
        ///  - then simulation thread recalculates values.
        ///  - OnAfterCalculate() will then Refreshes panel values.
        /// </summary>
        void RefreshAndUpdate();

        bool IsSelected();
    }

    public interface INetworkData<T> {
        T Clone();
    }
}
