using System.Collections.Generic;
using UnityEngine;

namespace AI4GamesFinalProj.Gameplay
{
    public interface IGameBoard
    {
        int Width { get; }

        int Height { get; }

        bool IsInside(Vector3 coords);

        IBoardCell GetCell(Vector3 coords);

        IEnumerable<IBoardCell> GetNeighbors(
            Vector3 coords,
            NeighborhoodType neighborhoodType = NeighborhoodType.Moore);
    }

    public enum NeighborhoodType
    {
        VonNeumann = 0,
        Moore = 1
    }
}
