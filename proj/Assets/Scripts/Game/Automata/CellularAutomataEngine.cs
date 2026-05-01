using System;

namespace AI4GamesFinalProj.Gameplay
{
    public sealed class CellularAutomataEngine
    {
        private readonly ICellularAutomataRules rules;

        public CellularAutomataEngine(ICellularAutomataRules rules)
        {
            this.rules = rules ?? NoOpCellularAutomataRules.Instance;
        }

        public void Step(Attempt attempt)
        {
            if (attempt == null)
            {
                throw new ArgumentNullException(nameof(attempt));
            }

            rules.ApplyStep(attempt);
        }

        private sealed class NoOpCellularAutomataRules : ICellularAutomataRules
        {
            public static NoOpCellularAutomataRules Instance { get; } = new NoOpCellularAutomataRules();

            public void ApplyStep(Attempt attempt)
            {
            }
        }
    }
}
