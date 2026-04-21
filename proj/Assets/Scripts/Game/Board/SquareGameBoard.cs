using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AI4GamesFinalProj.Gameplay
{
    public sealed class SquareGameBoard : IGameBoard
    {
        private readonly BoardCell[,] cells;
        private readonly List<BoardCell> allCells = new List<BoardCell>();
        private readonly System.Random random = new System.Random();

        public SquareGameBoard(int width, int height, int seed = 0)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            Width = width;
            Height = height;
            Seed = seed;
            cells = new BoardCell[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    BoardCell cell = new BoardCell(new Vector3(x, y, 0f), CellArchetype.HealthyLand);
                    cells[x, y] = cell;
                    allCells.Add(cell);
                }
            }

            random = seed == 0 ? new System.Random() : new System.Random(seed);
        }

        public int Width { get; }

        public int Height { get; }

        public int Seed { get; }

        public BoardCell[,] Grid => cells;

        public IEnumerable<IBoardCell> Cells => allCells;

        public IEnumerable<BoardCell> AllCells => allCells;

        public bool IsInside(Vector3 coords)
        {
            Vector2Int index = ToIndex(coords);
            return index.x >= 0 && index.x < Width && index.y >= 0 && index.y < Height;
        }

        public IBoardCell GetCell(Vector3 coords)
        {
            TryGetCell(coords, out IBoardCell cell);
            return cell;
        }

        public bool TryGetCell(Vector3 coords, out IBoardCell cell)
        {
            cell = null;
            Vector2Int index = ToIndex(coords);
            if (index.x < 0 || index.x >= Width || index.y < 0 || index.y >= Height)
            {
                return false;
            }

            cell = cells[index.x, index.y];
            return true;
        }

        public BoardCell GetBoardCell(Vector3 coords)
        {
            return GetCell(coords) as BoardCell;
        }

        public BoardCell GetCell(int x, int y)
        {
            return GetBoardCell(new Vector3(x, y, 0f));
        }

        public IEnumerable<IBoardCell> GetNeighbors(Vector3 coords, NeighborhoodType neighborhoodType = NeighborhoodType.Moore)
        {
            return GetNeighborCells(coords, neighborhoodType);
        }

        public IEnumerable<BoardCell> GetNeighbors(BoardCell cell)
        {
            return GetNeighborCells(cell.Coords, NeighborhoodType.Moore);
        }

        public IEnumerable<BoardCell> GetNeighborCells(Vector3 coords, NeighborhoodType neighborhoodType = NeighborhoodType.Moore)
        {
            Vector2Int index = ToIndex(coords);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    if (neighborhoodType == NeighborhoodType.VonNeumann && Math.Abs(dx) + Math.Abs(dy) != 1)
                    {
                        continue;
                    }

                    int nx = index.x + dx;
                    int ny = index.y + dy;
                    if (nx < 0 || nx >= Width || ny < 0 || ny >= Height)
                    {
                        continue;
                    }

                    yield return cells[nx, ny];
                }
            }
        }

        public IEnumerable<BoardCell> GetCellsInRadius(Vector3 coords, int radius)
        {
            Vector2Int center = ToIndex(coords);
            for (int x = center.x - radius; x <= center.x + radius; x++)
            {
                for (int y = center.y - radius; y <= center.y + radius; y++)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height)
                    {
                        continue;
                    }

                    yield return cells[x, y];
                }
            }
        }

        public void SetArchetype(Vector3 coords, CellArchetype archetype)
        {
            BoardCell cell = GetBoardCell(coords);
            if (cell == null)
            {
                return;
            }

            cell.TransformTo(archetype);
            cell.SetNextType(cell.Type);
        }

        public Vector3 GetWorldPosition(BoardCell cell, float cellSpacing, Vector3 origin, float heightOffset = 0f)
        {
            if (cell == null)
            {
                return origin;
            }

            return origin + new Vector3(cell.X * cellSpacing, heightOffset, cell.Y * cellSpacing);
        }

        public int CountCellsByType(CellType type)
        {
            return allCells.Count(cell => cell.Type == type);
        }

        public int CountAliveCellsByType(CellType type)
        {
            return allCells.Count(cell => cell.Type == type && cell.IsAlive() && !cell.IsCorrupted);
        }

        public int CountNeighborsByType(BoardCell cell, CellType type)
        {
            return GetNeighbors(cell).Count(neighbor => neighbor.Type == type);
        }

        public void UpdateCells()
        {
            foreach (BoardCell cell in allCells)
            {
                cell.SetNextType(cell.Type);
            }

            HashSet<BoardCell> spreadTargets = new HashSet<BoardCell>();

            foreach (BoardCell cell in allCells)
            {
                CellTypeRule rules = CellTypeRules.For(cell.Type);
                int corruptedNeighbors = CountNeighborsByType(cell, CellType.CorruptedLand);
                int sacredNeighbors = CountNeighborsByType(cell, CellType.SacredCell);

                if (rules.Immutable)
                {
                    cell.SetNextType(CellType.DeadCell);
                    continue;
                }

                if (rules.DiesFromCorruptedNeighbor && corruptedNeighbors > 0)
                {
                    cell.SetNextType(CellType.DeadCell);
                    continue;
                }

                if (cell.Type == CellType.CorruptedLand)
                {
                    if (cell.CorruptedTurns + 1 >= rules.CorruptionLifetime)
                    {
                        cell.SetNextType(CellType.DeadCell);
                    }

                    foreach (BoardCell target in PickSpreadTargets(cell, rules.SpreadCount))
                    {
                        spreadTargets.Add(target);
                    }

                    continue;
                }

                if (!rules.Corruptible)
                {
                    continue;
                }

                int temporaryBonus = (cell.ShieldTurns > 0 ? 1 : 0) + (cell.PurifiedTurns > 0 ? 2 : 0);
                int sacredSupportBonus = cell.Type == CellType.HealthyLand && sacredNeighbors >= 2 ? 1 : 0;
                int threshold = rules.BaseThreshold + rules.ResistanceBonus + temporaryBonus + sacredSupportBonus;
                if (corruptedNeighbors >= threshold)
                {
                    cell.SetNextType(CellType.CorruptedLand);
                }
            }

            foreach (BoardCell target in spreadTargets)
            {
                if (target.Type != CellType.DeadCell)
                {
                    target.SetNextType(CellType.CorruptedLand);
                }
            }
        }

        public void ApplyNextStates()
        {
            foreach (BoardCell cell in allCells)
            {
                cell.ApplyNextState();
            }
        }

        private IEnumerable<BoardCell> PickSpreadTargets(BoardCell sourceCell, int spreadCount)
        {
            if (spreadCount <= 0 || sourceCell.FrozenTurns > 0)
            {
                yield break;
            }

            List<BoardCell> candidates = GetNeighbors(sourceCell)
                .Where(candidate =>
                    candidate.Type != CellType.DeadCell &&
                    candidate.Type != CellType.CorruptedLand &&
                    candidate.ShieldTurns <= 0 &&
                    candidate.PurifiedTurns <= 0)
                .OrderBy(_ => random.Next())
                .Take(spreadCount)
                .ToList();

            foreach (BoardCell candidate in candidates)
            {
                yield return candidate;
            }
        }

        private static Vector2Int ToIndex(Vector3 coords)
        {
            return new Vector2Int(Mathf.RoundToInt(coords.x), Mathf.RoundToInt(coords.y));
        }
    }
}
