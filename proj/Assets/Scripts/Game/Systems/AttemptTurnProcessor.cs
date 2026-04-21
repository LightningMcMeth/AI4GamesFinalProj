using System.Linq;

namespace AI4GamesFinalProj.Gameplay
{
    public sealed class AttemptTurnProcessor
    {
        public void BeginTurn(Attempt attempt)
        {
            foreach (BoardCell cell in attempt.Cells)
            {
                cell.AdvanceTurn();
            }

            SquareGameBoard board = attempt.Board as SquareGameBoard;
            attempt.World.CollectResources(board);
            attempt.World.SetDangerLevel(BoardAnalysis.ComputeDangerLevel(attempt));
            attempt.World.CheckLoseCondition();
        }

        public void EndTurn(Attempt attempt)
        {
            attempt.World.UpdateLifeRootsRemaining(BoardAnalysis.CountAliveRoots(attempt));
            attempt.World.SetDangerLevel(BoardAnalysis.ComputeDangerLevel(attempt));

            if (attempt.World.CheckLoseCondition())
            {
                return;
            }

            if (attempt.World.CurrentTick >= attempt.World.TotalTicks)
            {
                attempt.World.Win("You survived the shrinking territory.");
            }
        }
    }
}
