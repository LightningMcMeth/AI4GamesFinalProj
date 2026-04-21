using System.Collections.Generic;
using UnityEngine;

namespace AI4GamesFinalProj.Gameplay
{
    public interface IBoardCell
    {
        Vector3 Coords { get; }

        CellType CellType { get; }

        CellArchetype Archetype { get; }

        CellCondition Condition { get; }

        int CorruptedTurns { get; }

        int BarrierTurns { get; }

        int PurifiedTurns { get; }

        int FrozenTurns { get; }

        int ManaYield { get; }

        int VitalityYield { get; }

        int EssenceYield { get; }

        int LifeSupportValue { get; }

        string VisualKey { get; }

        bool BlocksMovement { get; }

        IReadOnlyDictionary<string, float> StateValues { get; }
    }
}
