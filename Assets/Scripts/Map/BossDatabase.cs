using System.Collections.Generic;

namespace StickmanOfWar.Map
{
    public static class BossDatabase
    {
        private static readonly Dictionary<int, BossDefinition[]> Candidates = new Dictionary<int, BossDefinition[]>
        {
            { 1, new[]
                {
                    new BossDefinition("act1_boss_a", "가시멧돼지 두목"),
                    new BossDefinition("act1_boss_b", "해골 궁수장"),
                    new BossDefinition("act1_boss_c", "늪지 거대괴수"),
                }
            },
            { 2, new[]
                {
                    new BossDefinition("act2_boss_a", "강철 골렘"),
                    new BossDefinition("act2_boss_b", "그림자 암살자"),
                    new BossDefinition("act2_boss_c", "불타는 야수"),
                }
            },
            { 3, new[]
                {
                    new BossDefinition("act3_boss_a", "타락한 황제"),
                    new BossDefinition("act3_boss_b", "심연의 군주"),
                    new BossDefinition("act3_boss_c", "종말의 파수꾼"),
                }
            },
        };

        public static BossDefinition[] GetCandidates(int act)
        {
            return Candidates[act];
        }

        public static BossDefinition GetById(int act, string id)
        {
            foreach (BossDefinition boss in Candidates[act])
            {
                if (boss.Id == id) return boss;
            }
            return null;
        }
    }
}
