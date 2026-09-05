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
        [SerializeField] private GameObject endingPanel;
        [SerializeField] private GameObject mapRoot;
        [SerializeField] private Button backToMenuButton;

        [SerializeField] private float columnSpacing = 260f;
        [SerializeField] private float floorSpacing = 240f;
        [SerializeField] private float bottomPadding = 160f;

        [SerializeField] private float dashLength = 18f;
        [SerializeField] private float dashGap = 14f;
        [SerializeField] private float dashEndInset = 70f;

        [SerializeField] private Sprite battleIcon;
        [SerializeField] private Sprite eliteIcon;
        [SerializeField] private Sprite eventIcon;
        [SerializeField] private Sprite shopIcon;
        [SerializeField] private Sprite tavernIcon;
        [SerializeField] private Sprite villageIcon;
        [SerializeField] private Sprite bossIcon;

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
        };

        private void Start()
        {
            if (endingPanel != null) endingPanel.SetActive(false);
            if (backToMenuButton != null) backToMenuButton.onClick.AddListener(OnClickBackToMainMenu);

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
        }

        private void ShowEnding()
        {
            if (mapRoot != null) mapRoot.SetActive(false);
            if (endingPanel != null) endingPanel.SetActive(true);
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
                float x = centerOffsetX + ((node.Floor == MapGenerator.FloorCount - 1)
                    ? columnsWidth * 0.5f
                    : node.Column * columnSpacing);
                float y = bottomPadding + node.Floor * floorSpacing;
                positions[node.Id] = new Vector2(x, y);
            }

            foreach (MapNode node in graph.Nodes.Values)
            {
                foreach (int nextId in node.NextIds)
                {
                    SpawnConnector(positions[node.Id], positions[nextId]);
                }
            }

            foreach (MapNode node in graph.Nodes.Values)
            {
                SpawnNode(node, positions[node.Id], graph);
            }

            float contentHeight = bottomPadding * 2f + (MapGenerator.FloorCount - 1) * floorSpacing;
            content.sizeDelta = new Vector2(content.sizeDelta.x, contentHeight);
        }

        private void SpawnConnector(Vector2 from, Vector2 to)
        {
            Vector2 diff = to - from;
            float distance = diff.magnitude;
            float usableDistance = distance - dashEndInset * 2f;
            if (usableDistance <= 0f) return;

            Vector2 direction = diff / distance;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

            int dashCount = Mathf.Max(1, Mathf.FloorToInt(usableDistance / (dashLength + dashGap)));
            float totalDashSpan = dashCount * dashLength + (dashCount - 1) * dashGap;
            float startOffset = dashEndInset + (usableDistance - totalDashSpan) * 0.5f;

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
                default: return null;
            }
        }

        private void SpawnNode(MapNode node, Vector2 position, MapGraph graph)
        {
            MapNodeView view = Instantiate(nodeButtonPrefab, nodeLayer);
            RectTransform rect = (RectTransform)view.transform;
            rect.anchoredPosition = position;

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
                    stubResultPanel.Show("이벤트", "알 수 없는 사건이 발생했다. (구현 예정)", CompleteAndRefresh);
                    break;
                case NodeType.Shop:
                    stubResultPanel.Show("상점", "상인이 물건을 늘어놓았다. (구현 예정)", CompleteAndRefresh);
                    break;
                case NodeType.Tavern:
                    stubResultPanel.Show("선술집", "새로운 용병을 고용할 수 있다. (구현 예정)", CompleteAndRefresh);
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
