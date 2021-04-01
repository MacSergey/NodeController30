using ColossalFramework;
using HarmonyLib;
using KianCommons;

namespace NodeController.Patches
{
    [HarmonyPatch(typeof(NetNode), nameof(NetNode.CalculateNode))]
    static class CalculateNode
    {
        static void Postfix(ushort nodeID)
        {
            NodeManager.Instance.OnBeforeCalculateNodePatch(nodeID); // invalid/unsupported nodes are set to null.
            NodeData nodeData = NodeManager.Instance.buffer[nodeID];
            ref NetNode node = ref nodeID.ToNode();

            if (nodeData == null || nodeData.SegmentCount != 2)
                return;
            if (node.m_flags.IsFlagSet(NetNode.Flags.Outside))
                return;

            if (nodeData.NeedsTransitionFlag())
                node.m_flags |= NetNode.Flags.Transition;
            else
                node.m_flags &= ~NetNode.Flags.Transition;

            if (nodeData.NeedMiddleFlag())
            {
                node.m_flags &= ~(NetNode.Flags.Junction | NetNode.Flags.AsymForward | NetNode.Flags.AsymBackward);
                node.m_flags |= NetNode.Flags.Middle;
            }

            if (nodeData.NeedBendFlag())
            {
                node.m_flags &= ~(NetNode.Flags.Junction | NetNode.Flags.Middle);
                node.m_flags |= NetNode.Flags.Bend; // TODO set asymForward and asymBackward
            }

            if (nodeData.NeedJunctionFlag())
            {
                node.m_flags |= NetNode.Flags.Junction;
                node.m_flags &= ~(NetNode.Flags.Middle | NetNode.Flags.AsymForward | NetNode.Flags.AsymBackward | NetNode.Flags.Bend | NetNode.Flags.End);
            }

            node.m_flags &= ~NetNode.Flags.Moveable;
        }
    }
}
