using System;
using UnityEngine;

namespace AI4GamesFinalProj.Gameplay
{
    [Serializable]
    public sealed class BoardViewPrefabLibrary
    {
        public Transform CellParent;
        public Transform GeometryParent;
        public GameObject GeometryPlaceholderPrefab;
        public Vector3 BoardOrigin = Vector3.zero;
        public float CellSpacing = 1.1f;
        public float CellDepth = 0f;
        public Vector3 CellScale = Vector3.one;
        public Vector3 CellRotationEuler = Vector3.zero;
        public bool ShowCellStatusText = true;
        public Vector3 StatusTextOffset = new Vector3(0f, -0.30f, 0f);
        public Vector3 StatusTextScale = new Vector3(0.2f, 0.2f, 0.2f);
        public GameObject HealthyLandPrefab;
        public GameObject CorruptedLandPrefab;
        public GameObject ManaSpringPrefab;
        public GameObject LifeRootPrefab;
        public GameObject SacredCellPrefab;
        public GameObject DeadCellPrefab;

        public Transform GetSpawnParent()
        {
            return CellParent != null ? CellParent : GeometryParent;
        }

        public GameObject ResolveCellPrefab(CellType cellType)
        {
            return cellType switch
            {
                CellType.CorruptedLand => CorruptedLandPrefab,
                CellType.ManaSpring => ManaSpringPrefab,
                CellType.LifeRoot => LifeRootPrefab,
                CellType.SacredCell => SacredCellPrefab,
                CellType.DeadCell => DeadCellPrefab,
                _ => HealthyLandPrefab
            };
        }

        public Vector3 GetCellWorldPosition(BoardCell cell)
        {
            if (cell == null)
            {
                return BoardOrigin;
            }

            return BoardOrigin + new Vector3(cell.X * CellSpacing, cell.Y * CellSpacing, CellDepth);
        }

        public Quaternion GetCellRotation()
        {
            return Quaternion.Euler(CellRotationEuler);
        }

        public Color GetFallbackTint(CellType cellType)
        {
            return cellType switch
            {
                CellType.CorruptedLand => new Color(0.65f, 0.15f, 0.15f),
                CellType.ManaSpring => new Color(0.15f, 0.55f, 0.95f),
                CellType.LifeRoot => new Color(0.2f, 0.8f, 0.35f),
                CellType.SacredCell => new Color(0.95f, 0.9f, 0.35f),
                CellType.DeadCell => new Color(0.25f, 0.25f, 0.25f),
                _ => new Color(0.35f, 0.75f, 0.35f)
            };
        }
    }
}
