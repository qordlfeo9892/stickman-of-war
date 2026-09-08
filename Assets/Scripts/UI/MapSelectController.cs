using System.Collections.Generic;
using StickmanOfWar.Map;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StickmanOfWar.UI
{
    public class MapSelectController : MonoBehaviour
    {
        private const string GameplaySceneName = "Gameplay";
        private const string MainMenuSceneName = "MainMenu";

        [SerializeField] private RectTransform content;
        [SerializeField] private RectTransform lineLayer;
        [SerializeField] private RectTransform nodeLayer;
        [SerializeField] private MapNodeView nodeButtonPrefab;
        [SerializeField] private RectTransform connectorLinePrefab;
        [SerializeField] private StubResultPanel stubResultPanel;
        [SerializeField] private CampfirePanel campfirePanel;
        [SerializeField] private CastleRecruitPanel castleRecruitPanel;
        [SerializeField] private TavernPanel tavernPanel;
        [SerializeField] private PartyPanel partyPanel;
        [SerializeField] private BagPanel bagPanel;
        [SerializeField] private RelicAcquirePanel relicAcquirePanel;
        [SerializeField] private ShopPanel shopPanel;
        [SerializeField] private EventPanel eventPanel;
        [SerializeField] private GameObject endingPanel;
        [SerializeField] private GameObject mapRoot;
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private Button partyButton;
        [SerializeField] private Button bagButton;

        [SerializeField] private float columnSpacing = 260f;
        [SerializeField] private float floorSpacing = 240f;
        [SerializeField] private float bottomPadding = 160f;

        [SerializeField] private float dashLength = 18f;
        [SerializeField] private float dashGap = 14f;
        [SerializeField] private float dashEndInset = 70f;

        // 슬더스의 "조우자(Neow)"처럼 시작 성 노드를 크게 강조한다.
        [SerializeField] private float castleNodeScale = 2.2f;
        // 커진 성에서 뻗어나가는 연결선이 성 안쪽에서 시작하지 않도록 추가로 밀어낼 거리.
        [SerializeField] private float castleConnectorInset = 90f;
        // 커진 성이 1층 노드와 겹치지 않도록 1층 이상을 위로 더 띄우는 간격.
        [SerializeField] private float castleExtraGap = 120f;

        [SerializeField] private Sprite battleIcon;
        [SerializeField] private Sprite eliteIcon;
        [SerializeField] private Sprite eventIcon;
        [SerializeField] private Sprite shopIcon;
        [SerializeField] private Sprite tavernIcon;
        [SerializeField] private Sprite villageIcon;
        [SerializeField] private Sprite bossIcon;
        [SerializeField] private Sprite castleIcon;

        private readonly List<GameObject> spawned = new List<GameObject>();

        private static readonly Dictionary<NodeType, Color> NodeColors = new Dictionary<NodeType, Color>
        {
            { NodeType.Battle, new Color(0.80f, 0.30f, 0.30f) },
            { NodeType.Elite, new Color(0.60f, 0.10f, 0.60f) },
            { NodeType.Event, new Color(0.90f, 0.80f, 0.20f) },
            { NodeType.Shop, new Color(0.20f, 0.60f, 0.90f) },
            { NodeType.Tavern, new Color(0.80f, 0.50f, 0.20f) },
            { NodeType.Campfire, new Color(0.95f, 0.50f, 0.10f) },
            { NodeType.Boss, new Color(0.75f, 0.05f, 0.05f) },
            { NodeType.Castle, new Color(0.85f, 0.72f, 0.25f) },
        };

        private static readonly Dictionary<NodeType, string> NodeLabels = new Dictionary<NodeType, string>
        {
            { NodeType.Battle, "전투" },
            { NodeType.Elite, "엘리트" },
            { NodeType.Event, "이벤트" },
            { NodeType.Shop, "상점" },
            { NodeType.Tavern, "선술집" },
            { NodeType.Campfire, "마을" },
            { NodeType.Boss, "보스" },
            { NodeType.Castle, "성" },
        };

        private void Start()
        {
            if (endingPanel != null) endingPanel.SetActive(false);
            if (backToMenuButton != null) backToMenuButton.onClick.AddListener(OnClickBackToMainMenu);
            if (partyButton != null) partyButton.onClick.AddListener(() => partyPanel.Show());
            if (bagButton != null) bagButton.onClick.AddListener(() => bagPanel.Show());

            SetupMapZoom();

            if (!RunState.HasActiveRun)
            {
                RunState.StartNewRun();
            }
            else if (RunState.IsBossNodeCompleted())
            {
                if (RunState.IsFinalActBossCleared())
                {
                    ShowEnding();
                    return;
                }
                RunState.AdvanceToNextAct();
            }

            RenderMap();
            ProcessPendingRelics();
        }

        // 마우스 휠 줌 컴포넌트를 스크롤 뷰포트에 런타임으로 붙인다 (씬 수정 없이).
        private void SetupMapZoom()
        {
            if (content == null) return;
            var viewport = content.parent as RectTransform;
            if (viewport == null) return;

            var zoom = viewport.GetComponent<MapZoomController>();
            if (zoom == null) zoom = viewport.gameObject.AddComponent<MapZoomController>();
            zoom.Init(content);
        }

        private void ProcessPendingRelics()
        {
            if (RunState.PendingRelicIds.Count == 0)
            {
                ProcessPendingMercOffer();
                return;
            }

            string relicId = RunState.PendingRelicIds[0];
            RelicDefinition def = RelicDatabase.GetById(relicId);
            if (def == null)
            {
                RunState.ResolvePendingRelic(relicId);
                ProcessPendingRelics();
                return;
            }

            relicAcquirePanel.Show(def, () =>
            {
                RunState.ResolvePendingRelic(relicId);
                ProcessPendingRelics();
            });
        }

        // 전투 후 용병 제안 — 유물 정산이 끝난 뒤 1회 표시.
        private void ProcessPendingMercOffer()
        {
            string mercId = RunState.PendingMercOfferId;
            if (string.IsNullOrEmpty(mercId)) return;

            MercenaryDefinition merc = MercenaryDatabase.GetById(mercId);
            if (merc == null)
            {
                RunState.ClearPendingMercOffer();
                return;
            }

            MercenaryOfferPopup.Show(this, merc, () =>
            {
                RunState.ClearPendingMercOffer();
                RenderMap(); // 정원/구성이 바뀌었을 수 있으니 갱신
            });
        }

        private void ShowEnding()
        {
            if (mapRoot != null) mapRoot.SetActive(false);
            if (endingPanel != null) endingPanel.SetActive(true);
            SaveSystem.DeleteSave();
        }

        private void OnClickBackToMainMenu()
        {
            SceneManager.LoadScene(MainMenuSceneName);
        }

        private void RenderMap()
        {
            foreach (GameObject go in spawned)
            {
                Destroy(go);
            }
            spawned.Clear();

            MapGraph graph = RunState.CurrentMap;
            var positions = new Dictionary<int, Vector2>();

            float columnsWidth = (MapGenerator.ColumnCount - 1) * columnSpacing;
            float centerOffsetX = (content.rect.width - columnsWidth) * 0.5f;

            foreach (MapNode node in graph.Nodes.Values)
            {
                float x = centerOffsetX + ((node.Floor == 0 || node.Floor == MapGenerator.FloorCount - 1)
                    ? columnsWidth * 0.5f
                    : node.Column * columnSpacing);
                float y = bottomPadding + node.Floor * floorSpacing;
                if (node.Floor >= 1) y += castleExtraGap; // 커진 성 위로 여유 공간 확보
                positions[node.Id] = new Vector2(x, y);
            }

            foreach (MapNode node in graph.Nodes.Values)
            {
                float fromInset = node.Type == NodeType.Castle ? dashEndInset + castleConnectorInset : dashEndInset;
                foreach (int nextId in node.NextIds)
                {
                    SpawnConnector(positions[node.Id], positions[nextId], fromInset);
                }
            }

            foreach (MapNode node in graph.Nodes.Values)
            {
                SpawnNode(node, positions[node.Id], graph);
            }

            float contentHeight = bottomPadding * 2f + (MapGenerator.FloorCount - 1) * floorSpacing + castleExtraGap;
            content.sizeDelta = new Vector2(content.sizeDelta.x, contentHeight);
        }

        private void SpawnConnector(Vector2 from, Vector2 to, float fromInset)
        {
            Vector2 diff = to - from;
            float distance = diff.magnitude;
            float usableDistance = distance - fromInset - dashEndInset;
            if (usableDistance <= 0f) return;

            Vector2 direction = diff / distance;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

            int dashCount = Mathf.Max(1, Mathf.FloorToInt(usableDistance / (dashLength + dashGap)));
            float totalDashSpan = dashCount * dashLength + (dashCount - 1) * dashGap;
            float startOffset = fromInset + (usableDistance - totalDashSpan) * 0.5f;

            for (int i = 0; i < dashCount; i++)
            {
                float t = startOffset + i * (dashLength + dashGap) + dashLength * 0.5f;
                Vector2 center = from + direction * t;

                RectTransform dash = Instantiate(connectorLinePrefab, lineLayer);
                dash.anchoredPosition = center;
                dash.sizeDelta = new Vector2(dashLength, dash.sizeDelta.y);
                dash.localRotation = Quaternion.Euler(0f, 0f, angle);
                spawned.Add(dash.gameObject);
            }
        }

        private Sprite GetIcon(NodeType type)
        {
            switch (type)
            {
                case NodeType.Battle: return battleIcon;
                case NodeType.Elite: return eliteIcon;
                case NodeType.Event: return eventIcon;
                case NodeType.Shop: return shopIcon;
                case NodeType.Tavern: return tavernIcon;
                case NodeType.Campfire: return villageIcon;
                case NodeType.Boss: return bossIcon;
                case NodeType.Castle: return castleIcon;
                default: return null;
            }
        }

        private void SpawnNode(MapNode node, Vector2 position, MapGraph graph)
        {
            MapNodeView view = Instantiate(nodeButtonPrefab, nodeLayer);
            RectTransform rect = (RectTransform)view.transform;
            rect.anchoredPosition = position;

            // 시작 성 노드는 슬더스 조우자처럼 크게 표시
            rect.localScale = node.Type == NodeType.Castle ? Vector3.one * castleNodeScale : Vector3.one;

            bool available = graph.IsNodeAvailable(node.Id);
            view.Setup(node.Id, NodeLabels[node.Type], GetIcon(node.Type), NodeColors[node.Type], available, node.IsCompleted, OnNodeClicked);
            spawned.Add(view.gameObject);
        }

        private void OnNodeClicked(int nodeId)
        {
            MapGraph graph = RunState.CurrentMap;
            if (!graph.IsNodeAvailable(nodeId)) return;

            MapNode node = graph.Nodes[nodeId];
            RunState.CurrentNodeId = nodeId;

            switch (node.Type)
            {
                case NodeType.Battle:
                case NodeType.Elite:
                case NodeType.Boss:
                    SceneManager.LoadScene(GameplaySceneName);
                    break;
                case NodeType.Campfire:
                    campfirePanel.Show(CompleteAndRefresh);
                    break;
                case NodeType.Event:
                    eventPanel.Show(CompleteAndRefresh);
                    break;
                case NodeType.Shop:
                    shopPanel.Show(CompleteAndRefresh);
                    break;
                case NodeType.Tavern:
                    tavernPanel.Show(CompleteAndRefresh);
                    break;
                case NodeType.Castle:
                    castleRecruitPanel.Show(CompleteAndRefresh);
                    break;
            }
        }

        private void CompleteAndRefresh()
        {
            RunState.CompleteCurrentNode();
            RenderMap();
        }
    }
}
