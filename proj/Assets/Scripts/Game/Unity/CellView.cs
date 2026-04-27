#if UNITY_EDITOR
#endif

using System.Collections.Generic;
using UnityEngine;

namespace AI4GamesFinalProj.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CellView : MonoBehaviour
    {
        private readonly List<Renderer> fallbackRenderers = new List<Renderer>();

        private BoardViewPrefabLibrary prefabLibrary;
        private BoardCell currentCell;
        private GameObject currentVisual;
        private TextMesh statusText;
        private CellType currentVisualType;
        private bool usingFallbackVisual;

        public Vector3 Coords { get; private set; }

        public void Initialize(BoardViewPrefabLibrary boardViewPrefabLibrary, BoardCell cell)
        {
            prefabLibrary = boardViewPrefabLibrary;
            Refresh(cell, true);
        }

        public void Refresh(BoardCell cell, bool forceVisualRebuild = false)
        {
            if (prefabLibrary == null || cell == null)
            {
                return;
            }

            currentCell = cell;
            Coords = cell.Coords;
            name = $"[{cell.X},{cell.Y}]";
            transform.localPosition = prefabLibrary.GetCellWorldPosition(cell);
            transform.localRotation = prefabLibrary.GetCellRotation();
            transform.localScale = prefabLibrary.CellScale;

            if (forceVisualRebuild || currentVisual == null || currentVisualType != cell.Type)
            {
                RebuildVisual(cell.Type);
                //SetEditorIcon();
            }

            UpdateFallbackTint();
            UpdateStatusText();
        }

        private void RebuildVisual(CellType cellType)
        {
            if (currentVisual != null)
            {
                Destroy(currentVisual);
            }

            fallbackRenderers.Clear();
            usingFallbackVisual = false;

            GameObject sourcePrefab = prefabLibrary.ResolveCellPrefab(cellType);
            if (sourcePrefab == null)
            {
                sourcePrefab = prefabLibrary.GeometryPlaceholderPrefab;
                usingFallbackVisual = true;
            }

            if (sourcePrefab != null)
            {
                currentVisual = Instantiate(sourcePrefab, transform);
                currentVisual.transform.localPosition = Vector3.zero;
                currentVisual.transform.localRotation = Quaternion.identity;
                currentVisual.transform.localScale = Vector3.one;
            }
            else
            {
                currentVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                currentVisual.transform.SetParent(transform, false);
                currentVisual.name = "GeneratedPlaceholder";
                Collider collider = currentVisual.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                usingFallbackVisual = true;
            }

            currentVisual.name = $"{cellType}_Visual";
            currentVisualType = cellType;
            fallbackRenderers.AddRange(currentVisual.GetComponentsInChildren<Renderer>(true));
        }

        private void UpdateFallbackTint()
        {
            if (!usingFallbackVisual || prefabLibrary == null)
            {
                return;
            }

            Color tint = prefabLibrary.GetFallbackTint(currentVisualType);
            foreach (Renderer renderer in fallbackRenderers)
            {
                if (renderer == null || renderer.sharedMaterial == null)
                {
                    continue;
                }

                renderer.material.color = tint;
            }
        }

        private void UpdateStatusText()
        {
            if (prefabLibrary == null)
            {
                return;
            }

            if (!prefabLibrary.ShowCellStatusText)
            {
                if (statusText != null)
                {
                    statusText.gameObject.SetActive(false);
                }

                return;
            }

            EnsureStatusText();
            statusText.gameObject.SetActive(true);
            statusText.transform.localPosition = prefabLibrary.StatusTextOffset;
            statusText.transform.localScale = prefabLibrary.StatusTextScale;
            statusText.text = BuildStatusText();
            statusText.color = Color.white;
        }

        private string BuildStatusText()
        {
            if (currentCell == null)
            {
                return string.Empty;
            }

            List<string> lines = new List<string>(4)
            {
                currentCell.Type.ToString()
            };

            if (currentCell.CorruptedTurns > 0)
            {
                lines.Add($"C:{currentCell.CorruptedTurns}");
            }

            if (currentCell.BarrierTurns > 0)
            {
                lines.Add($"B:{currentCell.BarrierTurns}");
            }

            if (currentCell.PurifiedTurns > 0)
            {
                lines.Add($"P:{currentCell.PurifiedTurns}");
            }

            if (currentCell.FrozenTurns > 0)
            {
                lines.Add($"F:{currentCell.FrozenTurns}");
            }

            return string.Join("\n", lines);
        }

        private void EnsureStatusText()
        {
            if (statusText != null)
            {
                return;
            }

            GameObject labelObject = new GameObject("StatusText");
            labelObject.transform.SetParent(transform, false);
            statusText = labelObject.AddComponent<TextMesh>();
            statusText.anchor = TextAnchor.MiddleCenter;
            statusText.alignment = TextAlignment.Center;
            statusText.characterSize = 0.1f;
            statusText.fontSize = 32;
        }

        //#if UNITY_EDITOR
        //        private void SetEditorIcon()
        //        {
        //            if (currentCell == null)
        //                return;

        //            Texture2D icon = currentCell.Type switch
        //            {
        //                CellType.HealthyLand => EditorGUIUtility.IconContent("sv_label_3").image as Texture2D,   // green
        //                CellType.CorruptedLand => EditorGUIUtility.IconContent("sv_label_6").image as Texture2D, // red
        //                CellType.ManaSpring => EditorGUIUtility.IconContent("sv_label_1").image as Texture2D,    // blue
        //                CellType.LifeRoot => EditorGUIUtility.IconContent("sv_label_2").image as Texture2D,      // teal
        //                CellType.SacredCell => EditorGUIUtility.IconContent("sv_label_4").image as Texture2D,    // yellow
        //                CellType.DeadCell => EditorGUIUtility.IconContent("sv_label_0").image as Texture2D,      // gray
        //                _ => null
        //            };

        //            if (icon != null)
        //            {
        //                EditorGUIUtility.SetIconForObject(gameObject, icon);
        //            }
        //        }
        //#endif
    }
}
