using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace StickmanOfWar.Map
{
    public static class SaveSystem
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        [Serializable]
        private class SaveNodeData
        {
            public int id;
            public int floor;
            public int column;
            public int type;
            public List<int> nextIds;
            public List<int> prevIds;
            public bool isCompleted;
            public string bossId;
        }

        [Serializable]
        private class SaveRosterMember
        {
            public string id;
            public int star;
        }

        [Serializable]
        private class SavePlacedRelic
        {
            public string instanceId;
            public string relicId;
            public int x;
            public int y;
            public int rotation;
        }

        [Serializable]
        private class SaveData
        {
            public int act;
            public List<SaveNodeData> nodes;
            public List<int> startNodeIds;
            public int bossNodeId;
            public string chosenBossId;
            public bool hasCurrentPosition;
            public int currentPositionNodeId;
            public List<string> rosterIds; // 구버전 세이브 호환용 (별 정보 없이 id만) — 새 세이브는 rosterMembers 사용
            public List<SaveRosterMember> rosterMembers;
            public int dice;
            public int gold;
            public int partySizeCap;
            public int bagWidth;
            public int bagHeight;
            public List<SavePlacedRelic> placedRelics;
            public List<string> pendingRelicIds;
            public float nextBattleBaseDamage;
            public string pendingMercOfferId;
        }

        public static bool HasSave()
        {
            return File.Exists(SavePath);
        }

        public static void Save()
        {
            if (RunState.CurrentMap == null) return;

            MapGraph graph = RunState.CurrentMap;
            var data = new SaveData
            {
                act = RunState.CurrentAct,
                nodes = graph.Nodes.Values.Select(n => new SaveNodeData
                {
                    id = n.Id,
                    floor = n.Floor,
                    column = n.Column,
                    type = (int)n.Type,
                    nextIds = n.NextIds,
                    prevIds = n.PrevIds,
                    isCompleted = n.IsCompleted,
                    bossId = n.BossId,
                }).ToList(),
                startNodeIds = graph.StartNodeIds,
                bossNodeId = graph.BossNodeId,
                chosenBossId = graph.ChosenBossId,
                hasCurrentPosition = graph.CurrentPositionNodeId.HasValue,
                currentPositionNodeId = graph.CurrentPositionNodeId ?? -1,
                rosterMembers = RunState.Roster.Select(m => new SaveRosterMember { id = m.Definition.Id, star = m.Star }).ToList(),
                dice = RunState.DiceCount,
                gold = RunState.Gold,
                partySizeCap = RunState.PartySizeCap,
                bagWidth = RunState.Bag.Width,
                bagHeight = RunState.Bag.Height,
                placedRelics = RunState.Bag.Placed.Select(p => new SavePlacedRelic
                {
                    instanceId = p.InstanceId,
                    relicId = p.RelicId,
                    x = p.X,
                    y = p.Y,
                    rotation = p.Rotation,
                }).ToList(),
                pendingRelicIds = RunState.PendingRelicIds,
                nextBattleBaseDamage = RunState.NextBattleBaseDamage,
                pendingMercOfferId = RunState.PendingMercOfferId,
            };

            File.WriteAllText(SavePath, JsonUtility.ToJson(data));
        }

        public static bool Load()
        {
            if (!HasSave()) return false;

            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));

            var graph = new MapGraph
            {
                Act = data.act,
                StartNodeIds = data.startNodeIds,
                BossNodeId = data.bossNodeId,
                ChosenBossId = data.chosenBossId,
                CurrentPositionNodeId = data.hasCurrentPosition ? data.currentPositionNodeId : (int?)null,
            };
            foreach (SaveNodeData saved in data.nodes)
            {
                graph.Nodes[saved.id] = new MapNode
                {
                    Id = saved.id,
                    Floor = saved.floor,
                    Column = saved.column,
                    Type = (NodeType)saved.type,
                    NextIds = saved.nextIds,
                    PrevIds = saved.prevIds,
                    IsCompleted = saved.isCompleted,
                    BossId = saved.bossId,
                };
            }

            // 새 포맷(rosterMembers)이 있으면 그걸 쓰고, 구버전 세이브(rosterIds만 존재)는 전부 1성으로 불러온다.
            List<RosterMember> roster;
            if (data.rosterMembers != null)
            {
                roster = new List<RosterMember>();
                foreach (SaveRosterMember r in data.rosterMembers)
                {
                    MercenaryDefinition def = MercenaryDatabase.GetById(r.id);
                    if (def != null) roster.Add(new RosterMember(def, r.star));
                }
            }
            else
            {
                roster = (data.rosterIds ?? new List<string>())
                    .Select(MercenaryDatabase.GetById)
                    .Where(m => m != null)
                    .Select(def => new RosterMember(def))
                    .ToList();
            }

            var bag = new RelicBag
            {
                Width = data.bagWidth,
                Height = data.bagHeight,
                Placed = data.placedRelics.Select(p => new PlacedRelic
                {
                    InstanceId = p.instanceId,
                    RelicId = p.relicId,
                    X = p.x,
                    Y = p.y,
                    Rotation = p.rotation,
                }).ToList(),
            };

            RunState.LoadState(data.act, graph, roster, data.dice, data.gold,
                data.partySizeCap, bag, data.pendingRelicIds, data.nextBattleBaseDamage);
            RunState.PendingMercOfferId = data.pendingMercOfferId;
            return true;
        }

        public static void DeleteSave()
        {
            if (HasSave()) File.Delete(SavePath);
        }
    }
}
