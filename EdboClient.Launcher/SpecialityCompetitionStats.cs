namespace EdboClient.Launcher;

public record SpecialityCompetitionStats
{
    public required string SpecialityName { get; init; }
    public required int Quota1AbiturientsCount { get; init; }
    public required int Quota2AbiturientsCount { get; init; }
    public required int SimpleAbiturientsCount { get; init; }
    public required double SecondPriorityUpperLimit { get; init; }
    public required double Quota1PassingScore { get; init; }
    public required double Quota2PassingScore { get; init; }
    public required double GeneralPassingScore { get; init; }
}
