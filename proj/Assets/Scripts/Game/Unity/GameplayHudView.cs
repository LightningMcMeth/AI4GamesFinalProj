using System;
using System.Collections.Generic;
using UnityEngine;

namespace AI4GamesFinalProj.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayHudView : MonoBehaviour
    {
        [SerializeField] private AttemptController attemptController;
        [SerializeField] private Rect hudRect = new Rect(16f, 16f, 390f, 560f);
        [SerializeField] private string headerTitle = "Wizard Territory";
        [SerializeField] private bool showKeyboardHints = true;

        private IReadOnlyList<PlayerActionOffer> currentOffers = Array.Empty<PlayerActionOffer>();
        private string lastActionText = string.Empty;
        private string finalOutcomeText = string.Empty;

        private GUIStyle titleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle smallTextStyle;
        private GUIStyle barTextStyle;
        private GUIStyle cardStyle;
        private GUIStyle warningStyle;
        private Texture2D whiteTexture;

        private void Awake()
        {
            if (attemptController == null)
            {
                attemptController = GetComponent<AttemptController>();
            }

            whiteTexture = Texture2D.whiteTexture;
        }

        private void OnEnable()
        {
            if (attemptController == null)
            {
                return;
            }

            attemptController.AttemptStarted += HandleAttemptStarted;
            attemptController.OffersUpdated += HandleOffersUpdated;
            attemptController.TurnResolved += HandleTurnResolved;
            attemptController.AttemptEnded += HandleAttemptEnded;

            if (attemptController.CurrentAttempt != null)
            {
                currentOffers = attemptController.CurrentOffers;
            }
        }

        private void OnDisable()
        {
            if (attemptController == null)
            {
                return;
            }

            attemptController.AttemptStarted -= HandleAttemptStarted;
            attemptController.OffersUpdated -= HandleOffersUpdated;
            attemptController.TurnResolved -= HandleTurnResolved;
            attemptController.AttemptEnded -= HandleAttemptEnded;
        }

        private void OnGUI()
        {
            if (attemptController == null || attemptController.CurrentAttempt == null)
            {
                return;
            }

            EnsureStyles();

            Attempt attempt = attemptController.CurrentAttempt;

            GUILayout.BeginArea(hudRect, GUI.skin.box);
            DrawHeader(attempt);
            DrawWorldState(attempt);
            DrawOffers(attempt);
            DrawFooter(attempt);
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            smallTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true
            };

            barTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 5, 5),
                margin = new RectOffset(0, 0, 3, 3)
            };

            warningStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
        }

        private void DrawHeader(Attempt attempt)
        {
            GUILayout.Label(headerTitle, titleStyle);
            GUILayout.Label($"Player: {attempt.Player.Name}", smallTextStyle);
            GUILayout.Space(6f);
        }

        private void DrawWorldState(Attempt attempt)
        {
            GameWorld world = attempt.World;

            GUILayout.Label($"Turn {world.TurnNumber}/{world.TotalTicks}", sectionStyle);

            DrawProgressBar("Mana", world.Mana, world.MaxMana, world.ManaIncomeLastTurn, new Color(0.2f, 0.45f, 1f));
            DrawProgressBar("Vitality", world.Vitality, world.MaxVitality, world.VitalityIncomeLastTurn, new Color(0.2f, 0.85f, 0.35f));
            DrawProgressBar("Essence", world.Essence, world.MaxEssence, world.EssenceIncomeLastTurn, new Color(0.75f, 0.35f, 1f));
            DrawProgressBar("Danger", Mathf.RoundToInt(world.DangerLevel * 100f), 100, 0, GetDangerColor(world.DangerLevel), "%");

            GUILayout.Space(6f);

            GUILayout.Label($"Life Roots: {world.LifeRootsRemaining}", sectionStyle);
            GUILayout.Label($"Actions: {attempt.ActionsResolvedThisTurn}/{world.MaxPlayerActionsPerTurn}", sectionStyle);

            GUILayout.Space(6f);
        }

        private void DrawProgressBar(
            string label,
            int value,
            int maxValue,
            int income,
            Color fillColor,
            string suffix = "")
        {
            maxValue = Mathf.Max(1, maxValue);
            float percent = Mathf.Clamp01((float)value / maxValue);

            Rect rect = GUILayoutUtility.GetRect(1f, 16f, GUILayout.ExpandWidth(true));

            GUI.color = new Color(0.05f, 0.05f, 0.05f, 0.8f);
            GUI.DrawTexture(rect, whiteTexture);

            Rect fillRect = new Rect(rect.x, rect.y, rect.width * percent, rect.height);
            GUI.color = fillColor;
            GUI.DrawTexture(fillRect, whiteTexture);

            GUI.color = Color.white;

            string incomeText = income == 0 ? string.Empty : $" ({FormatSigned(income)})";
            GUI.Label(rect, $"{label}: {value}/{maxValue}{suffix}{incomeText}", barTextStyle);

            GUILayout.Space(2f);
        }

        private void DrawOffers(Attempt attempt)
        {
            GUILayout.Label("Spells Offered by Utility AI", sectionStyle);

            string selectedActionId = attemptController.SelectedPreviewActionId;

            if (currentOffers == null || currentOffers.Count == 0)
            {
                GUILayout.Label("No offers available.", cardStyle);
            }
            else
            {
                for (int index = 0; index < currentOffers.Count; index++)
                {
                    PlayerActionOffer offer = currentOffers[index];
                    bool isSelected = string.Equals(
                        selectedActionId,
                        offer.Action.Id,
                        StringComparison.OrdinalIgnoreCase);

                    GUILayout.BeginVertical(cardStyle);

                    string selectedMarker = isSelected ? "> " : string.Empty;
                    string costText = FormatActionCost(offer.Action);
                    string title = $"{selectedMarker}{index + 1}. {offer.Action.DisplayName} {costText}";

                    if (GUILayout.Button(title))
                    {
                        attemptController.TrySelectOfferedPreview(index);
                    }

                    GUILayout.Label(offer.Action.Description, smallTextStyle);
                    GUILayout.Label($"Utility score: {offer.UtilityScore:F1}", smallTextStyle);

                    GUILayout.EndVertical();
                }
            }

            bool canEndTurn = attempt.CanEndTurnEarly || (currentOffers?.Count ?? 0) == 0;
            GUI.enabled = canEndTurn;

            if (GUILayout.Button(canEndTurn ? "End Turn" : "Use all required actions first"))
            {
                attemptController.SubmitEndTurn();
            }

            GUI.enabled = true;
            GUILayout.Space(6f);
        }

        private void DrawFooter(Attempt attempt)
        {
            if (attemptController.HasSelectedPreviewAction)
            {
                GUILayout.Label(
                    $"Selected spell: {attemptController.SelectedPreviewActionId}\nClick a board cell to cast.",
                    warningStyle);
            }

            if (!string.IsNullOrWhiteSpace(lastActionText))
            {
                GUILayout.Label(lastActionText, smallTextStyle);
            }

            if (showKeyboardHints)
            {
                GUILayout.Label(
                    "Keys: 1/2/3 select spell, click cell to cast, 0/Enter/Space end turn.",
                    smallTextStyle);
            }

            if (attempt.World.HasEnded)
            {
                string outcome = string.IsNullOrWhiteSpace(finalOutcomeText)
                    ? $"{attempt.World.Outcome}: {attempt.World.OutcomeReason}"
                    : finalOutcomeText;

                GUILayout.Label(outcome, warningStyle);
            }
        }

        private static Color GetDangerColor(float dangerLevel)
        {
            if (dangerLevel < 0.35f)
            {
                return new Color(0.2f, 0.8f, 0.35f);
            }

            if (dangerLevel < 0.7f)
            {
                return new Color(1f, 0.75f, 0.15f);
            }

            return new Color(1f, 0.2f, 0.15f);
        }

        private static string FormatSigned(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString();
        }

        private static string FormatActionCost(PlayerAction action)
        {
            if (action == null)
            {
                return string.Empty;
            }

            List<string> costs = new List<string>();

            if (action.ManaCost > 0)
            {
                costs.Add($"{action.ManaCost} mana");
            }

            if (action.EssenceCost > 0)
            {
                costs.Add($"{action.EssenceCost} essence");
            }

            return costs.Count == 0
                ? "(free)"
                : $"({string.Join(", ", costs)})";
        }

        private void HandleAttemptStarted(Attempt attempt)
        {
            currentOffers = attempt.CurrentOffers;
            lastActionText = string.Empty;
            finalOutcomeText = string.Empty;
        }

        private void HandleOffersUpdated(Attempt attempt, IReadOnlyList<PlayerActionOffer> offers)
        {
            currentOffers = offers ?? Array.Empty<PlayerActionOffer>();
        }

        private void HandleTurnResolved(Attempt attempt, PlayerAction action)
        {
            if (action == null)
            {
                return;
            }

            lastActionText = $"Last Action: {action.DisplayName}";
        }

        private void HandleAttemptEnded(Attempt attempt)
        {
            finalOutcomeText = $"{attempt.World.Outcome}: {attempt.World.OutcomeReason}";
        }
    }
}