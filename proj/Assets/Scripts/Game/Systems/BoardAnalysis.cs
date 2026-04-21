using System.Linq;
using UnityEngine;

namespace AI4GamesFinalProj.Gameplay
{
    public static class BoardAnalysis
    {
        public static SquareGameBoard GetSquareBoard(Attempt attempt)
        {
            return attempt.Board as SquareGameBoard;
        }

        public static int CountCorruptedNeighbors(SquareGameBoard board, BoardCell cell)
        {
            return board.GetNeighborCells(cell.Coords).Count(neighbor => neighbor.IsCorrupted);
        }

        public static int CountSacredNeighbors(SquareGameBoard board, BoardCell cell)
        {
            return board.GetNeighborCells(cell.Coords)
                .Count(neighbor => neighbor.Archetype == CellArchetype.SacredSite && neighbor.IsStable);
        }

        public static int CountAdjacentRoots(SquareGameBoard board, BoardCell cell)
        {
            return board.GetNeighborCells(cell.Coords)
                .Count(neighbor => neighbor.Archetype == CellArchetype.LifeRoot && neighbor.IsStable);
        }

        public static int CountAdjacentSprings(SquareGameBoard board, BoardCell cell)
        {
            return board.GetNeighborCells(cell.Coords)
                .Count(neighbor => neighbor.Archetype == CellArchetype.ManaSpring && neighbor.IsStable);
        }

        public static float ComputeDangerLevel(Attempt attempt)
        {
            SquareGameBoard board = GetSquareBoard(attempt);
            if (board == null)
            {
                return 0f;
            }

            int corruptedCount = board.AllCells.Count(cell => cell.IsCorrupted);
            int totalCells = board.AllCells.Count();
            int rootThreats = board.AllCells.Count(cell =>
                cell.Archetype == CellArchetype.LifeRoot &&
                board.GetNeighborCells(cell.Coords).Any(neighbor => neighbor.IsCorrupted));
            int springThreats = board.AllCells.Count(cell =>
                cell.Archetype == CellArchetype.ManaSpring &&
                board.GetNeighborCells(cell.Coords).Any(neighbor => neighbor.IsCorrupted));

            float corruptedRatio = totalCells == 0 ? 0f : (float)corruptedCount / totalCells;
            return Mathf.Clamp01(corruptedRatio * 0.5f + rootThreats * 0.3f + springThreats * 0.1f);
        }

        public static BoardCell FindBestCleanseTarget(Attempt attempt)
        {
            SquareGameBoard board = GetSquareBoard(attempt);
            if (board == null)
            {
                return null;
            }

            return board.AllCells
                .Where(cell => cell.IsCorrupted)
                .OrderByDescending(cell => ScoreCleanseTarget(board, cell))
                .FirstOrDefault();
        }

        public static float ScoreCleanseTarget(SquareGameBoard board, BoardCell cell)
        {
            int corruptedNeighbors = CountCorruptedNeighbors(board, cell);
            int rootThreat = CountAdjacentRoots(board, cell);
            int springThreat = CountAdjacentSprings(board, cell);
            return rootThreat * 12f + springThreat * 6f + corruptedNeighbors * 3f + cell.CorruptedTurns * 2f;
        }

        public static BoardCell FindBestFortifyTarget(Attempt attempt)
        {
            SquareGameBoard board = GetSquareBoard(attempt);
            if (board == null)
            {
                return null;
            }

            return board.AllCells
                .Where(cell => cell.IsStable && !cell.IsDead)
                .OrderByDescending(cell =>
                    CountCorruptedNeighbors(board, cell) * 4f +
                    CountAdjacentRoots(board, cell) * 8f +
                    CountAdjacentSprings(board, cell) * 5f +
                    (cell.Archetype == CellArchetype.SacredSite ? 3f : 0f) -
                    cell.BarrierTurns * 4f)
                .FirstOrDefault();
        }

