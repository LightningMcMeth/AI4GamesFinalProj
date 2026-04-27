using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AI4GamesFinalProj.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayHudView : MonoBehaviour
    {
        [SerializeField]
        private AttemptController attemptController;

        [SerializeField]
        private Rect hudRect = new Rect(16f, 16f, 360f, 360f);

        [SerializeField]
        private string headerTitle = "Wizard Territory";

        [SerializeField]
        private bool showKeyboardHints = true;

        private IReadOnlyList<PlayerActionOffer> currentOffers = Array.Empty<PlayerActionOffer>();
        private string lastActionText = string.Empty;
        private string finalOutcomeText = string.Empty;

        private void Awake()
        {
            if (attemptController == null)
            {
                attemptController = GetComponent<AttemptController>();
            }
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
            if (attemptController == null)
            {
                return;
            }

            Attempt attempt = attemptController.CurrentAttempt;
            if (attempt == null)
            {
                return;
            }

            GUILayout.BeginArea(hudRect, GUI.skin.box);
            DrawHeader(attempt);
            DrawWorldState(attempt);
            DrawOffers(attempt);
            DrawFooter(attempt);
            GUILayout.EndArea();
        }

        private void DrawHeader(Attempt attempt)
        {
            GUILayout.Label(headerTitle, GUI.skin.label);
            GUILayout.Label($"Player: {attempt.Player.Name}");
            GUILayout.Space(6f);
        }

        private void DrawWorldState(Attempt attempt)
        {
            GameWorld world = attempt.World;
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Turn: {world.TurnNumber}/{world.TotalTicks}");
            builder.AppendLine($"Mana: {world.Mana} (+{world.ManaIncomeLastTurn})");
            builder.AppendLine($"Vitality: {world.Vitality} (+{world.VitalityIncomeLastTurn})");
            builder.AppendLine($"Essence: {world.Essence} (+{world.EssenceIncomeLastTurn})");
            builder.AppendLine($"Life Roots: {world.LifeRootsRemaining}");
            builder.AppendLine($"Danger: {world.DangerLevel:F2}");
            builder.AppendLine($"Actions: {attempt.ActionsResolvedThisTurn}/{world.MaxPlayerActionsPerTurn}");
            GUILayout.Label(builder.ToString(), GUI.skin.box);
        }

        private void DrawOffers(Attempt attempt)
        {
            GUILayout.Space(6f);
            GUILayout.Label("Offers");
            string selectedActionId = attemptController.SelectedPreviewActionId;

            if (currentOffers == null || currentOffers.Count == 0)
            {
                GUILayout.Label("No offers available.");
            }
            else
            {
                for (int index = 0; index < currentOffers.Count; index++)
                {
                    PlayerActionOffer offer = currentOffers[index];
                    bool isSelected = string.Equals(selectedActionId, offer.Action.Id, StringComparison.OrdinalIgnoreCase);
                    string label = $"{(isSelected ? "> " : string.Empty)}{index + 1}. {offer.Action.DisplayName} [{offer.UtilityScore:F1}]";
                    if (GUILayout.Button(label))
                    {
                        attemptController.TrySelectOfferedPreview(index);
                    }

                    GUILayout.Label(offer.Action.Description);
                }
            }

            GUI.enabled = attempt.CanEndTurnEarly || (currentOffers?.Count ?? 0) == 0;
            if (GUILayout.Button("End Turn"))
            {
                attemptController.SubmitEndTurn();
            }

            GUI.enabled = true;
        }

        private void DrawFooter(Attempt attempt)
        {
            GUILayout.Space(6f);

            if (showKeyboardHints)
            {
                GUILayout.Label("Keys: 1/2/3 cast offers, 0/Enter/Space end turn.");
            }

            if (!string.IsNullOrWhiteSpace(lastActionText))
            {
                GUILayout.Label(lastActionText, GUI.skin.box);
            }

            if (attemptController.HasSelectedPreviewAction)
            {
                GUILayout.Label($"Selected Spell: {attemptController.SelectedPreviewActionId}. Click a board cell to cast.", GUI.skin.box);
            }

            if (attempt.World.HasEnded)
            {
                string outcome = string.IsNullOrWhiteSpace(finalOutcomeText)
                    ? $"{attempt.World.Outcome}: {attempt.World.OutcomeReason}"
                    : finalOutcomeText;
                GUILayout.Label(outcome, GUI.skin.box);
            }
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
