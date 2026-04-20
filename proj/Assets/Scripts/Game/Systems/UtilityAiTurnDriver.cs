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

        public bool TryResolvePlayerTurn(Attempt attempt, PlayerActionRequest request, out PlayerAction resolvedAction)
        {
            resolvedAction = null;

            if (attempt == null)
            {
                throw new ArgumentNullException(nameof(attempt));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (attempt.IsComplete || !attempt.TryGetAction(request.ActionId, out PlayerAction chosenAction))
            {
                return false;
            }

            PlayerActionContext context = attempt.CreateActionContext(request);
            if (!chosenAction.CanExecute(context))
            {
                return false;
            }

            chosenAction.Apply(context);

            if (!attempt.World.TryAdvanceTick())
            {
                return false;
            }

            cellularAutomataEngine.Step(attempt);
            resolvedAction = chosenAction;
            return true;
        }
    }
}
