using System.Collections.Generic;

namespace StickmanOfWar.Map
{
    public class MapGraph
    {
        public int Act;
        public Dictionary<int, MapNode> Nodes = new Dictionary<int, MapNode>();
        public List<int> StartNodeIds = new List<int>();
        public int BossNodeId;
        public string ChosenBossId;
        public int? CurrentPositionNodeId;

        public bool IsNodeAvailable(int id)
        {
            MapNode node = Nodes[id];
            if (node.IsCompleted) return false;

            if (!CurrentPositionNodeId.HasValue)
            {
                return StartNodeIds.Contains(id);
            }

            // 오직 "현재 서 있는 노드"에서 바로 이어지는 다음 노드들만 진행 가능하다 —
            // 예전엔 "이전에 완료된 노드가 하나라도 있으면" 조건이라 같은 층의 다른 갈림길(선택하지 않은 형제 노드)이
            // 영원히 열려 있는 채로 남아 다른 층으로 넘어간 뒤에도 옆/뒤로 이동할 수 있는 버그가 있었다.
            return Nodes[CurrentPositionNodeId.Value].NextIds.Contains(id);
        }
    }
}
