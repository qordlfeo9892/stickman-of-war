using System;
using System.Collections.Generic;
using System.Linq;
using StickmanOfWar.Map;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StickmanOfWar.UI
{
    public class BagPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform gridContainer;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private Button rotateButton;
        [SerializeField] private Button discardButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private ConfirmDialog confirmDialog;

        private const float CellSize = 80f;
        private const float CellGap = 6f;
        private const float Pitch = CellSize + CellGap;

        private static readonly Color EmptyCellColor = new Color(0.08f, 0.08f, 0.10f, 0.9f);
        private static readonly Color ValidHighlight = new Color(0.2f, 0.85f, 0.3f, 0.55f);
        private static readonly Color InvalidHighlight = new Color(0.9f, 0.2f, 0.2f, 0.55f);

        private readonly List<GameObject> spawnedObjects = new List<GameObject>();
        private Image[,] highlightCells;

        private RelicDefinition heldDef;
        private int heldRotation;
        private bool isNewRelicMode;
        private Action<bool> onResolvedNew;
        private bool inputLocked;

        private void Awake()
        {
            rotateButton.onClick.AddListener(HandleRotate);
            discardButton.onClick.AddListener(HandleDiscard);
            closeButton.onClick.AddListener(HandleClose);
        }

        public void Show()
        {
            heldDef = null;
            isNewRelicMode = false;
            inputLocked = false;
            gameObject.SetActive(true);
            RefreshGrid();
            RefreshButtons();
        }

        public void ShowForNewRelic(RelicDefinition def, Action<bool> onResolved)
        {
            heldDef = def;
            heldRotation = 0;
            isNewRelicMode = true;
            onResolvedNew = onResolved;
            inputLocked = false;
            gameObject.SetActive(true);
            RefreshGrid();
            RefreshButtons();
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy) return;
            if (Mouse.current == null) return;
            if (inputLocked) return;

            Vector2 screenPos = Mouse.current.position.ReadValue();
            int hoverX = 0, hoverY = 0;
            bool overGrid = RectTransformUtility.ScreenPointToLocalPointInRectangle(gridContainer, screenPos, null, out Vector2 local)
                && TryLocalToCell(local, out hoverX, out hoverY);

            UpdateHighlightAndInfo(overGrid, new Vector2Int(hoverX, hoverY));

            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            if (overGrid)
            {
                if (heldDef != null) TryPlaceAt(hoverX, hoverY);
                else TryPickUpAt(hoverX, hoverY);
                return;
            }

            if (heldDef != null && !IsOverButton(rotateButton, screenPos) && !IsOverButton(discardButton, screenPos) && !IsOverButton(closeButton, screenPos))
            {
                ConfirmDropOutside();
            }
        }

        private static bool IsOverButton(Button btn, Vector2 screenPos)
        {
            if (btn == null || !btn.gameObject.activeInHierarchy) return false;
            return RectTransformUtility.RectangleContainsScreenPoint((RectTransform)btn.transform, screenPos, null);
        }

        private void ConfirmDropOutside()
        {
            inputLocked = true;
            confirmDialog.Show("가방 밖에 정말 버리시겠습니까?",
                onYes: () =>
                {
                    inputLocked = false;
                    HandleDiscard();
                },
                onNo: () => inputLocked = false);
        }

        private bool TryLocalToCell(Vector2 local, out int cx, out int cy)
        {
            float totalWidth = RunState.Bag.Width * CellSize + (RunState.Bag.Width - 1) * CellGap;
            float totalHeight = RunState.Bag.Height * CellSize + (RunState.Bag.Height - 1) * CellGap;
            float leftEdge = -totalWidth / 2f;
            float bottomEdge = -totalHeight / 2f;

            cx = Mathf.FloorToInt((local.x - leftEdge) / Pitch);
            cy = Mathf.FloorToInt((local.y - bottomEdge) / Pitch);
            return cx >= 0 && cx < RunState.Bag.Width && cy >= 0 && cy < RunState.Bag.Height;
        }

        private void ClearHighlights()
        {
            if (highlightCells == null) return;
            foreach (Image img in highlightCells)
            {
                if (img != null) img.color = Color.clear;
            }
        }

        private void UpdateHighlightAndInfo(bool overGrid, Vector2Int hover)
        {
            ClearHighlights();

            if (heldDef != null)
            {
                if (infoText != null) infoText.text = $"{heldDef.DisplayName} - {heldDef.EffectDescription}";
                if (!overGrid) return;

                bool valid = RunState.Bag.CanPlace(heldDef, hover.x, hover.y, heldRotation);
                Color color = valid ? ValidHighlight : InvalidHighlight;
                foreach (Vector2Int cell in RunState.Bag.GetCells(heldDef, hover.x, hover.y, heldRotation))
                {
                    if (cell.x >= 0 && cell.x < RunState.Bag.Width && cell.y >= 0 && cell.y < RunState.Bag.Height)
                    {
                        highlightCells[cell.x, cell.y].color = color;
                    }
                }
                return;
            }

            if (!overGrid)
            {
                if (infoText != null) infoText.text = "";
                return;
            }

            PlacedRelic under = FindPlacedAt(hover.x, hover.y);
            if (infoText != null)
            {
                if (under != null)
                {
                    RelicDefinition def = RelicDatabase.GetById(under.RelicId);
                    infoText.text = $"{def.DisplayName} - {def.EffectDescription}";
                }
                else
                {
                    infoText.text = "";
                }
            }
        }

        private PlacedRelic FindPlacedAt(int x, int y)
        {
            foreach (PlacedRelic p in RunState.Bag.Placed)
            {
                RelicDefinition def = RelicDatabase.GetById(p.RelicId);
                if (RunState.Bag.GetCells(def, p.X, p.Y, p.Rotation).Contains(new Vector2Int(x, y))) return p;
            }
            return null;
        }

        private void TryPickUpAt(int x, int y)
        {
            PlacedRelic found = FindPlacedAt(x, y);
            if (found == null) return;

            RunState.Bag.Remove(found.InstanceId);
            heldDef = RelicDatabase.GetById(found.RelicId);
            heldRotation = found.Rotation;
            isNewRelicMode = false;
            RefreshGrid();
            RefreshButtons();
        }

        private void TryPlaceAt(int x, int y)
        {
            if (!RunState.Bag.CanPlace(heldDef, x, y, heldRotation)) return;

            RunState.Bag.Place(heldDef, x, y, heldRotation);
            heldDef = null;
            RefreshGrid();
            RefreshButtons();
            SaveSystem.Save();

            if (isNewRelicMode)
            {
                isNewRelicMode = false;
                onResolvedNew?.Invoke(true);
            }
        }

        private void HandleRotate()
        {
            if (heldDef == null) return;
            heldRotation = (heldRotation + 1) % 4;
        }

        private void HandleDiscard()
        {
            if (heldDef == null) return;
            heldDef = null;
            RefreshGrid();
            RefreshButtons();
            SaveSystem.Save();

            if (isNewRelicMode)
            {
                isNewRelicMode = false;
                onResolvedNew?.Invoke(false);
            }
        }

        private void HandleClose()
        {
            if (heldDef != null) return;
            gameObject.SetActive(false);
        }

        private void RefreshButtons()
        {
            rotateButton.gameObject.SetActive(heldDef != null);
            discardButton.gameObject.SetActive(heldDef != null);
            closeButton.gameObject.SetActive(heldDef == null);
        }

        private void RefreshGrid()
        {
            foreach (GameObject go in spawnedObjects) Destroy(go);
            spawnedObjects.Clear();

            int width = RunState.Bag.Width;
            int height = RunState.Bag.Height;
            highlightCells = new Image[width, height];

            float totalWidth = width * CellSize + (width - 1) * CellGap;
            float totalHeight = height * CellSize + (height - 1) * CellGap;
            float leftEdge = -totalWidth / 2f;
            float bottomEdge = -totalHeight / 2f;

            Func<int, int, Vector2> cellPos = (cx, cy) => new Vector2(
                leftEdge + cx * Pitch + CellSize / 2f,
                bottomEdge + cy * Pitch + CellSize / 2f);

            // base empty cells
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GameObject cell = new GameObject($"Cell_{x}_{y}", typeof(RectTransform));
                    var rt = (RectTransform)cell.transform;
                    rt.SetParent(gridContainer, false);
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(CellSize, CellSize);
                    rt.anchoredPosition = cellPos(x, y);
                    Image img = cell.AddComponent<Image>();
                    img.color = EmptyCellColor;
                    spawnedObjects.Add(cell);
                }
            }

            // placed relics
            foreach (PlacedRelic placed in RunState.Bag.Placed)
            {
                RelicDefinition def = RelicDatabase.GetById(placed.RelicId);
                if (def == null) continue;
                List<Vector2Int> cells = RunState.Bag.GetCells(def, placed.X, placed.Y, placed.Rotation);

                bool labelPlaced = false;
                foreach (Vector2Int cell in cells)
                {
                    if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height) continue;

                    GameObject go = new GameObject($"Relic_{placed.InstanceId}_{cell.x}_{cell.y}", typeof(RectTransform));
                    var rt = (RectTransform)go.transform;
                    rt.SetParent(gridContainer, false);
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(CellSize - 4f, CellSize - 4f);
                    rt.anchoredPosition = cellPos(cell.x, cell.y);
                    Image img = go.AddComponent<Image>();
                    img.color = def.IconColor;
                    spawnedObjects.Add(go);

                    if (!labelPlaced)
                    {
                        var labelGO = new GameObject("Label", typeof(RectTransform));
                        var labelRT = (RectTransform)labelGO.transform;
                        labelRT.SetParent(rt, false);
                        labelRT.anchorMin = Vector2.zero;
                        labelRT.anchorMax = Vector2.one;
                        labelRT.offsetMin = Vector2.zero;
                        labelRT.offsetMax = Vector2.zero;
                        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
                        tmp.text = def.DisplayName.Length > 4 ? def.DisplayName.Substring(0, 4) : def.DisplayName;
                        tmp.alignment = TextAlignmentOptions.Center;
                        tmp.enableAutoSizing = true;
                        tmp.fontSizeMin = 8f;
                        tmp.fontSizeMax = 18f;
                        tmp.color = Color.white;
                        labelPlaced = true;
                    }
                }
            }

            // highlight overlay (topmost, transparent by default)
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GameObject go = new GameObject($"Highlight_{x}_{y}", typeof(RectTransform));
                    var rt = (RectTransform)go.transform;
                    rt.SetParent(gridContainer, false);
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(CellSize, CellSize);
                    rt.anchoredPosition = cellPos(x, y);
                    Image img = go.AddComponent<Image>();
                    img.color = Color.clear;
                    img.raycastTarget = false;
                    highlightCells[x, y] = img;
                    spawnedObjects.Add(go);
                }
            }
        }
    }
}
