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

            board.GetBoardCell(new Vector3(width - 3, height - 3, 0f))?.MarkCorrupted();
            board.GetBoardCell(new Vector3(width - 4, height - 3, 0f))?.MarkCorrupted();
            board.GetBoardCell(new Vector3(width - 3, height - 4, 0f))?.MarkCorrupted();

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
    }
}
