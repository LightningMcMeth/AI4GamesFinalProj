using System.Linq;
using UnityEngine;

namespace AI4GamesFinalProj.Gameplay
{
    //These are not AI comments, just notes from sanity checks to make sure code is clean (at least somewhat)
    //PrototypeGameFactory assembles the main objects needed for a game (Board, rules, player)
    public sealed class PrototypeGameFactory
    {
        private readonly int width;
        private readonly int height;
        private readonly int seed;

        public PrototypeGameFactory(int width, int height, int seed)
        {
            this.width = width;
            this.height = height;
            this.seed = seed;
        }

        public SquareGameBoard CreateBoard()
        {
            SquareGameBoard board = new SquareGameBoard(width, height, seed);
            System.Random random = seed == 0 ? new System.Random() : new System.Random(seed * 17 + 11);

            Vector3 center = new Vector3(width / 2, height / 2, 0f);
            board.SetArchetype(center, CellArchetype.LifeRoot);
            board.SetArchetype(center + new Vector3(-1f, 0f, 0f), CellArchetype.SacredSite);
            board.SetArchetype(center + new Vector3(1f, 0f, 0f), CellArchetype.SacredSite);
            board.SetArchetype(center + new Vector3(0f, 1f, 0f), CellArchetype.HealthyLand);
            board.SetArchetype(center + new Vector3(0f, -1f, 0f), CellArchetype.HealthyLand);

            board.SetArchetype(new Vector3(1f, 1f, 0f), CellArchetype.ManaSpring);
            board.SetArchetype(new Vector3(width - 2, 1f, 0f), CellArchetype.ManaSpring);
            board.SetArchetype(new Vector3(1f, height - 2, 0f), CellArchetype.ManaSpring);
            board.SetArchetype(new Vector3(width - 2, height - 2, 0f), CellArchetype.ManaSpring);

            SpawnInitialCorruption(board, random);

            return board;
        }

        public Player CreatePlayer(string playerName)
        {
            return PrototypeSpellbook.CreatePlayer(playerName);
        }

        public ICellularAutomataRules CreateAutomataRules()
        {
            return new ShrinkingTerritoryAutomataRules(seed);
        }

        private void SpawnInitialCorruption(SquareGameBoard board, System.Random random)
        {
            System.Collections.Generic.List<BoardCell> manaSprings = board.AllCells
                .Where(cell => cell.Archetype == CellArchetype.ManaSpring)
                .ToList();

            System.Collections.Generic.List<BoardCell> spawnCandidates = board.AllCells
                .Where(cell => IsInOuterTwoRings(board, cell) && IsValidVirusSpawnCell(cell, manaSprings))
                .ToList();

            if (spawnCandidates.Count == 0)
            {
                return;
            }

            BoardCell origin = spawnCandidates[random.Next(spawnCandidates.Count)];
            origin.MarkCorrupted();

            Vector3 boardCenter = new Vector3((board.Width - 1) * 0.5f, (board.Height - 1) * 0.5f, 0f);
            System.Collections.Generic.List<BoardCell> preferredNeighbors = board.GetNeighbors(origin)
                .Where(cell => cell != null && IsValidVirusSpawnCell(cell, manaSprings))
                .OrderBy(cell => Vector3.SqrMagnitude(cell.Coords - boardCenter))
                .Take(2)
                .ToList();

            foreach (BoardCell cell in preferredNeighbors)
            {
                cell.MarkCorrupted();
            }
        }

        private static bool IsInOuterTwoRings(SquareGameBoard board, BoardCell cell)
        {
            return cell.X <= 1 ||
                cell.Y <= 1 ||
                cell.X >= board.Width - 2 ||
                cell.Y >= board.Height - 2;
        }

        private static bool IsValidVirusSpawnCell(
            BoardCell cell,
            System.Collections.Generic.IReadOnlyList<BoardCell> manaSprings)
        {
            if (cell == null ||
                cell.Archetype == CellArchetype.ManaSpring ||
                cell.Archetype == CellArchetype.LifeRoot ||
                cell.Archetype == CellArchetype.SacredSite ||
                cell.IsDead)
            {
                return false;
            }

            foreach (BoardCell manaSpring in manaSprings)
            {
                int dx = Mathf.Abs(cell.X - manaSpring.X);
                int dy = Mathf.Abs(cell.Y - manaSpring.Y);

                if (Mathf.Max(dx, dy) <= 1)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
