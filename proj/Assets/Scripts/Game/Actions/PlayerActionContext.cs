using System;
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

        public Vector3? TargetCoords => Request.TargetCoords;
    }
}
