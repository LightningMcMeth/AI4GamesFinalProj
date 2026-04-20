using System;
using System.Collections.Generic;

namespace AI4GamesFinalProj.Gameplay
{
    public sealed class AttemptRunner
    {
        private readonly Queue<PlayerActionRequest> pendingRequests = new Queue<PlayerActionRequest>();
        private readonly UtilityAiTurnDriver turnDriver;

        public AttemptRunner(Attempt attempt, UtilityAiTurnDriver turnDriver)
        {
            Attempt = attempt ?? throw new ArgumentNullException(nameof(attempt));
            this.turnDriver = turnDriver ?? throw new ArgumentNullException(nameof(turnDriver));
            State = AttemptLoopState.NotStarted;
        }

        public Attempt Attempt { get; }

        public AttemptLoopState State { get; private set; }

        public bool IsWaitingForPlayerInput => State == AttemptLoopState.WaitingForPlayerInput;

        public event Action<Attempt> AttemptStarted;
        public event Action<Attempt> WaitingForPlayerInput;
        public event Action<Attempt, PlayerActionRequest, PlayerAction> TurnResolved;
        public event Action<Attempt> AttemptEnded;

        public void Start()
        {
            if (State != AttemptLoopState.NotStarted)
            {
                return;
            }

            State = AttemptLoopState.WaitingForPlayerInput;
            AttemptStarted?.Invoke(Attempt);
            WaitingForPlayerInput?.Invoke(Attempt);
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

            bool resolved = turnDriver.TryResolvePlayerTurn(Attempt, request, out PlayerAction action);
            if (resolved)
            {
                TurnResolved?.Invoke(Attempt, request, action);
            }

            if (Attempt.IsComplete)
            {
                State = AttemptLoopState.Completed;
                AttemptEnded?.Invoke(Attempt);
                return;
            }

            State = AttemptLoopState.WaitingForPlayerInput;
            WaitingForPlayerInput?.Invoke(Attempt);
        }
    }
}