        public static BoardCell FindBestManaBloomTarget(Attempt attempt)
        {
            SquareGameBoard board = GetSquareBoard(attempt);
            if (board == null)
            {
                return null;
            }

            return board.AllCells
                .Where(cell => cell.IsStable && cell.Archetype == CellArchetype.HealthyLand)
                .OrderByDescending(cell =>
                    CountAdjacentRoots(board, cell) * 5f +
                    CountSacredNeighbors(board, cell) * 2f -
                    CountCorruptedNeighbors(board, cell) * 4f)
                .FirstOrDefault();
        }

        public static BoardCell FindBestPurifyAreaTarget(Attempt attempt)
        {
            SquareGameBoard board = GetSquareBoard(attempt);
            if (board == null)
            {
                return null;
            }

            return board.AllCells
                .Where(cell => !cell.IsDead)
                .OrderByDescending(cell => ScorePurifyAreaTarget(board, cell))
                .FirstOrDefault();
        }

        public static float ScorePurifyAreaTarget(SquareGameBoard board, BoardCell cell)
        {
            return board.GetCellsInRadius(cell.Coords, 1).Sum(neighbor =>
                neighbor.IsCorrupted ? 4f + CountAdjacentRoots(board, neighbor) * 3f : 0f);
        }

        public static BoardCell FindBestFreezeTarget(Attempt attempt)
        {
            SquareGameBoard board = GetSquareBoard(attempt);
            if (board == null)
            {
                return null;
            }

            return board.AllCells
                .Where(cell => cell.IsCorrupted)
                .OrderByDescending(cell =>
                    CountCorruptedNeighbors(board, cell) * 5f +
                    CountAdjacentRoots(board, cell) * 7f +
                    CountAdjacentSprings(board, cell) * 3f -
                    cell.FrozenTurns * 4f)
                .FirstOrDefault();
        }

        public static BoardCell FindBestSacrificeTarget(Attempt attempt)
        {
            SquareGameBoard board = GetSquareBoard(attempt);
            if (board == null)
            {
                return null;
            }

            return board.AllCells
                .Where(cell =>
                    !cell.IsDead &&
                    cell.Archetype != CellArchetype.LifeRoot &&
                    cell.Archetype != CellArchetype.SacredSite &&
                    CountCorruptedNeighbors(board, cell) > 0)
                .OrderByDescending(cell =>
                    CountCorruptedNeighbors(board, cell) * 5f +
                    CountAdjacentRoots(board, cell) * 6f +
                    CountAdjacentSprings(board, cell) * 3f -
                    (cell.Archetype == CellArchetype.ManaSpring ? 4f : 0f))
                .FirstOrDefault();
        }

        public static int CountAliveRoots(Attempt attempt)
        {
            return attempt.Cells.Count(cell => cell.Archetype == CellArchetype.LifeRoot && cell.IsStable);
        }

        public static int CountAliveSprings(Attempt attempt)
        {
            return attempt.Cells.Count(cell => cell.Archetype == CellArchetype.ManaSpring && cell.IsStable);
        }

        public static int CountAliveSacredCells(Attempt attempt)
        {
            return attempt.Cells.Count(cell => cell.Archetype == CellArchetype.SacredSite && cell.IsStable);
        }

        public static BoardCell FindBestRestoreLandTarget(Attempt attempt)
        {
            SquareGameBoard board = GetSquareBoard(attempt);
            if (board == null)
            {
                return null;
            }

            return board.AllCells
                .Where(cell => cell.Type == CellType.DeadCell)
                .OrderByDescending(cell => ScoreRestoreLandTarget(board, cell))
                .FirstOrDefault();
        }

        public static float ScoreRestoreLandTarget(SquareGameBoard board, BoardCell cell)
        {
            return CountAdjacentRoots(board, cell) * 10f +
                CountAdjacentSprings(board, cell) * 6f +
                CountSacredNeighbors(board, cell) * 5f -
                CountCorruptedNeighbors(board, cell) * 4f;
        }
    }
}
