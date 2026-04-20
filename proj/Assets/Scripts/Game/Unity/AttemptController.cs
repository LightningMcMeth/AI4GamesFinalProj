using System;
using System.Collections.Generic;
using UnityEngine;

namespace AI4GamesFinalProj.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class AttemptController : MonoBehaviour
    {
        [SerializeField]
        [Min(5)]
        private int totalTicks = 20;

        private readonly List<IAttemptInputSource> inputSources = new List<IAttemptInputSource>();
        private AttemptRunner attemptRunner;

        public Attempt CurrentAttempt { get; private set; }

        public bool HasActiveAttempt =>
            CurrentAttempt != null &&
            attemptRunner != null &&
            attemptRunner.State != AttemptLoopState.Completed;

        public event Action<Attempt> AttemptStarted;
        public event Action<Attempt> AwaitingPlayerInput;
        public event Action<Attempt, PlayerAction> TurnResolved;
        public event Action<Attempt> AttemptEnded;

        private void Update()
        {
            if (attemptRunner == null)
            {
                return;
            }

            PollRegisteredInputSources();
            attemptRunner.Update();
        }

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

            ClearAttempt();

            CurrentAttempt = new Attempt(new GameWorld(totalTicks), board, player);
            attemptRunner = new AttemptRunner(
                CurrentAttempt,
                new UtilityAiTurnDriver(new CellularAutomataEngine(automataRules)));

            attemptRunner.AttemptStarted += HandleAttemptStarted;
            attemptRunner.WaitingForPlayerInput += HandleWaitingForPlayerInput;
            attemptRunner.TurnResolved += HandleTurnResolved;
            attemptRunner.AttemptEnded += HandleAttemptEnded;

            attemptRunner.Start();
        }

        public bool SubmitPlayerAction(PlayerAction action)
        {
            if (action == null)
            {
                return false;
            }

            return SubmitPlayerAction(action.Id);
        }

        public bool SubmitPlayerAction(
            string actionId,
            Vector3? targetCoords = null,
            PlayerInputKind inputKind = PlayerInputKind.Ui,
            string inputBindingId = null)
        {
            if (attemptRunner == null || string.IsNullOrWhiteSpace(actionId))
            {
                return false;
            }

            attemptRunner.EnqueueInput(new PlayerActionRequest(actionId, inputKind, targetCoords, inputBindingId));
            return true;
        }

        public void RegisterInputSource(IAttemptInputSource inputSource)
        {
            if (inputSource == null || inputSources.Contains(inputSource))
            {
                return;
            }

            inputSources.Add(inputSource);
        }

        public void UnregisterInputSource(IAttemptInputSource inputSource)
        {
            if (inputSource == null)
            {
                return;
            }

            inputSources.Remove(inputSource);
        }

        public void ClearAttempt()
        {
            if (attemptRunner != null)
            {
                attemptRunner.AttemptStarted -= HandleAttemptStarted;
                attemptRunner.WaitingForPlayerInput -= HandleWaitingForPlayerInput;
                attemptRunner.TurnResolved -= HandleTurnResolved;
                attemptRunner.AttemptEnded -= HandleAttemptEnded;
            }

            CurrentAttempt = null;
            attemptRunner = null;
        }

        private void PollRegisteredInputSources()
        {
            if (attemptRunner == null || !attemptRunner.IsWaitingForPlayerInput)
            {
                return;
            }

            foreach (IAttemptInputSource inputSource in inputSources)
            {
                if (inputSource != null && inputSource.TryCreateRequest(CurrentAttempt, out PlayerActionRequest request))
                {
                    attemptRunner.EnqueueInput(request);
                }
            }
        }

        private void HandleAttemptStarted(Attempt attempt)
        {
            AttemptStarted?.Invoke(attempt);
        }

        private void HandleWaitingForPlayerInput(Attempt attempt)
        {
            AwaitingPlayerInput?.Invoke(attempt);
        }

        private void HandleTurnResolved(Attempt attempt, PlayerActionRequest request, PlayerAction action)
        {
            TurnResolved?.Invoke(attempt, action);
        }

        private void HandleAttemptEnded(Attempt attempt)
        {
            AttemptEnded?.Invoke(attempt);
        }
    }
}
