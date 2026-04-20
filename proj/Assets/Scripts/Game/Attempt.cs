using System;
using System.Collections.Generic;
using System.Linq;

namespace AI4GamesFinalProj.Gameplay
{
    public sealed class Attempt
    {
        public Attempt(GameWorld world, IGameBoard board, Player player)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            Board = board ?? throw new ArgumentNullException(nameof(board));
            Player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public GameWorld World { get; }

        public IGameBoard Board { get; }

        public Player Player { get; }

        public bool IsComplete => World.HasEnded;

        public IReadOnlyList<PlayerAction> AvailableActions => Player.Actions;

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

        public bool TryGetAction(string actionId, out PlayerAction action)
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                action = null;
                
                return false;
            }

            action = AvailableActions.FirstOrDefault(candidate => string.Equals(candidate.Id, actionId, StringComparison.OrdinalIgnoreCase));

            return action != null;
        }
    }
}
