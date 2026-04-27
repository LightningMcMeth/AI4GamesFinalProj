using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AI4GamesFinalProj.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class AttemptController : MonoBehaviour
    {
        [Header("Prototype Start")]
        [SerializeField]
        private bool autoStartPrototypeOnPlay = true;

        [SerializeField]
        [Min(5)]
        private int totalTicks = 20;

        [SerializeField]
        [Min(4)]
        private int boardWidth = 8;

        [SerializeField]
        [Min(4)]
        private int boardHeight = 8;

        [SerializeField]
        private int boardSeed = 7;

        [SerializeField]
        private string playerName = "Wizard";

        [SerializeField]
        [Min(0)]
        private int startingMana = 6;

        [SerializeField]
        [Min(1)]
        private int startingVitality = 5;

        [SerializeField]
        [Min(1)]
        private int maxPlayerActionsPerTurn = 2;

        [SerializeField]
        [Min(0)]
        private int vitalityUpkeepPerTurn = 1;

        [SerializeField]
        [Min(0)]
        private int startingEssence = 1;

        [SerializeField]
        private bool enableKeyboardDebugInput = true;

        [SerializeField]
        private bool enablePlayerActions = true;

        [SerializeField]
        private bool logTurnFlow = true;

        [SerializeField]
        private BoardViewPrefabLibrary boardPrefabs = new BoardViewPrefabLibrary();

        private readonly List<IAttemptInputSource> inputSources = new List<IAttemptInputSource>();
        private AttemptRunner attemptRunner;
        private KeyboardOfferInputSource keyboardInputSource;

        public Attempt CurrentAttempt { get; private set; }

        public bool HasActiveAttempt =>
            CurrentAttempt != null &&
            attemptRunner != null &&
            attemptRunner.State != AttemptLoopState.Completed;

        public bool IsWaitingForPlayerInput => attemptRunner != null && attemptRunner.IsWaitingForPlayerInput;

        public BoardViewPrefabLibrary BoardPrefabs => boardPrefabs;

        public IReadOnlyList<PlayerActionOffer> CurrentOffers => CurrentAttempt?.CurrentOffers ?? Array.Empty<PlayerActionOffer>();

        public string SelectedPreviewActionId => attemptRunner?.SelectedPreviewActionId ?? string.Empty;

        public bool HasSelectedPreviewAction => !string.IsNullOrWhiteSpace(SelectedPreviewActionId);

        public event Action<Attempt> AttemptStarted;
        public event Action<Attempt, IReadOnlyList<PlayerActionOffer>> OffersUpdated;
        public event Action<Attempt> AwaitingPlayerInput;
        public event Action<Attempt, PlayerAction> TurnResolved;
        public event Action<Attempt> AttemptEnded;
        public event Action<Attempt, string> PreviewActionChanged;

        private void Start()
        {
            if (enableKeyboardDebugInput)
            {
                keyboardInputSource = new KeyboardOfferInputSource();
                RegisterInputSource(keyboardInputSource);
            }

            if (autoStartPrototypeOnPlay)
            {
                StartPrototypeAttempt();
            }
        }

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
            GameWorld world = new GameWorld(totalTicks, startingMana, startingVitality, startingEssence, maxPlayerActionsPerTurn, vitalityUpkeepPerTurn);
            StartAttempt(world, board, player, automataRules);
        }

        public void StartAttempt(GameWorld world, IGameBoard board, Player player, ICellularAutomataRules automataRules)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            ClearAttempt();

            CurrentAttempt = new Attempt(world, board, player);

            attemptRunner = new AttemptRunner(
                CurrentAttempt,
                new UtilityAiTurnDriver(new CellularAutomataEngine(automataRules)),
                new UtilityAiActionSelector(),
                enablePlayerActions);

            attemptRunner.AttemptStarted += HandleAttemptStarted;
            attemptRunner.WaitingForPlayerInput += HandleWaitingForPlayerInput;
            attemptRunner.TurnResolved += HandleTurnResolved;
            attemptRunner.AttemptEnded += HandleAttemptEnded;
            attemptRunner.PreviewActionChanged += HandlePreviewActionChanged;

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

        public bool SubmitOfferedAction(int offerIndex, Vector3? targetCoords = null)
        {
            if (CurrentAttempt == null || offerIndex < 0 || offerIndex >= CurrentAttempt.CurrentOffers.Count)
            {
                return false;
            }

            return SubmitPlayerAction(CurrentAttempt.CurrentOffers[offerIndex].Action.Id, targetCoords);
        }

        public bool SubmitEndTurn(PlayerInputKind inputKind = PlayerInputKind.Ui, string inputBindingId = null)
        {
            if (attemptRunner == null)
            {
                return false;
            }

            attemptRunner.EnqueueInput(PlayerActionRequest.CreateEndTurnRequest(inputKind, inputBindingId));
            return true;
        }

        public bool TrySelectPreviewAction(string actionId)
        {
            return attemptRunner != null && attemptRunner.TrySelectPreviewAction(actionId);
        }

        public bool TrySelectOfferedPreview(int offerIndex)
        {
            if (CurrentAttempt == null || offerIndex < 0 || offerIndex >= CurrentAttempt.CurrentOffers.Count)
            {
                return false;
            }

            return TrySelectPreviewAction(CurrentAttempt.CurrentOffers[offerIndex].Action.Id);
        }

        public void ClearPreviewAction()
        {
            attemptRunner?.ClearSelectedPreviewAction();
        }

        public bool TrySubmitSelectedPreviewToBoard(Vector3 targetCoords)
        {
            return attemptRunner != null && attemptRunner.TrySubmitSelectedPreviewToBoard(targetCoords);
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
                attemptRunner.PreviewActionChanged -= HandlePreviewActionChanged;
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
            OffersUpdated?.Invoke(attempt, attempt.CurrentOffers);
            AwaitingPlayerInput?.Invoke(attempt);

            if (logTurnFlow)
            {
                Debug.Log(FormatAttemptState(attempt));
            }
        }

        private void HandleTurnResolved(Attempt attempt, PlayerActionRequest request, PlayerAction action)
        {
            TurnResolved?.Invoke(attempt, action);

            if (logTurnFlow)
            {
                Debug.Log($"Turn {attempt.World.CurrentTick}: cast {action.DisplayName} using {request.InputKind}.");
            }
        }

        private void HandleAttemptEnded(Attempt attempt)
        {
            AttemptEnded?.Invoke(attempt);

            if (logTurnFlow)
            {
                Debug.Log($"Attempt ended: {attempt.World.Outcome} - {attempt.World.OutcomeReason}");
            }
        }

        private void HandlePreviewActionChanged(Attempt attempt, string actionId)
        {
            PreviewActionChanged?.Invoke(attempt, actionId);
        }

        private void StartPrototypeAttempt()
        {
            PrototypeGameFactory factory = new PrototypeGameFactory(boardWidth, boardHeight, boardSeed);

            GameWorld world = new GameWorld(
                totalTicks,
                startingMana,
                startingVitality,
                startingEssence,
                maxPlayerActionsPerTurn,
                vitalityUpkeepPerTurn);

            StartAttempt(world, factory.CreateBoard(), factory.CreatePlayer(playerName), factory.CreateAutomataRules());
        }

        private static string FormatAttemptState(Attempt attempt)
        {
            GameWorld world = attempt.World;
            string offers = string.Join(", ",
                attempt.CurrentOffers.Select((offer, index) =>
                    $"{index + 1}:{offer.Action.DisplayName} ({offer.UtilityScore:F1})"));
            if (string.IsNullOrWhiteSpace(offers))
            {
                offers = "none";
            }

            string actionPhase = $"Actions {attempt.ActionsResolvedThisTurn}/{world.MaxPlayerActionsPerTurn}";
            string controls = attempt.CanEndTurnEarly || attempt.CurrentOffers.Count == 0
                ? " | 0:End Turn"
                : string.Empty;

            return $"Awaiting input. Turn {world.TurnNumber}/{world.TotalTicks} | {actionPhase} | Mana {world.Mana} | Vitality {world.Vitality} | Essence {world.Essence} | Danger {world.DangerLevel:F2} | Offers {offers}{controls}";
        }
    }
}
