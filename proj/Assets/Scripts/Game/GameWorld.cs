using System;
using UnityEngine;

namespace AI4GamesFinalProj.Gameplay
{
    public sealed class GameWorld
    {
        public GameWorld(
            int totalTicks,
            int startingMana,
            int startingVitality,
            int startingEssence,
            int maxPlayerActionsPerTurn = 2,
            int vitalityUpkeepPerTurn = 1)
        {
            if (totalTicks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalTicks), "Total ticks must be greater than zero.");
            }

            if (startingVitality <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingVitality), "Starting vitality must be greater than zero.");
            }

            if (maxPlayerActionsPerTurn <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxPlayerActionsPerTurn), "At least one action must be allowed each turn.");
            }

            if (vitalityUpkeepPerTurn < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(vitalityUpkeepPerTurn));
            }

            TotalTicks = totalTicks;
            Mana = Math.Max(0, startingMana);
            Vitality = startingVitality;
            Essence = Math.Max(0, startingEssence);
            MaxPlayerActionsPerTurn = maxPlayerActionsPerTurn;
            VitalityUpkeepPerTurn = vitalityUpkeepPerTurn;
        }

        public int CurrentTick { get; private set; }

        public int TurnNumber => Math.Min(CurrentTick + 1, TotalTicks);

        public int TotalTicks { get; }

        public int MaxPlayerActionsPerTurn { get; }

        public int VitalityUpkeepPerTurn { get; }

        public int Mana { get; private set; }

        public int Vitality { get; private set; }

        public int Essence { get; private set; }

        public bool WizardAlive { get; private set; } = true;

        public int LifeRootsRemaining { get; private set; }

        public int ManaIncomeLastTurn { get; private set; }

        public int VitalityIncomeLastTurn { get; private set; }

        public int EssenceIncomeLastTurn { get; private set; }

        public float DangerLevel { get; private set; }

        public string OutcomeReason { get; private set; } = string.Empty;

        public int TicksRemaining => Math.Max(TotalTicks - CurrentTick, 0);

        public bool HasEnded => Outcome != GameOutcome.InProgress;

        public bool HasWon => Outcome == GameOutcome.Won;

        public bool HasLost => Outcome == GameOutcome.Lost;

        public GameOutcome Outcome { get; private set; } = GameOutcome.InProgress;

        public float NormalizedTimeRemaining => (float)TicksRemaining / TotalTicks;

        public bool NextTurn()
        {
            if (HasEnded || CurrentTick >= TotalTicks)
            {
                return false;
            }

            CurrentTick++;
            return true;
        }

        public bool TryAdvanceTick()
        {
            return NextTurn();
        }

        public bool TrySpendMana(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (Mana < amount)
            {
                return false;
            }

            Mana -= amount;
            return true;
        }

        public bool TrySpendEssence(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (Essence < amount)
            {
                return false;
            }

            Essence -= amount;
            return true;
        }

        public void RegisterTurnIncome(int manaIncome, int vitalityIncome, int essenceIncome)
        {
            ManaIncomeLastTurn = manaIncome;
            VitalityIncomeLastTurn = vitalityIncome;
            EssenceIncomeLastTurn = essenceIncome;

            Mana = Math.Max(0, Mana + manaIncome);
            Vitality = Math.Max(0, Vitality + vitalityIncome);
            Essence = Math.Max(0, Essence + essenceIncome);
        }

        public void CollectResources(SquareGameBoard board)
        {
            if (board == null)
            {
                return;
            }

            int manaIncome = 0;
            int vitalityIncome = 0;
            int essenceIncome = 0;

            foreach (BoardCell cell in board.AllCells)
            {
                if (!cell.IsAlive() || cell.IsCorrupted)
                {
                    continue;
                }

                manaIncome += cell.ManaYield;
                vitalityIncome += cell.VitalityYield;
                essenceIncome += cell.EssenceYield;
            }

            LifeRootsRemaining = board.CountAliveCellsByType(CellType.LifeRoot);
            RegisterTurnIncome(
                manaIncome,
                vitalityIncome - VitalityUpkeepPerTurn,
                essenceIncome);
        }

        public void SetDangerLevel(float dangerLevel)
        {
            DangerLevel = Mathf.Clamp01(dangerLevel);
        }

        public void Lose(string reason)
        {
            Outcome = GameOutcome.Lost;
            OutcomeReason = reason ?? string.Empty;
            WizardAlive = false;
        }

        public void Win(string reason)
        {
            if (Outcome == GameOutcome.Lost)
            {
                return;
            }

            Outcome = GameOutcome.Won;
            OutcomeReason = reason ?? string.Empty;
        }

        public bool CheckLoseCondition()
        {
            WizardAlive = LifeRootsRemaining > 0 && Vitality > 0;
            if (!WizardAlive)
            {
                string reason = LifeRootsRemaining <= 0
                    ? "All Life Roots have been lost."
                    : "Vitality reached zero.";
                Lose(reason);
            }

            return !WizardAlive;
        }

        public void UpdateLifeRootsRemaining(int remaining)
        {
            LifeRootsRemaining = Math.Max(0, remaining);
            WizardAlive = LifeRootsRemaining > 0 && Vitality > 0;
        }
    }

    public enum GameOutcome
    {
        InProgress = 0,
        Won = 1,
        Lost = 2
    }
}
