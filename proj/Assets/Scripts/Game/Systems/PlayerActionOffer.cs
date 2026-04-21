namespace AI4GamesFinalProj.Gameplay
{
    public sealed class PlayerActionOffer
    {
        public PlayerActionOffer(PlayerAction action, float utilityScore)
        {
            Action = action;
            UtilityScore = utilityScore;
        }

        public PlayerAction Action { get; }

        public float UtilityScore { get; }
    }
}
