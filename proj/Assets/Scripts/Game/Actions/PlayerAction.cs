using System;

namespace AI4GamesFinalProj.Gameplay
{
    public sealed class PlayerAction
    {
        private readonly Func<PlayerActionContext, bool> canExecute;
        private readonly Func<PlayerActionContext, float> score;
        private readonly Action<PlayerActionContext> execute;

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int ManaCost { get; }
        public int EssenceCost { get; }

        public PlayerAction(
            string id,
            string displayName,
            string description,
            int manaCost,
            int essenceCost,
            Func<PlayerActionContext, float> score,
            Func<PlayerActionContext, bool> canExecute,
            Action<PlayerActionContext> execute)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Action id cannot be empty.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
            }

            Id = id;
            DisplayName = displayName;
            Description = description ?? string.Empty;
            ManaCost = Math.Max(0, manaCost);
            EssenceCost = Math.Max(0, essenceCost);

            this.score = score ?? NoUtility;
            this.canExecute = canExecute ?? AlwaysAvailable;
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public bool CanExecute(PlayerActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return canExecute(context);
        }

        public float Score(PlayerActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return Math.Max(0f, score(context));
        }

        public void Apply(PlayerActionContext context)
        {
            if (!CanExecute(context))
            {
                throw new InvalidOperationException($"Action '{DisplayName}' cannot be executed right now.");
            }

            execute(context);
        }

        private static bool AlwaysAvailable(PlayerActionContext context)
        {
            return true;
        }

        private static float NoUtility(PlayerActionContext context)
        {
            return 0f;
        }
    }
}
