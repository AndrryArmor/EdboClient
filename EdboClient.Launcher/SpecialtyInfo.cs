namespace EdboClient.Launcher;

public record SpecialtyInfo
{
    public required string Name { get; init; }
    public required string Code { get; init; }
    public required int BudgetPlaces { get; init; }
    public required int Quota1BudgetPlaces { get; init; }
    public required int Quota2BudgetPlaces { get; init; }
}
