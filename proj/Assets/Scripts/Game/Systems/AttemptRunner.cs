using System;
using System.Collections.Generic;

namespace AI4GamesFinalProj.Gameplay
{
    public sealed class AttemptRunner
    {
        private readonly Queue<PlayerActionRequest> pendingRequests = new Queue<PlayerActionRequest>();
        private readonly AttemptTurnProcessor turnProcessor;
        private readonly UtilityAiActionSelector actionSelector;
        private readonly UtilityAiTurnDriver turnDriver;

        public Attempt Attempt { get; }

        public AttemptLoopState State { get; private set; }

        public bool IsWaitingForPlayerInput => State == AttemptLoopState.WaitingForPlayerInput;

        public event Action<Attempt> AttemptStarted;
        public event Action<Attempt> WaitingForPlayerInput;
        public event Action<Attempt, PlayerActionRequest, PlayerAction> TurnResolved;
        public event Action<Attempt> AttemptEnded;

        public AttemptRunner(
            Attempt attempt,
            UtilityAiTurnDriver turnDriver,
            AttemptTurnProcessor turnProcessor,
            UtilityAiActionSelector actionSelector)
        {
            Attempt = attempt ?? throw new ArgumentNullException(nameof(attempt));
            this.turnDriver = turnDriver ?? throw new ArgumentNullException(nameof(turnDriver));
            this.turnProcessor = turnProcessor ?? throw new ArgumentNullException(nameof(turnProcessor));
            this.actionSelector = actionSelector ?? throw new ArgumentNullException(nameof(actionSelector));
            State = AttemptLoopState.NotStarted;
        }

        public void Start()
        {
            if (State != AttemptLoopState.NotStarted)
            {
                return;
            }

            AttemptStarted?.Invoke(Attempt);
            BeginTurn();
        }

        public void EnqueueInput(PlayerActionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (State == AttemptLoopState.Completed)
            {
                return;
            }

            pendingRequests.Enqueue(request);
        }

        public void Update()
        {
            if (State != AttemptLoopState.WaitingForPlayerInput || pendingRequests.Count == 0)
            {
                return;
            }

            PlayerActionRequest request = pendingRequests.Dequeue();
            State = AttemptLoopState.ResolvingTurn;

            if (request.IsEndTurnRequest)
            {
                if (!Attempt.CanEndTurnEarly && Attempt.CurrentOffers.Count > 0)
                {
                    State = AttemptLoopState.WaitingForPlayerInput;
                    WaitingForPlayerInput?.Invoke(Attempt);

                    return;
                }

                FinalizeCurrentTurn();
                return;
            }

            bool resolved = turnDriver.TryResolvePlayerAction(Attempt, request, out PlayerAction action);
            if (resolved)
            {
                Attempt.RegisterResolvedAction(action);
                TurnResolved?.Invoke(Attempt, request, action);
            }
            else
            {
                State = AttemptLoopState.WaitingForPlayerInput;
                WaitingForPlayerInput?.Invoke(Attempt);

                return;
            }

            if (Attempt.IsComplete || !Attempt.CanResolveMoreActions)
            {
                FinalizeCurrentTurn();

                return;
            }

            RefreshOffers();
        }

        private void BeginTurn()
        {
            turnProcessor.BeginTurn(Attempt);
            if (Attempt.IsComplete)
            {
                State = AttemptLoopState.Completed;
                AttemptEnded?.Invoke(Attempt);
                
                return;
            }

            Attempt.BeginActionPhase();
            RefreshOffers();
        }

        private void RefreshOffers()
        {
            Attempt.SetCurrentOffers(actionSelector.BuildTopOffers(Attempt));
            State = AttemptLoopState.WaitingForPlayerInput;
            WaitingForPlayerInput?.Invoke(Attempt);
        }

        private void FinalizeCurrentTurn()
        {
            Attempt.EndActionPhase();
            bool advanced = turnDriver.CompleteTurn(Attempt);

            if (!advanced || Attempt.IsComplete)
            {
                State = AttemptLoopState.Completed;
                AttemptEnded?.Invoke(Attempt);

                return;
            }

            BeginTurn();
        }
    }
}
