using System;
using UnityEngine;

namespace AI4GamesFinalProj.Gameplay
{
    public sealed class PlayerActionRequest
    {
        public PlayerActionRequest(
            string actionId,
            PlayerInputKind inputKind = PlayerInputKind.Ui,
            Vector3? targetCoords = null,
            string inputBindingId = null)
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                throw new ArgumentException("Action id cannot be empty.", nameof(actionId));
            }

            ActionId = actionId;
            InputKind = inputKind;
            TargetCoords = targetCoords;
            InputBindingId = inputBindingId ?? string.Empty;
        }

        public string ActionId { get; }

        public PlayerInputKind InputKind { get; }

        public Vector3? TargetCoords { get; }

        public string InputBindingId { get; }

        public bool HasTarget => TargetCoords.HasValue;

        public PlayerActionRequest ForAction(string actionId)
        {
            return new PlayerActionRequest(actionId, InputKind, TargetCoords, InputBindingId);
        }
    }

    public enum PlayerInputKind
    {
        Ui = 0,
        Mouse = 1,
        Keyboard = 2
    }
}
