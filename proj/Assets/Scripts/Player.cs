using System;
using System.Collections.Generic;
using System.Linq;

namespace AI4GamesFinalProj.Gameplay
{
    public sealed class Player
    {
        public string Name { get; }
        private readonly List<PlayerAction> actions = new List<PlayerAction>();

        public Player(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Player name cannot be empty.", nameof(name));
            }

            Name = name;
        }


        public IReadOnlyList<PlayerAction> Actions => actions;

        public void LearnAction(PlayerAction action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (actions.Any(existingAction => existingAction.Id == action.Id))
            {
                return;
            }

            actions.Add(action);
        }
    }
}
