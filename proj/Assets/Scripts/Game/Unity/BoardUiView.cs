using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AI4GamesFinalProj.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BoardUiView : MonoBehaviour
    {
        private enum PreviewFamily
        {
            SingleRestore = 0,
            SingleDefense = 1,
            SingleGrowth = 2,
            SingleDestruction = 3,
            AreaRestore = 4,
            AreaFreeze = 5
        }

        private readonly struct PreviewProfile
        {
            public PreviewProfile(PreviewFamily family, int radius)
            {
                Family = family;
                Radius = radius;
            }

            public PreviewFamily Family { get; }

            public int Radius { get; }
        }

        private static Sprite overlaySprite;

        [SerializeField]
        private AttemptController attemptController;

        [SerializeField]
        private Camera targetCamera;

        [SerializeField]
        private Transform overlayParent;

        [SerializeField]
        private float overlayDepthOffset = -0.15f;

        [SerializeField]
        [Range(0.1f, 1f)]
        private float overlayScaleMultiplier = 0.92f;

        [SerializeField]
        private int sortingOrder = 50;

        [SerializeField]
        private Color hoverColor = new Color(0.2f, 0.55f, 1f, 0.45f);

        [SerializeField]
        private Color invalidTargetColor = new Color(1f, 0.15f, 0.15f, 0.5f);

        private readonly Dictionary<Vector2Int, SpriteRenderer> overlayRenderers = new Dictionary<Vector2Int, SpriteRenderer>();
        private readonly Dictionary<string, PreviewProfile> previewProfiles = new Dictionary<string, PreviewProfile>(StringComparer.OrdinalIgnoreCase);

        private SquareGameBoard currentBoard;
        private Vector2Int? hoveredCoords;

        private void Awake()
        {
            if (attemptController == null)
            {
                attemptController = GetComponent<AttemptController>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            BuildPreviewProfiles();
            EnsureOverlayParent();
        }

        private void OnEnable()
        {
            if (attemptController == null)
            {
                return;
            }

            attemptController.AttemptStarted += HandleAttemptStarted;
            attemptController.AwaitingPlayerInput += HandleAttemptStateChanged;
            attemptController.TurnResolved += HandleTurnResolved;
            attemptController.AttemptEnded += HandleAttemptEnded;
            attemptController.PreviewActionChanged += HandlePreviewActionChanged;

            if (attemptController.CurrentAttempt != null)
            {
                BuildOverlay(attemptController.CurrentAttempt);
            }
        }

        private void OnDisable()
        {
            if (attemptController == null)
            {
                return;
            }

            attemptController.AttemptStarted -= HandleAttemptStarted;
            attemptController.AwaitingPlayerInput -= HandleAttemptStateChanged;
            attemptController.TurnResolved -= HandleTurnResolved;
            attemptController.AttemptEnded -= HandleAttemptEnded;
            attemptController.PreviewActionChanged -= HandlePreviewActionChanged;
        }

        private void Update()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (attemptController == null || currentBoard == null || targetCamera == null)
            {
                HideAll();
                return;
            }

            if (!attemptController.HasActiveAttempt || !attemptController.IsWaitingForPlayerInput)
            {
                hoveredCoords = null;
                HideAll();
                return;
            }

            hoveredCoords = TryGetHoveredCoords(out Vector2Int coords) ? coords : (Vector2Int?)null;
            DrawPreview();
            HandleBoardInput();
        }

        private void BuildOverlay(Attempt attempt)
        {
            if (attempt?.Board is not SquareGameBoard board)
            {
                currentBoard = null;
                HideAll();
                return;
            }

            currentBoard = board;
            EnsureOverlayParent();
            ClearOverlay();

            foreach (BoardCell cell in board.AllCells)
            {
                Vector2Int key = new Vector2Int(cell.X, cell.Y);
                overlayRenderers[key] = CreateOverlayRenderer(cell);
            }

            HideAll();
        }

        private void HandleBoardInput()
        {
            if (!attemptController.HasSelectedPreviewAction || !hoveredCoords.HasValue)
            {
                if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                {
                    attemptController.ClearPreviewAction();
                }

                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    attemptController.ClearPreviewAction();
                }

                return;
            }

            Vector3 targetCoords = new Vector3(hoveredCoords.Value.x, hoveredCoords.Value.y, 0f);
            bool isValidTarget = IsTargetValid(attemptController.SelectedPreviewActionId, targetCoords);

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && isValidTarget)
            {
                attemptController.TrySubmitSelectedPreviewToBoard(targetCoords);
            }

            if ((Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) ||
                (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
            {
                attemptController.ClearPreviewAction();
            }
        }

        private void DrawPreview()
        {
            HideAll();
            if (!hoveredCoords.HasValue || !overlayRenderers.TryGetValue(hoveredCoords.Value, out SpriteRenderer hoveredRenderer))
            {
                return;
            }

            if (!attemptController.HasSelectedPreviewAction)
            {
                hoveredRenderer.color = hoverColor;
                return;
            }

            string actionId = attemptController.SelectedPreviewActionId;

            PreviewProfile previewProfile = GetPreviewProfile(actionId);
            Vector3 targetCoords = new Vector3(hoveredCoords.Value.x, hoveredCoords.Value.y, 0f);
            bool isValidTarget = IsTargetValid(actionId, targetCoords);
            Color previewColor = isValidTarget ? GetPreviewColor(previewProfile.Family) : invalidTargetColor;

            BoardCell centerCell = currentBoard.GetCell(hoveredCoords.Value.x, hoveredCoords.Value.y);
            if (centerCell == null)
            {
                return;
            }

            foreach (BoardCell cell in GetPreviewCells(actionId, centerCell, previewProfile))
            {
                if (!overlayRenderers.TryGetValue(new Vector2Int(cell.X, cell.Y), out SpriteRenderer renderer))
                {
                    continue;
                }

                Color cellColor = previewColor;
                if (cell.X != centerCell.X || cell.Y != centerCell.Y)
                {
                    cellColor.a *= 0.7f;
                }

                renderer.color = cellColor;
            }
        }

        private bool IsTargetValid(string actionId, Vector3 targetCoords)
        {
            Attempt attempt = attemptController.CurrentAttempt;
            if (attempt == null || !attempt.TryGetAction(actionId, out PlayerAction action))
            {
                return false;
            }

            PlayerActionContext context = attempt.CreateActionContext(
                new PlayerActionRequest(actionId, PlayerInputKind.Mouse, targetCoords, "BoardUiPreview"));
            return action.CanExecute(context);
        }

        private IEnumerable<BoardCell> GetPreviewCells(string actionId, BoardCell centerCell, PreviewProfile previewProfile)
        {
            if (string.Equals(actionId, "purify_area", StringComparison.OrdinalIgnoreCase))
            {
                foreach (BoardCell cell in PrototypeSpellbook.GetPurifyAreaCells(attemptController.CurrentAttempt, centerCell.Coords))
                {
                    yield return cell;
                }

                yield break;
            }

            if (previewProfile.Radius <= 0)
            {
                yield return centerCell;

                yield break;
            }

            foreach (BoardCell cell in currentBoard.GetCellsInRadius(centerCell.Coords, previewProfile.Radius))
            {
                yield return cell;
            }
        }

        private PreviewProfile GetPreviewProfile(string actionId)
        {
            return previewProfiles.TryGetValue(actionId ?? string.Empty, out PreviewProfile profile)
                ? profile
                : new PreviewProfile(PreviewFamily.SingleRestore, 0);
        }

        private Color GetPreviewColor(PreviewFamily family)
        {
            return family switch
            {
                PreviewFamily.SingleDefense => new Color(1f, 0.85f, 0.2f, 0.45f),
                PreviewFamily.SingleGrowth => new Color(0.2f, 0.95f, 0.95f, 0.45f),
                PreviewFamily.SingleDestruction => new Color(1f, 0.3f, 0.3f, 0.45f),
                PreviewFamily.AreaRestore => new Color(0.2f, 1f, 0.45f, 0.4f),
                PreviewFamily.AreaFreeze => new Color(0.35f, 0.7f, 1f, 0.4f),
                _ => new Color(0.2f, 1f, 0.45f, 0.45f)
            };
        }

        private bool TryGetHoveredCoords(out Vector2Int coords)
        {
            coords = default;
            if (Mouse.current == null)
            {
                return false;
            }

            float planeZ = attemptController.BoardPrefabs.CellDepth + overlayDepthOffset - targetCamera.transform.position.z;
            Vector3 mousePosition = Mouse.current.position.ReadValue();
            Vector3 worldPoint = targetCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, planeZ));
            Vector3 boardPoint = worldPoint - attemptController.BoardPrefabs.BoardOrigin;
            int x = Mathf.RoundToInt(boardPoint.x / attemptController.BoardPrefabs.CellSpacing);
            int y = Mathf.RoundToInt(boardPoint.y / attemptController.BoardPrefabs.CellSpacing);

            coords = new Vector2Int(x, y);

            return currentBoard.GetCell(x, y) != null;
        }

        private SpriteRenderer CreateOverlayRenderer(BoardCell cell)
        {
            GameObject overlayObject = new GameObject($"BoardUi_{cell.X}_{cell.Y}");
            overlayObject.transform.SetParent(overlayParent, false);
            overlayObject.transform.localPosition = GetOverlayPosition(cell);
            overlayObject.transform.localRotation = Quaternion.identity;
            overlayObject.transform.localScale = GetOverlayScale();

            SpriteRenderer spriteRenderer = overlayObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetOverlaySprite();
            spriteRenderer.color = Color.clear;
            spriteRenderer.sortingOrder = sortingOrder;

            return spriteRenderer;
        }

        private Vector3 GetOverlayPosition(BoardCell cell)
        {
            Vector3 basePosition = attemptController.BoardPrefabs.GetCellWorldPosition(cell);
            return new Vector3(basePosition.x, basePosition.y, basePosition.z + overlayDepthOffset);
        }

        private Vector3 GetOverlayScale()
        {
            Vector3 cellScale = attemptController.BoardPrefabs.CellScale;
            float size = attemptController.BoardPrefabs.CellSpacing * overlayScaleMultiplier;
            float width = size * (Mathf.Abs(cellScale.x) <= 0.001f ? 1f : Mathf.Abs(cellScale.x));
            float height = size * (Mathf.Abs(cellScale.y) <= 0.001f ? 1f : Mathf.Abs(cellScale.y));
            return new Vector3(width, height, 1f);
        }

        private void HideAll()
        {
            foreach (SpriteRenderer renderer in overlayRenderers.Values)
            {
                if (renderer != null)
                {
                    renderer.color = Color.clear;
                }
            }
        }

        private void ClearOverlay()
        {
            foreach (SpriteRenderer renderer in overlayRenderers.Values)
            {
                if (renderer != null)
                {
                    Destroy(renderer.gameObject);
                }
            }

            overlayRenderers.Clear();
        }

        private void EnsureOverlayParent()
        {
            if (overlayParent != null)
            {
                return;
            }

            GameObject rootObject = new GameObject("BoardUiRoot");
            rootObject.transform.SetParent(transform, false);
            overlayParent = rootObject.transform;
        }

        private void BuildPreviewProfiles()
        {
            previewProfiles.Clear();
            previewProfiles["cleanse_tile"] = new PreviewProfile(PreviewFamily.SingleRestore, 0);
            previewProfiles["heal_land"] = new PreviewProfile(PreviewFamily.SingleRestore, 0);
            previewProfiles["fortify_cell"] = new PreviewProfile(PreviewFamily.SingleDefense, 0);
            previewProfiles["mana_bloom"] = new PreviewProfile(PreviewFamily.SingleGrowth, 0);
            previewProfiles["purify_area"] = new PreviewProfile(PreviewFamily.AreaRestore, 1);
            previewProfiles["freeze_spread"] = new PreviewProfile(PreviewFamily.AreaFreeze, 1);
            previewProfiles["consecrate_cell"] = new PreviewProfile(PreviewFamily.SingleDestruction, 0);
        }

        private static Sprite GetOverlaySprite()
        {
            if (overlaySprite != null)
            {
                return overlaySprite;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            overlaySprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);

            return overlaySprite;
        }

        private void HandleAttemptStarted(Attempt attempt)
        {
            BuildOverlay(attempt);
        }

        private void HandleAttemptStateChanged(Attempt attempt)
        {
            if (currentBoard == null)
            {
                BuildOverlay(attempt);
            }
        }

        private void HandleTurnResolved(Attempt attempt, PlayerAction action)
        {
            HideAll();
        }

        private void HandleAttemptEnded(Attempt attempt)
        {
            HideAll();
        }

        private void HandlePreviewActionChanged(Attempt attempt, string actionId)
        {
            DrawPreview();
        }
    }
}
