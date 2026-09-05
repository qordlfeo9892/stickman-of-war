namespace StickmanOfWar.Map
{
    public static class RunState
    {
        public const int MaxPartySizeCap = 6;
        public const int MaxBagSizeCap = 20;

        public static int CurrentAct { get; private set; }
        public static MapGraph CurrentMap { get; private set; }
        public static int? CurrentNodeId { get; set; }

        public static int PartySizeCap { get; private set; } = 4;
        public static int PartySizeCurrent { get; set; } = 1;
        public static int BagSizeCap { get; private set; } = 10;
        public static int BagSizeCurrent { get; set; }

        public static bool HasActiveRun => CurrentMap != null;

        public static void StartNewRun()
        {
            CurrentAct = 1;
            PartySizeCap = 4;
            PartySizeCurrent = 1;
            BagSizeCap = 10;
            BagSizeCurrent = 0;
            CurrentNodeId = null;
            CurrentMap = MapGenerator.Generate(CurrentAct);
        }

        public static void CompleteCurrentNode()
        {
            if (CurrentNodeId.HasValue)
            {
                CompleteNode(CurrentNodeId.Value);
            }
            CurrentNodeId = null;
        }

        public static void CompleteNode(int nodeId)
        {
            if (CurrentMap != null && CurrentMap.Nodes.TryGetValue(nodeId, out MapNode node))
            {
                node.IsCompleted = true;
            }
        }

        public static bool IsBossNodeCompleted()
        {
            return CurrentMap != null && CurrentMap.Nodes[CurrentMap.BossNodeId].IsCompleted;
        }

        public static void AdvanceToNextAct()
        {
            CurrentAct++;
            CurrentNodeId = null;
            CurrentMap = MapGenerator.Generate(CurrentAct);
        }

        public static bool IsFinalActBossCleared()
        {
            return CurrentAct >= 3 && IsBossNodeCompleted();
        }

        public static void IncreasePartySizeCap()
        {
            if (PartySizeCap < MaxPartySizeCap) PartySizeCap++;
        }

        public static void IncreaseBagSizeCap()
        {
            if (BagSizeCap < MaxBagSizeCap) BagSizeCap++;
        }
    }
}
