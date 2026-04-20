using System;
using UnityEngine;

namespace AI4GamesFinalProj.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class AttemptController : MonoBehaviour
    {
        [SerializeField]
        [Min(5)]
        private int totalTicks = 20;

        private UtilityAiTurnDriver turnDriver;

        public Attempt CurrentAttempt { get; private set; }

        public bool HasActiveAttempt => CurrentAttempt != null;

        public event Action<Attempt> AttemptStarted;
        public event Action<Attempt, PlayerAction> TurnResolved;
        public event Action<Attempt> AttemptEnded;

        public void StartAttempt(IGameBoard board, Player player, ICellularAutomataRules automataRules)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            CurrentAttempt = new Attempt(new GameWorld(totalTicks), board, player);
            turnDriver = new UtilityAiTurnDriver(new CellularAutomataEngine(automataRules));

            AttemptStarted?.Invoke(CurrentAttempt);
        }

        public bool SubmitPlayerAction(PlayerAction action)
        {
            if (CurrentAttempt == null || turnDriver == null || action == null)
            {
                return false;
            }

            bool didResolve = turnDriver.TryResolvePlayerTurn(CurrentAttempt, action);
            if (!didResolve)
            {
                return false;
            }

            TurnResolved?.Invoke(CurrentAttempt, action);

            if (CurrentAttempt.IsComplete)
            {
                AttemptEnded?.Invoke(CurrentAttempt);
            }

            return true;
        }

        public void ClearAttempt()
        {
            CurrentAttempt = null;
            turnDriver = null;
        }
    }
}
