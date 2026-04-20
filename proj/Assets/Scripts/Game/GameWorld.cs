using System;

namespace AI4GamesFinalProj.Gameplay
{
    public sealed class GameWorld
    {
        public GameWorld(int totalTicks)
        {
            if (totalTicks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalTicks), "Total ticks must be greater than zero.");
            }

            TotalTicks = totalTicks;
        }

        public int CurrentTick { get; private set; }

        public int TotalTicks { get; }

        public int TicksRemaining => TotalTicks - CurrentTick;

        public bool HasEnded => CurrentTick >= TotalTicks;

        public float NormalizedTimeRemaining => (float)TicksRemaining / TotalTicks;

        public bool TryAdvanceTick()
        {
            if (HasEnded)
            {
                return false;
            }

            CurrentTick++;
            return true;
        }
    }
}
