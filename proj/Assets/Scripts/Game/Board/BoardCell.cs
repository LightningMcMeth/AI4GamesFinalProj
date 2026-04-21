using System.Collections.Generic;
using UnityEngine;

namespace AI4GamesFinalProj.Gameplay
{
    public sealed class BoardCell : IBoardCell
    {
        public BoardCell(Vector3 coords, CellArchetype archetype)
        {
            Coords = coords;
            Archetype = archetype;
            NextType = CellType;
        }

        public Vector3 Coords { get; }

        public CellArchetype Archetype { get; private set; }

        public CellCondition Condition { get; private set; } = CellCondition.Stable;

        public CellType NextType { get; private set; }

        public int X => Mathf.RoundToInt(Coords.x);

        public int Y => Mathf.RoundToInt(Coords.y);

        public int CorruptedTurns { get; private set; }

        public int BarrierTurns { get; private set; }

        public int ShieldTurns => BarrierTurns;

        public int PurifiedTurns { get; private set; }

        public int FrozenTurns { get; private set; }

        public bool BlocksMovement => false;

        public bool IsStable => Condition == CellCondition.Stable;

        public bool IsCorrupted => Condition == CellCondition.Corrupted;

        public bool IsDead => Condition == CellCondition.Dead;

        public CellType Type => CellType;

        public string VisualKey => CellType.ToString();

        public int ManaYield => IsStable ? CellTypeRules.For(CellType).ManaOutput : 0;

        public int VitalityYield => IsStable ? CellTypeRules.For(CellType).LifeOutput : 0;

        public int EssenceYield => IsStable && Archetype == CellArchetype.SacredSite ? 1 : 0;

        public int LifeSupportValue => IsStable && CellType == CellType.HealthyLand ? 1 : 0;

        public CellType CellType
        {
            get
            {
                if (IsDead)
                {
                    return CellType.DeadCell;
                }

                if (IsCorrupted)
                {
                    return CellType.CorruptedLand;
                }

                return Archetype switch
                {
                    CellArchetype.ManaSpring => CellType.ManaSpring,
                    CellArchetype.LifeRoot => CellType.LifeRoot,
                    CellArchetype.SacredSite => CellType.SacredCell,
                    _ => CellType.HealthyLand
                };
            }
        }

        public IReadOnlyDictionary<string, float> StateValues =>
            new Dictionary<string, float>
            {
                ["is_corrupted"] = IsCorrupted ? 1f : 0f,
                ["is_dead"] = IsDead ? 1f : 0f,
                ["corrupted_turns"] = CorruptedTurns,
                ["barrier_turns"] = BarrierTurns,
                ["purified_turns"] = PurifiedTurns,
                ["frozen_turns"] = FrozenTurns,
                ["mana_yield"] = ManaYield,
                ["vitality_yield"] = VitalityYield,
                ["essence_yield"] = EssenceYield,
                ["life_support"] = LifeSupportValue
            };

        public void TransformTo(CellArchetype archetype)
        {
            if (IsDead)
            {
                return;
            }

            Archetype = archetype;
        }

        public void ReviveAs(CellArchetype archetype)
        {
            Archetype = archetype;
            Condition = CellCondition.Stable;
            CorruptedTurns = 0;
            BarrierTurns = 0;
            PurifiedTurns = 0;
            FrozenTurns = 0;
            NextType = CellType;
        }

        public int GetCorruptionThreshold(int sacredSupport)
        {
            int threshold = Archetype switch
            {
                CellArchetype.HealthyLand => 3,
                CellArchetype.ManaSpring => 4,
                CellArchetype.SacredSite => 4,
                CellArchetype.LifeRoot => int.MaxValue,
                _ => 3
            };

            if (Archetype == CellArchetype.HealthyLand && sacredSupport >= 2)
            {
                threshold += 1;
            }

            return threshold;
        }

        public void MarkCorrupted()
        {
            if (IsDead)
            {
                return;
            }

            if (!IsCorrupted)
            {
                Condition = CellCondition.Corrupted;
                CorruptedTurns = 0;
            }

            PurifiedTurns = 0;
            NextType = CellType;
        }

        public void Purify()
        {
            if (IsDead)
            {
                return;
            }

            Condition = CellCondition.Stable;
            CorruptedTurns = 0;
            FrozenTurns = 0;
            NextType = CellType;
        }

        public void MakeDead()
        {
            Condition = CellCondition.Dead;
            CorruptedTurns = 0;
            BarrierTurns = 0;
            PurifiedTurns = 0;
            FrozenTurns = 0;
            NextType = CellType;
        }

        public void AddBarrier(int turns)
        {
            BarrierTurns = Mathf.Max(BarrierTurns, turns);
        }

        public void AddPurifiedShield(int turns)
        {
            PurifiedTurns = Mathf.Max(PurifiedTurns, turns);
        }

        public void Freeze(int turns)
        {
            FrozenTurns = Mathf.Max(FrozenTurns, turns);
        }

        public void AdvanceTurn()
        {
            if (BarrierTurns > 0)
            {
                BarrierTurns--;
            }

            if (PurifiedTurns > 0)
            {
                PurifiedTurns--;
            }

            if (FrozenTurns > 0)
            {
                FrozenTurns--;
            }
        }

        public bool IsAlive()
        {
            return !IsDead;
        }

        public void ResetTemporaryState()
        {
            BarrierTurns = 0;
            PurifiedTurns = 0;
            FrozenTurns = 0;
            NextType = CellType;
        }

        public void SetNextType(CellType nextType)
        {
            NextType = nextType;
        }

        public void ApplyNextState()
        {
            ApplyResolvedType(NextType);
            NextType = CellType;
        }

        public void IncrementCorruptionTurn()
        {
            if (IsCorrupted)
            {
                CorruptedTurns++;
            }
        }

        private void ApplyResolvedType(CellType resolvedType)
        {
            switch (resolvedType)
            {
                case CellType.CorruptedLand:
                    MarkCorrupted();
                    IncrementCorruptionTurn();
                    break;
                case CellType.ManaSpring:
                    ReviveAs(CellArchetype.ManaSpring);
                    break;
                case CellType.LifeRoot:
                    ReviveAs(CellArchetype.LifeRoot);
                    break;
                case CellType.SacredCell:
                    ReviveAs(CellArchetype.SacredSite);
                    break;
                case CellType.DeadCell:
                    MakeDead();
                    break;
                default:
                    ReviveAs(CellArchetype.HealthyLand);
                    break;
            }
        }
    }
}
