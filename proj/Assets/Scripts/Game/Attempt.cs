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

        public IEnumerable<PlayerAction> GetExecutableActions()
        {
            return AvailableActions.Where(action => action.CanExecute(this));
        }

        public void Run()
        {
            
        }
    }
}
