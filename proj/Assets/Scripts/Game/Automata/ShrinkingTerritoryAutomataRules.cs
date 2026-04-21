namespace AI4GamesFinalProj.Gameplay
{
    public sealed class ShrinkingTerritoryAutomataRules : ICellularAutomataRules
    {
        public ShrinkingTerritoryAutomataRules(int seed)
        {
        }

        public void ApplyStep(Attempt attempt)
        {
            SquareGameBoard board = attempt.Board as SquareGameBoard;
            if (board == null)
            {
                return;
            }

            board.UpdateCells();
            board.ApplyNextStates();
        }
    }
}
