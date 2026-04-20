using System.Collections.Generic;
using UnityEngine;

namespace AI4GamesFinalProj.Gameplay
{
    public interface IBoardCell
    {
        Vector3 Coords { get; }

        bool BlocksMovement { get; }

        IReadOnlyDictionary<string, float> StateValues { get; }
    }
}
