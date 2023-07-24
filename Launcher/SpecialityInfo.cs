namespace SilentThief
{
    public sealed record SpecialityInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int BudgetPlaces { get; set; }
        public int Quota1BudgetPlaces { get; set; }
        public int Quota2BudgetPlaces { get; set; }
    }
}
