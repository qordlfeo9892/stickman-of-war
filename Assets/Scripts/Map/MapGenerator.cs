using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StickmanOfWar.Map
{
    public static class MapGenerator
    {
        public const int FloorCount = 7;
        public const int ColumnCount = 4;
        private const int BossFloor = FloorCount - 1;
        private const int PreBossFloor = FloorCount - 2;

        private const int MinPaths = 5;
        private const int MaxPaths = 8;

        private const int MaxElites = 4;
        private const int MaxShops = 2;
        private const int MaxTaverns = 2;

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

            int pathCount = Random.Range(MinPaths, MaxPaths + 1);
            for (int p = 0; p < pathCount; p++)
            {
                int column = Random.Range(0, ColumnCount);
                MapNode prev = GetOrCreateNode(0, column);
                for (int floor = 1; floor <= PreBossFloor; floor++)
                {
                    int step = Random.Range(-1, 2);
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

            graph.StartNodeIds = graph.Nodes.Values.Where(n => n.Floor == 0).Select(n => n.Id).ToList();

            AssignNodeTypes(graph);

            BossDefinition[] candidates = BossDatabase.GetCandidates(act);
            BossDefinition chosen = candidates[Random.Range(0, candidates.Length)];
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
            int eliteCount = 0, shopCount = 0, tavernCount = 0;

            for (int floor = 0; floor <= PreBossFloor; floor++)
            {
                List<MapNode> floorNodes = graph.Nodes.Values.Where(n => n.Floor == floor).OrderBy(n => n.Column).ToList();

                foreach (MapNode node in floorNodes)
                {
                    if (floor == 0)
                    {
                        node.Type = NodeType.Battle;
                        continue;
                    }
                    if (floor == PreBossFloor)
                    {
                        node.Type = NodeType.Campfire;
                        continue;
                    }

                    bool parentIsCampfire = node.PrevIds.Any(id => graph.Nodes[id].Type == NodeType.Campfire);
                    bool allowElite = floor >= 2 && eliteCount < MaxElites;
                    bool allowCampfire = floor >= 2 && floor != PreBossFloor - 1 && !parentIsCampfire;

                    var options = new List<(NodeType type, int weight)>
                    {
                        (NodeType.Battle, 45),
                        (NodeType.Event, 28),
                    };
                    if (allowElite) options.Add((NodeType.Elite, 15));
                    if (shopCount < MaxShops) options.Add((NodeType.Shop, 6));
                    if (tavernCount < MaxTaverns) options.Add((NodeType.Tavern, 6));
                    if (allowCampfire) options.Add((NodeType.Campfire, 8));

                    NodeType picked = PickWeighted(options);
                    node.Type = picked;

                    switch (picked)
                    {
                        case NodeType.Elite: eliteCount++; break;
                        case NodeType.Shop: shopCount++; break;
                        case NodeType.Tavern: tavernCount++; break;
                    }
                }
            }
        }

        private static NodeType PickWeighted(List<(NodeType type, int weight)> options)
        {
            int total = options.Sum(o => o.weight);
            int roll = Random.Range(0, total);
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
