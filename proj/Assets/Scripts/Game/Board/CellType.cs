namespace AI4GamesFinalProj.Gameplay
{
    public enum CellType
    {
        HealthyLand = 0,
        CorruptedLand = 1,
        ManaSpring = 2,
        LifeRoot = 3,
        SacredCell = 4,
        DeadCell = 5
    }

    public enum CellArchetype
    {
        HealthyLand = 0,
        ManaSpring = 1,
        LifeRoot = 2,
        SacredSite = 3
    }

    public enum CellCondition
    {
        Stable = 0,
        Corrupted = 1,
        Dead = 2
    }
}
