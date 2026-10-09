namespace MouseJiggler.Application;

/// <summary>Result of a keep-awake session: either it ran (and how many jiggles it did) or it could not start.</summary>
public sealed record KeepAwakeOutcome(bool HasRun, int JiggleCount, string? Problem)
{
    public static KeepAwakeOutcome Completed(int jiggleCount) => new(true, jiggleCount, null);

    public static KeepAwakeOutcome NotReady(string problem) => new(false, 0, problem);
}
