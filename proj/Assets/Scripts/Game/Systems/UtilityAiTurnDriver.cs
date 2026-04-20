using System;

namespace AI4GamesFinalProj.Gameplay
{
    public sealed class UtilityAiTurnDriver
    {
        private readonly CellularAutomataEngine cellularAutomataEngine;

        public UtilityAiTurnDriver(CellularAutomataEngine cellularAutomataEngine)
        {
            this.cellularAutomataEngine = cellularAutomataEngine ?? throw new ArgumentNullException(nameof(cellularAutomataEngine));
        }

        public bool TryResolvePlayerTurn(Attempt attempt, PlayerAction chosenAction)
        {
            if (attempt == null)
            {
                throw new ArgumentNullException(nameof(attempt));
            }

            if (chosenAction == null)
            {
                throw new ArgumentNullException(nameof(chosenAction));
            }

            if (attempt.IsComplete || !chosenAction.CanExecute(attempt))
            {
                return false;
            }

            chosenAction.Apply(attempt);

            if (!attempt.World.TryAdvanceTick())
            {
                return false;
            }

            cellularAutomataEngine.Step(attempt);
            return true;
        }
    }
}
