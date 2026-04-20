using System;

namespace AI4GamesFinalProj.Gameplay
{
    public sealed class PlayerAction
    {
        private readonly Func<Attempt, bool> canExecute;
        private readonly Action<Attempt> execute;

        public PlayerAction(string id, string displayName, string description, Func<Attempt, bool> canExecute, Action<Attempt> execute)
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
            this.canExecute = canExecute ?? AlwaysAvailable;
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public string Id { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public bool CanExecute(Attempt attempt)
        {
            if (attempt == null)
            {
                throw new ArgumentNullException(nameof(attempt));
            }

            return canExecute(attempt);
        }

        public void Apply(Attempt attempt)
        {
            if (!CanExecute(attempt))
            {
                throw new InvalidOperationException($"Action '{DisplayName}' cannot be executed right now.");
            }

            execute(attempt);
        }

        private static bool AlwaysAvailable(Attempt attempt)
        {
            return true;
        }
    }
}
