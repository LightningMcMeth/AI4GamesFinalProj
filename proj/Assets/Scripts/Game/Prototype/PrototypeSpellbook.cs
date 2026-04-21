using System.Linq;

namespace AI4GamesFinalProj.Gameplay
{
    public static class PrototypeSpellbook
    {
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

                    return 30f + BoardAnalysis.ScoreCleanseTarget(context.SquareBoard, target);
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
                0,
                score: context =>
                {
                    BoardCell target = ResolveFortifyTarget(context);
                    if (target == null)
                    {
                        return 0f;
                    }

                    return 18f +
                        BoardAnalysis.CountCorruptedNeighbors(context.SquareBoard, target) * 4f +
                        BoardAnalysis.CountAdjacentRoots(context.SquareBoard, target) * 5f;
                },
                canExecute: context =>
                    context.World.Mana >= 2 &&
                    ResolveFortifyTarget(context) != null,
                execute: context =>
                {
                    if (!context.World.TrySpendMana(2))
                    {
                        return;
                    }

                    BoardCell target = ResolveFortifyTarget(context);
                    target?.AddBarrier(2);
                });
        }

        private static PlayerAction CreateManaBloom()
        {
            return new PlayerAction(
                "mana_bloom",
                "Mana Bloom",
                "Turns a healthy cell into a Mana Spring.",
                4,
                0,
                score: context =>
                {
                    BoardCell target = ResolveManaBloomTarget(context);
                    if (target == null)
                    {
                        return 0f;
                    }

                    float manaPressure = context.World.Mana <= 3 ? 14f : 6f;
                    return manaPressure + (context.World.DangerLevel < 0.6f ? 8f : 0f);
                },
                canExecute: context =>
                    context.World.Mana >= 4 &&
                    ResolveManaBloomTarget(context) != null,
                execute: context =>
                {
                    if (!context.World.TrySpendMana(4))
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
                0,
                score: context =>
                {
                    BoardCell target = ResolvePurifyAreaTarget(context);
                    if (target == null)
                    {
                        return 0f;
                    }

                    return 12f + BoardAnalysis.ScorePurifyAreaTarget(context.SquareBoard, target);
                },
                canExecute: context =>
                {
                    BoardCell target = ResolvePurifyAreaTarget(context);
                    return context.World.Mana >= 6 &&
                        target != null &&
                        BoardAnalysis.ScorePurifyAreaTarget(context.SquareBoard, target) >= 8f;
                },
                execute: context =>
                {
                    if (!context.World.TrySpendMana(6))
                    {
                        return;
                    }

                    BoardCell target = ResolvePurifyAreaTarget(context);
                    if (target == null)
                    {
                        return;
                    }

                    foreach (BoardCell cell in context.SquareBoard.GetCellsInRadius(target.Coords, 1))
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

                    return 15f +
                        BoardAnalysis.CountCorruptedNeighbors(context.SquareBoard, target) * 4f +
                        BoardAnalysis.CountAdjacentRoots(context.SquareBoard, target) * 4f;
                },
                canExecute: context =>
                    context.World.Mana >= 2 &&
                    ResolveFreezeTarget(context) != null,
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
                "sacrifice_cell",
                "Sacrifice Cell",
                "Kills one non-core tile to create a dead buffer.",
                0,
                0,
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
                canExecute: context => ResolveSacrificeTarget(context) != null,
                execute: context =>
                {
                    BoardCell target = ResolveSacrificeTarget(context);
                    target?.MakeDead();
                });
        }

        private static BoardCell ResolveCleanseTarget(PlayerActionContext context)
        {
            if (TryResolveExplicitTarget(context, out BoardCell cell) && cell.IsCorrupted)
            {
                return cell;
            }

            return BoardAnalysis.FindBestCleanseTarget(context.Attempt);
        }

        private static BoardCell ResolveFortifyTarget(PlayerActionContext context)
        {
            if (TryResolveExplicitTarget(context, out BoardCell cell) && cell.IsStable && !cell.IsDead)
            {
                return cell;
            }

            return BoardAnalysis.FindBestFortifyTarget(context.Attempt);
        }

        private static BoardCell ResolveManaBloomTarget(PlayerActionContext context)
        {
            if (TryResolveExplicitTarget(context, out BoardCell cell) &&
                cell.IsStable &&
                cell.Archetype == CellArchetype.HealthyLand)
            {
                return cell;
            }

            return BoardAnalysis.FindBestManaBloomTarget(context.Attempt);
        }

        private static BoardCell ResolvePurifyAreaTarget(PlayerActionContext context)
        {
            if (TryResolveExplicitTarget(context, out BoardCell cell) && !cell.IsDead)
            {
                return cell;
            }

            return BoardAnalysis.FindBestPurifyAreaTarget(context.Attempt);
        }

        private static BoardCell ResolveFreezeTarget(PlayerActionContext context)
        {
            if (TryResolveExplicitTarget(context, out BoardCell cell) && cell.IsCorrupted)
            {
                return cell;
            }

            return BoardAnalysis.FindBestFreezeTarget(context.Attempt);
        }

        private static BoardCell ResolveSacrificeTarget(PlayerActionContext context)
        {
            if (TryResolveExplicitTarget(context, out BoardCell cell) &&
                cell.Archetype != CellArchetype.LifeRoot &&
                cell.Archetype != CellArchetype.SacredSite &&
                !cell.IsDead)
            {
                return cell;
            }

            return BoardAnalysis.FindBestSacrificeTarget(context.Attempt);
        }

        private static BoardCell ResolveRestoreLandTarget(PlayerActionContext context)
        {
            if (TryResolveExplicitTarget(context, out BoardCell cell) && cell.Type == CellType.DeadCell)
            {
                return cell;
            }

            return BoardAnalysis.FindBestRestoreLandTarget(context.Attempt);
        }

        private static bool TryResolveExplicitTarget(PlayerActionContext context, out BoardCell cell)
        {
            return context.TryGetTargetCell(out cell);
        }
    }
}
