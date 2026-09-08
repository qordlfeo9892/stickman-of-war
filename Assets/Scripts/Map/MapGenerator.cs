using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StickmanOfWar.Map
{
    public static class MapGenerator
    {
        public const int FloorCount = 12;
        public const int ColumnCount = 4;
        private const int CastleFloor = 0;
        private const int FirstPathFloor = 1;
        private const int BossFloor = FloorCount - 1;
        private const int PreBossFloor = FloorCount - 2;

        private const int MinPaths = 5;
        private const int MaxPaths = 8;

        public static MapGraph Generate(int act)
        {
            var graph = new MapGraph { Act = act };
            var nodeLookup = new Dictionary<(int floor, int column), MapNode>();
            int nextId = 0;

            MapNode GetOrCreateNode(int floor, int column)
            {
                var key = (floor, column);
                if (nodeLookup.TryGetValue(key, out MapNode existing))
                {
                    return existing;
                }
                var node = new MapNode { Id = nextId++, Floor = floor, Column = column };
                nodeLookup[key] = node;
                graph.Nodes[node.Id] = node;
                return node;
            }

            int pathCount = UnityEngine.Random.Range(MinPaths, MaxPaths + 1);
            for (int p = 0; p < pathCount; p++)
            {
                int column = UnityEngine.Random.Range(0, ColumnCount);
                MapNode prev = GetOrCreateNode(FirstPathFloor, column);
                for (int floor = FirstPathFloor + 1; floor <= PreBossFloor; floor++)
                {
                    int step = UnityEngine.Random.Range(-1, 2);
                    column = Mathf.Clamp(column + step, 0, ColumnCount - 1);
                    MapNode current = GetOrCreateNode(floor, column);
                    Connect(prev, current);
                    prev = current;
                }
            }

            MapNode boss = GetOrCreateNode(BossFloor, 0);
            boss.Type = NodeType.Boss;
            graph.BossNodeId = boss.Id;

            foreach (MapNode node in graph.Nodes.Values.Where(n => n.Floor == PreBossFloor).ToList())
            {
                Connect(node, boss);
            }

            MapNode castle = GetOrCreateNode(CastleFloor, 0);
            castle.Type = NodeType.Castle;
            graph.StartNodeIds = new List<int> { castle.Id };

            foreach (MapNode node in graph.Nodes.Values.Where(n => n.Floor == FirstPathFloor).ToList())
            {
                Connect(castle, node);
            }

            AssignNodeTypes(graph);

            BossDefinition[] candidates = BossDatabase.GetCandidates(act);
            BossDefinition chosen = candidates[UnityEngine.Random.Range(0, candidates.Length)];
            graph.ChosenBossId = chosen.Id;
            boss.BossId = chosen.Id;

            return graph;
        }

        private static void Connect(MapNode from, MapNode to)
        {
            if (!from.NextIds.Contains(to.Id)) from.NextIds.Add(to.Id);
            if (!to.PrevIds.Contains(from.Id)) to.PrevIds.Add(from.Id);
        }

        private static void AssignNodeTypes(MapGraph graph)
        {
            int targetElites = UnityEngine.Random.Range(1, 3);
            int targetShops = UnityEngine.Random.Range(1, 3);
            int targetTaverns = UnityEngine.Random.Range(1, 3);
            int targetExtraCampfires = UnityEngine.Random.Range(0, 3);
            int targetEvents = UnityEngine.Random.Range(2, 5);

            int eliteCount = 0, shopCount = 0, tavernCount = 0, campfireCount = 0, eventCount = 0;

            for (int floor = FirstPathFloor; floor <= PreBossFloor; floor++)
            {
                List<MapNode> floorNodes = graph.Nodes.Values.Where(n => n.Floor == floor).OrderBy(n => n.Column).ToList();

                foreach (MapNode node in floorNodes)
                {
                    if (floor == FirstPathFloor)
                    {
                        node.Type = NodeType.Battle;
                        continue;
                    }

                    // 보스 직전 층은 예산과 무관하게 무조건 마을이다 — 어느 경로로 오든(컬럼이 몇 개로 갈라지든)
                    // 반드시 보스 직전에 마을을 한 번은 거치도록 보장하기 위함. 이전에는 이 강제 배치가
                    // 마을 총량 캡에 걸려 건너뛰어질 수 있었는데, 그러면 플레이어가 실제로 밟는 단일 경로에는
                    // 마을이 한 번도 안 나올 수 있었다(캡을 다른 층에서 이미 소진했을 경우).
                    if (floor == PreBossFloor)
                    {
                        node.Type = NodeType.Campfire;
                        campfireCount++;
                        continue;
                    }

                    bool parentIsCampfire = node.PrevIds.Any(id => graph.Nodes[id].Type == NodeType.Campfire);
                    bool allowElite = floor >= FirstPathFloor + 2 && eliteCount < targetElites;
                    bool allowShop = shopCount < targetShops;
                    bool allowTavern = tavernCount < targetTaverns;
                    bool allowEvent = eventCount < targetEvents;
                    bool allowCampfire = floor >= FirstPathFloor + 2 && !parentIsCampfire && (campfireCount - 1) < targetExtraCampfires;

                    var options = new List<(NodeType type, int weight)>
                    {
                        (NodeType.Battle, 45),
                    };
                    if (allowEvent) options.Add((NodeType.Event, 28));
                    if (allowElite) options.Add((NodeType.Elite, 15));
                    if (allowShop) options.Add((NodeType.Shop, 6));
                    if (allowTavern) options.Add((NodeType.Tavern, 6));
                    if (allowCampfire) options.Add((NodeType.Campfire, 8));

                    NodeType picked = PickWeighted(options);
                    node.Type = picked;

                    switch (picked)
                    {
                        case NodeType.Elite: eliteCount++; break;
                        case NodeType.Shop: shopCount++; break;
                        case NodeType.Tavern: tavernCount++; break;
                        case NodeType.Campfire: campfireCount++; break;
                        case NodeType.Event: eventCount++; break;
                    }
                }
            }

            // 가중치 뽑기가 목표 최소치를 못 채웠을 수 있으므로, 남는 Battle 노드를 부족한 만큼 강제 전환한다.
            // 마을은 보스 직전 층에서 이미 보장되므로 여기서 다시 채우지 않는다.
            TopUp(graph, NodeType.Elite, targetElites, eliteCount, n => n.Floor >= FirstPathFloor + 2);
            TopUp(graph, NodeType.Shop, targetShops, shopCount, n => n.Floor >= FirstPathFloor + 1);
            TopUp(graph, NodeType.Tavern, targetTaverns, tavernCount, n => n.Floor >= FirstPathFloor + 1);
            TopUp(graph, NodeType.Event, targetEvents, eventCount, n => n.Floor >= FirstPathFloor + 1);
        }

        private static void TopUp(MapGraph graph, NodeType type, int target, int currentCount, Func<MapNode, bool> eligible)
        {
            int needed = target - currentCount;
            if (needed <= 0) return;

            List<MapNode> candidates = graph.Nodes.Values
                .Where(n => n.Type == NodeType.Battle && eligible(n))
                .OrderBy(_ => UnityEngine.Random.value)
                .ToList();

            foreach (MapNode node in candidates)
            {
                if (needed <= 0) break;
                node.Type = type;
                needed--;
            }
        }

        private static NodeType PickWeighted(List<(NodeType type, int weight)> options)
        {
            int total = options.Sum(o => o.weight);
            int roll = UnityEngine.Random.Range(0, total);
            int cumulative = 0;
            foreach ((NodeType type, int weight) in options)
            {
                cumulative += weight;
                if (roll < cumulative) return type;
            }
            return NodeType.Battle;
        }
    }
}
