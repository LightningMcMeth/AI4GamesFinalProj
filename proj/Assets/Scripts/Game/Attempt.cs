using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AI4GamesFinalProj.Gameplay
{
    public sealed class Attempt
    {
        private readonly List<PlayerActionOffer> currentOffers = new List<PlayerActionOffer>();

        public Attempt(GameWorld world, IGameBoard board, Player player)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            Board = board ?? throw new ArgumentNullException(nameof(board));
            Player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public GameWorld World { get; }

        public IGameBoard Board { get; }

        public Player Player { get; }

        public bool IsComplete => World.HasEnded || World.CurrentTick >= World.TotalTicks;

        public int ActionsResolvedThisTurn { get; private set; }

        public bool IsActionPhaseActive { get; private set; }

        public bool CanResolveMoreActions =>
            IsActionPhaseActive && ActionsResolvedThisTurn < World.MaxPlayerActionsPerTurn;

        public bool CanEndTurnEarly => IsActionPhaseActive && ActionsResolvedThisTurn > 0;

        public IReadOnlyList<PlayerAction> AvailableActions => Player.Actions;

        public IReadOnlyList<PlayerActionOffer> CurrentOffers => currentOffers;

        public IEnumerable<BoardCell> Cells => Board.Cells.OfType<BoardCell>();

        public IEnumerable<PlayerAction> GetExecutableActions(PlayerActionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return AvailableActions.Where(action => action.CanExecute(CreateActionContext(request.ForAction(action.Id))));
        }

        public PlayerActionContext CreateActionContext(PlayerActionRequest request)
        {
            return new PlayerActionContext(this, request);
        }

        public void SetCurrentOffers(IEnumerable<PlayerActionOffer> offers)
        {
            currentOffers.Clear();
            if (offers == null)
            {
                return;
            }

            currentOffers.AddRange(offers);
        }

        public bool TryGetAction(string actionId, out PlayerAction action)
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                action = null;
                return false;
            }

            action = AvailableActions.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, actionId, StringComparison.OrdinalIgnoreCase));

            return action != null;
        }

        public bool TryGetCell(Vector3 coords, out BoardCell cell)
        {
            cell = Board.GetCell(coords) as BoardCell;
            return cell != null;
        }

        public void BeginActionPhase()
        {
            ActionsResolvedThisTurn = 0;
            IsActionPhaseActive = true;
        }

        public void RegisterResolvedAction(PlayerAction action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (!IsActionPhaseActive)
            {
                return;
            }

            ActionsResolvedThisTurn++;
        }

        public void EndActionPhase()
        {
            IsActionPhaseActive = false;
        }
    }
}
