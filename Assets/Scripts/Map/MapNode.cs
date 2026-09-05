using System.Collections.Generic;

namespace StickmanOfWar.Map
{
    public class MapNode
    {
        public int Id;
        public int Floor;
        public int Column;
        public NodeType Type;
        public List<int> NextIds = new List<int>();
        public List<int> PrevIds = new List<int>();
        public bool IsCompleted;
        public string BossId;
    }
}
