using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AI4GamesFinalProj.Gameplay
{
    public static class PrototypeSpellbook
    {
        private const int PurifyAreaBaseCellCount = 7;
        private const int PurifyAreaGrowthCycleTurns = 5;

        public static Player CreatePlayer(string playerName)
        {
            Player player = new Player(playerName);
            player.LearnAction(CreateCleanseTile());
            player.LearnAction(CreateHealLand());
            player.LearnAction(CreateFortifyCell());
            player.LearnAction(CreateManaBloom());
            player.LearnAction(CreatePurifyArea());
            player.LearnAction(CreateFreezeSpread());
            player.LearnAction(CreateSacrificeCell());
            return player;
        }

        private static PlayerAction CreateCleanseTile()
        {
            return new PlayerAction(
                "cleanse_tile",
                "Cleanse Tile",
                "Purifies the most dangerous corrupted tile.",
                3,
                0,
                score: context =>
                {
                    BoardCell target = ResolveCleanseTarget(context);
                    if (target == null)
                    {
                        return 0f;
                    }

                    float dangerBonus = context.World.DangerLevel * 15f;

                    return 18f + dangerBonus + BoardAnalysis.ScoreCleanseTarget(context.SquareBoard, target);
                },
                canExecute: context =>
                    context.World.Mana >= 3 &&
                    ResolveCleanseTarget(context) != null,
                execute: context =>
                {
                    if (!context.World.TrySpendMana(3))
                    {
                        return;
                    }

                    BoardCell target = ResolveCleanseTarget(context);
                    target?.Purify();
                    target?.AddPurifiedShield(1);
                });
        }

        private static PlayerAction CreateHealLand()
        {
            return new PlayerAction(
                "heal_land",
                "Heal Land",
                "Restores a dead tile so the territory can regrow there later.",
                2,
                1,
                score: context =>
                {
                    BoardCell target = ResolveRestoreLandTarget(context);
                    if (target == null)
                    {
                        return 0f;
                    }

                    return 10f + BoardAnalysis.ScoreRestoreLandTarget(context.SquareBoard, target);
                },
                canExecute: context =>
                    context.World.Mana >= 2 &&
                    context.World.Essence >= 1 &&
                    ResolveRestoreLandTarget(context) != null,
                execute: context =>
                {
                    if (!context.World.TrySpendMana(2) || !context.World.TrySpendEssence(1))
                    {
                        return;
                    }

                    BoardCell target = ResolveRestoreLandTarget(context);
                    if (target == null)
                    {
                        return;
                    }

                    target.ReviveAs(CellArchetype.HealthyLand);
                    target.AddPurifiedShield(1);
                });
        }

        private static PlayerAction CreateFortifyCell()
        {
            return new PlayerAction(
                "fortify_cell",
                "Fortify Cell",
                "Adds a short defensive barrier to a threatened cell.",
                2,
                1,
                score: context =>
                {
                    BoardCell target = ResolveFortifyTarget(context);
                    if (target == null)
                    {
                        return 0f;
                    }

                    float preventionBonus = context.World.DangerLevel < 0.5f ? 10f : 2f;

                    return 14f +
                        preventionBonus +
                        BoardAnalysis.CountCorruptedNeighbors(context.SquareBoard, target) * 4f +
                        BoardAnalysis.CountAdjacentRoots(context.SquareBoard, target) * 5f;
                },
                canExecute: context =>
                    context.World.Mana >= 2 &&
                    context.World.Essence >= 1 &&
                    ResolveFortifyTarget(context) != null,
                execute: context =>
                {
                    if (!context.World.TrySpendMana(2) || !context.World.TrySpendEssence(1))
                    {
                        return;
                    }

                    BoardCell target = ResolveFortifyTarget(context);
                    target?.AddBarrier(6);
                });
        }

        private static PlayerAction CreateManaBloom()
        {
            return new PlayerAction(
                "mana_bloom",
                "Mana Bloom",
                "Turns a healthy cell into a Mana Spring.",
                4,
                4,
                score: context =>
                {
                    BoardCell target = ResolveManaBloomTarget(context);
                    if (target == null)
                    {
                        return 0f;
                    }

                    float manaMissingRatio = 1f - ((float)context.World.Mana / context.World.MaxMana);
                    float economyNeed = manaMissingRatio * 18f;

                    float earlyGameBonus = context.World.TurnNumber <= 8 ? 10f : 0f;
                    float safeBoardBonus = context.World.DangerLevel <= 0.35f ? 8f : 0f;
                    float dangerPenalty = context.World.DangerLevel > 0.55f ? 20f : 0f;

                    float targetQuality =
                        BoardAnalysis.CountAdjacentRoots(context.SquareBoard, target) * 4f +
                        BoardAnalysis.CountSacredNeighbors(context.SquareBoard, target) * 2f -
                        BoardAnalysis.CountCorruptedNeighbors(context.SquareBoard, target) * 5f;

                    return 8f + economyNeed + earlyGameBonus + safeBoardBonus + targetQuality - dangerPenalty;
                },
                canExecute: context =>
                    context.World.Mana >= 4 &&
                    context.World.Essence >= 4 &&
                    context.World.DangerLevel <= 0.65f &&
                    ResolveManaBloomTarget(context) != null,
                execute: context =>
                {
                    if (!context.World.TrySpendMana(4) || !context.World.TrySpendEssence(4))
                    {
                        return;
                    }

                    BoardCell target = ResolveManaBloomTarget(context);
                    if (target == null)
                    {
                        return;
                    }

                    target.TransformTo(CellArchetype.ManaSpring);
                    target.Purify();
                    target.AddBarrier(1);
                });
        }

        private static PlayerAction CreatePurifyArea()
        {
            return new PlayerAction(
                "purify_area",
                "Purify Area",
                "Cleanses a cluster of nearby corruption.",
                6,
                2,
                score: context =>
                {
                    BoardCell target = ResolvePurifyAreaTarget(context);
                    if (target == null)
                    {
                        return 0f;
                    }

                    float areaScore = ScorePurifyAreaTarget(context.Attempt, target);
                    float dangerBonus = context.World.DangerLevel * 20f;
                    float resourcePenalty = context.World.Mana <= 8 ? 8f : 0f;

                    return 8f + areaScore + dangerBonus - resourcePenalty;
                },
                canExecute: context =>
                {
                    BoardCell target = ResolvePurifyAreaTarget(context);

                    return context.World.Mana >= 6 &&
                        context.World.Essence >= 2 &&
                        context.World.DangerLevel >= 0.35f &&
                        target != null &&
                        ScorePurifyAreaTarget(context.Attempt, target) >= 10f;
                },
                execute: context =>
                {
                    if (!context.World.TrySpendMana(6) || !context.World.TrySpendEssence(2))
                    {
                        return;
                    }

                    BoardCell target = ResolvePurifyAreaTarget(context);
                    if (target == null)
                    {
                        return;
                    }

                    foreach (BoardCell cell in GetPurifyAreaCells(context.Attempt, target.Coords))
                    {
                        cell.Purify();
                        cell.AddPurifiedShield(1);
                    }
                });
        }

        private static PlayerAction CreateFreezeSpread()
        {
            return new PlayerAction(
                "freeze_spread",
                "Freeze Spread",
                "Temporarily freezes a corrupted cluster so it cannot spread.",
                2,
                0,
                score: context =>
                {
                    BoardCell target = ResolveFreezeTarget(context);
                    if (target == null)
                    {
                        return 0f;
                    }

                    int corruptedNeighbors = BoardAnalysis.CountCorruptedNeighbors(context.SquareBoard, target);
                    int adjacentRoots = BoardAnalysis.CountAdjacentRoots(context.SquareBoard, target);
                    int adjacentSprings = BoardAnalysis.CountAdjacentSprings(context.SquareBoard, target);

                    float dangerBonus = context.World.DangerLevel * 18f;
                    float earlyPenalty = context.World.TurnNumber <= 3 ? 10f : 0f;

                    return 6f +
                        dangerBonus +
                        corruptedNeighbors * 3f +
                        adjacentRoots * 6f +
                        adjacentSprings * 3f -
                        target.FrozenTurns * 8f -
                        earlyPenalty;
                },
                canExecute: context =>
                {
                    BoardCell target = ResolveFreezeTarget(context);

                    return context.World.Mana >= 2 &&
                        target != null &&
                        target.FrozenTurns <= 0 &&
                        (
                            context.World.DangerLevel >= 0.25f ||
                            BoardAnalysis.CountAdjacentRoots(context.SquareBoard, target) > 0 ||
                            BoardAnalysis.CountAdjacentSprings(context.SquareBoard, target) > 0
                        );
                },
                execute: context =>
                {
                    if (!context.World.TrySpendMana(2))
                    {
                        return;
                    }

                    BoardCell target = ResolveFreezeTarget(context);
                    if (target == null)
                    {
                        return;
                    }

                    foreach (BoardCell cell in context.SquareBoard.GetCellsInRadius(target.Coords, 1).Where(cell => cell.IsCorrupted))
                    {
                        cell.Freeze(2);
                    }
                });
        }

        private static PlayerAction CreateSacrificeCell()
        {
            return new PlayerAction(
                "consecrate_cell",
                "Consecrate Cell",
                "Transforms one non-core tile into a Sacred Cell.",
                0,
                2,
                score: context =>
                {
                    BoardCell target = ResolveSacrificeTarget(context);
                    if (target == null)
                    {
                        return 0f;
                    }

                    return context.World.DangerLevel * 20f +
                        BoardAnalysis.CountCorruptedNeighbors(context.SquareBoard, target) * 3f;
                },
                canExecute: context =>
                    context.World.Essence >= 2 &&
                    ResolveSacrificeTarget(context) != null,
                execute: context =>
                {
                    if (!context.World.TrySpendEssence(2))
                    {
                        return;
                    }

                    BoardCell target = ResolveSacrificeTarget(context);
                    if (target == null)
                    {
                        return;
                    }

                    target.Purify();
                    target.TransformTo(CellArchetype.SacredSite);
                    target.AddBarrier(2);
                });
        }

        private static BoardCell ResolveCleanseTarget(PlayerActionContext context)
        {
            return ResolveTarget(
                context,
                cell => cell.IsCorrupted,
                static attempt => BoardAnalysis.FindBestCleanseTarget(attempt));
        }

        private static BoardCell ResolveFortifyTarget(PlayerActionContext context)
        {
            return ResolveTarget(
                context,
                cell => cell.IsStable && !cell.IsDead,
                static attempt => BoardAnalysis.FindBestFortifyTarget(attempt));
        }

        private static BoardCell ResolveManaBloomTarget(PlayerActionContext context)
        {
            return ResolveTarget(
                context,
                cell => cell.IsStable && cell.Archetype == CellArchetype.HealthyLand,
                static attempt => BoardAnalysis.FindBestManaBloomTarget(attempt));
        }

        private static BoardCell ResolvePurifyAreaTarget(PlayerActionContext context)
        {
            return ResolveTarget(
                context,
                cell => IsPurifyAreaTargetValid(context.Attempt, cell),
                FindBestPurifyAreaTarget);
        }

        private static BoardCell ResolveFreezeTarget(PlayerActionContext context)
        {
            return ResolveTarget(
                context,
                cell => cell.IsCorrupted,
                static attempt => BoardAnalysis.FindBestFreezeTarget(attempt));
        }

        private static BoardCell ResolveSacrificeTarget(PlayerActionContext context)
        {
            return ResolveTarget(
                context,
                cell =>
                    cell.Archetype != CellArchetype.LifeRoot &&
                    cell.Archetype != CellArchetype.SacredSite &&
                    !cell.IsDead,
                static attempt => BoardAnalysis.FindBestSacrificeTarget(attempt));
        }

        private static BoardCell ResolveRestoreLandTarget(PlayerActionContext context)
        {
            return ResolveTarget(
                context,
                cell => cell.Type == CellType.DeadCell,
                static attempt => BoardAnalysis.FindBestRestoreLandTarget(attempt));
        }

        public static IReadOnlyList<BoardCell> GetPurifyAreaCells(Attempt attempt, Vector3 centerCoords)
        {
            if (attempt?.Board is not SquareGameBoard board)
            {
                return Array.Empty<BoardCell>();
            }

            return GetPurifyAreaCells(board, centerCoords, attempt.World.CurrentTick);
        }

        public static IReadOnlyList<BoardCell> GetPurifyAreaCells(
            SquareGameBoard board,
            Vector3 centerCoords,
            int turnsElapsed)
        {
            if (board == null)
            {
                return Array.Empty<BoardCell>();
            }

            BoardCell centerCell = board.GetBoardCell(centerCoords);
            if (centerCell == null)
            {
                return Array.Empty<BoardCell>();
            }

            int targetCount = PurifyAreaBaseCellCount + Math.Abs(turnsElapsed) % PurifyAreaGrowthCycleTurns;
            List<BoardCell> areaCells = new List<BoardCell>(targetCount);
            HashSet<BoardCell> included = new HashSet<BoardCell>();

            TryAddAreaCell(areaCells, included, centerCell);
            TryAddAreaCell(areaCells, included, board.GetCell(centerCell.X - 1, centerCell.Y));
            TryAddAreaCell(areaCells, included, board.GetCell(centerCell.X + 1, centerCell.Y));
            TryAddAreaCell(areaCells, included, board.GetCell(centerCell.X, centerCell.Y - 1));
            TryAddAreaCell(areaCells, included, board.GetCell(centerCell.X, centerCell.Y + 1));

            System.Random random = CreatePurifyAreaRandom(board, turnsElapsed);
            for (int ring = 1; areaCells.Count < targetCount && ring <= Math.Max(board.Width, board.Height); ring++)
            {
                List<BoardCell> ringCandidates = board.AllCells
                    .Where(cell => !included.Contains(cell) && GetChebyshevDistance(centerCell, cell) == ring)
                    .ToList();

                Shuffle(ringCandidates, random);
                foreach (BoardCell candidate in ringCandidates)
                {
                    TryAddAreaCell(areaCells, included, candidate);
                    if (areaCells.Count >= targetCount)
                    {
                        break;
                    }
                }
            }

            return areaCells;
        }

        public static bool IsPurifyAreaTargetValid(Attempt attempt, BoardCell centerCell)
        {
            if (attempt?.Board is not SquareGameBoard board || centerCell == null || centerCell.IsDead)
            {
                return false;
            }

            IReadOnlyList<BoardCell> areaCells = GetPurifyAreaCells(attempt, centerCell.Coords);
            if (areaCells.Count == 0)
            {
                return false;
            }

            foreach (BoardCell cell in areaCells)
            {
                if (cell.IsCorrupted)
                {
                    return true;
                }

                if (board.GetNeighbors(cell).Any(neighbor => neighbor.IsCorrupted))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryResolveExplicitTarget(PlayerActionContext context, out BoardCell cell)
        {
            return context.TryGetTargetCell(out cell);
        }

        private static BoardCell ResolveTarget(
            PlayerActionContext context,
            Func<BoardCell, bool> explicitTargetValidator,
            Func<Attempt, BoardCell> fallbackResolver)
        {
            if (TryResolveExplicitTarget(context, out BoardCell explicitCell))
            {
                return explicitTargetValidator(explicitCell) ? explicitCell : null;
            }

            return fallbackResolver(context.Attempt);
        }

        private static BoardCell FindBestPurifyAreaTarget(Attempt attempt)
        {
            if (attempt?.Board is not SquareGameBoard board)
            {
                return null;
            }

            return board.AllCells
                .Where(cell => !cell.IsDead)
                .OrderByDescending(cell => ScorePurifyAreaTarget(attempt, cell))
                .FirstOrDefault(cell => ScorePurifyAreaTarget(attempt, cell) > 0f);
        }

        private static float ScorePurifyAreaTarget(Attempt attempt, BoardCell centerCell)
        {
            if (!IsPurifyAreaTargetValid(attempt, centerCell) || attempt?.Board is not SquareGameBoard board)
            {
                return 0f;
            }

            HashSet<BoardCell> threatenedCorruption = new HashSet<BoardCell>();
            foreach (BoardCell areaCell in GetPurifyAreaCells(attempt, centerCell.Coords))
            {
                if (areaCell.IsCorrupted)
                {
                    threatenedCorruption.Add(areaCell);
                }

                foreach (BoardCell neighbor in board.GetNeighbors(areaCell).Where(neighbor => neighbor.IsCorrupted))
                {
                    threatenedCorruption.Add(neighbor);
                }
            }

            return threatenedCorruption.Sum(cell =>
                4f +
                BoardAnalysis.CountAdjacentRoots(board, cell) * 3f +
                BoardAnalysis.CountAdjacentSprings(board, cell) * 2f +
                cell.CorruptedTurns);
        }

        private static void TryAddAreaCell(
            ICollection<BoardCell> areaCells,
            ISet<BoardCell> included,
            BoardCell candidate)
        {
            if (candidate == null || !included.Add(candidate))
            {
                return;
            }

            areaCells.Add(candidate);
        }

        private static int GetChebyshevDistance(BoardCell centerCell, BoardCell otherCell)
        {
            return Math.Max(
                Math.Abs(centerCell.X - otherCell.X),
                Math.Abs(centerCell.Y - otherCell.Y));
        }

        private static System.Random CreatePurifyAreaRandom(
            SquareGameBoard board,
            int turnsElapsed)
        {
            int hash = board.Seed;
            hash = unchecked(hash * 397) ^ turnsElapsed;
            return new System.Random(hash);
        }

        private static void Shuffle<T>(IList<T> values, System.Random random)
        {
            for (int index = values.Count - 1; index > 0; index--)
            {
                int swapIndex = random.Next(index + 1);
                (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
            }
        }
    }
}
