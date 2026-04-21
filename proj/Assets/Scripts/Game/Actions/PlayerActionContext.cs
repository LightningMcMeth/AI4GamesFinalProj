using System;
using System.Collections.Generic;
using UnityEngine;

namespace AI4GamesFinalProj.Gameplay
{
    public sealed class PlayerActionContext
    {
        public PlayerActionContext(Attempt attempt, PlayerActionRequest request)
        {
            Attempt = attempt ?? throw new ArgumentNullException(nameof(attempt));
            Request = request ?? throw new ArgumentNullException(nameof(request));
        }

        public Attempt Attempt { get; }

        public PlayerActionRequest Request { get; }

        public Player Player => Attempt.Player;

        public GameWorld World => Attempt.World;

        public IGameBoard Board => Attempt.Board;

        public SquareGameBoard SquareBoard => Attempt.Board as SquareGameBoard;

        public IReadOnlyList<PlayerActionOffer> CurrentOffers => Attempt.CurrentOffers;

        public Vector3? TargetCoords => Request.TargetCoords;

        public bool TryGetTargetCell(out BoardCell cell)
        {
            cell = null;

            if (!TargetCoords.HasValue)
            {
                return false;
            }

            return Attempt.TryGetCell(TargetCoords.Value, out cell);
        }
    }
}
