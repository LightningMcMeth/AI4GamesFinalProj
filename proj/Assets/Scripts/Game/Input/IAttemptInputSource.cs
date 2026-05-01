namespace AI4GamesFinalProj.Gameplay
{
    public interface IAttemptInputSource
    {
        bool TryCreateRequest(Attempt attempt, out PlayerActionRequest request);
    }
}
