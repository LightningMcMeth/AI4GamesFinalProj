namespace AI4GamesFinalProj.Gameplay
{
    public readonly struct CellTypeRule
    {
        public CellTypeRule(
            bool corruptible,
            int baseThreshold,
            int manaOutput,
            int lifeOutput,
            int spreadCount,
            int corruptionLifetime,
            bool diesFromCorruptedNeighbor,
            int resistanceBonus,
            bool immutable)
        {
            Corruptible = corruptible;
            BaseThreshold = baseThreshold;
            ManaOutput = manaOutput;
            LifeOutput = lifeOutput;
            SpreadCount = spreadCount;
            CorruptionLifetime = corruptionLifetime;
            DiesFromCorruptedNeighbor = diesFromCorruptedNeighbor;
            ResistanceBonus = resistanceBonus;
            Immutable = immutable;
        }

        public bool Corruptible { get; }

        public int BaseThreshold { get; }

        public int ManaOutput { get; }

        public int LifeOutput { get; }

        public int SpreadCount { get; }

        public int CorruptionLifetime { get; }

        public bool DiesFromCorruptedNeighbor { get; }

        public int ResistanceBonus { get; }

        public bool Immutable { get; }
    }

    public static class CellTypeRules
    {
        public static CellTypeRule For(CellType type)
        {
            return type switch
            {
                CellType.HealthyLand => new CellTypeRule(true, 3, 0, 0, 0, 0, false, 0, false),
                CellType.CorruptedLand => new CellTypeRule(false, 0, 0, 0, 2, 3, false, 0, false),
                CellType.ManaSpring => new CellTypeRule(true, 4, 1, 0, 0, 0, false, 0, false),
                CellType.LifeRoot => new CellTypeRule(false, 0, 0, 1, 0, 0, true, 0, false),
                CellType.SacredCell => new CellTypeRule(true, 3, 0, 0, 0, 0, false, 1, false),
                CellType.DeadCell => new CellTypeRule(false, 0, 0, 0, 0, 0, false, 0, true),
                _ => new CellTypeRule(true, 3, 0, 0, 0, 0, false, 0, false)
            };
        }
    }
}
