using System.Collections.Generic;
using System.Linq;

namespace StickmanOfWar.Map
{
    public class MapGraph
    {
        public int Act;
        public Dictionary<int, MapNode> Nodes = new Dictionary<int, MapNode>();
        public List<int> StartNodeIds = new List<int>();
        public int BossNodeId;
        public string ChosenBossId;

        public bool IsNodeAvailable(int id)
        {
            MapNode node = Nodes[id];
            if (node.IsCompleted) return false;

            bool anyCompleted = Nodes.Values.Any(n => n.IsCompleted);
            if (!anyCompleted)
            {
                return StartNodeIds.Contains(id);
            }

            foreach (int prevId in node.PrevIds)
            {
                if (Nodes.TryGetValue(prevId, out MapNode prevNode) && prevNode.IsCompleted)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
