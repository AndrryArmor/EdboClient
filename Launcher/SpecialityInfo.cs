namespace SilentThief
{
    public struct SpecialityInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int BudgetPlaces { get; set; } = default;
        public int Quota1BudgetPlaces { get; set; } = default;
        public int Quota2BudgetPlaces { get; set; } = default;

        public SpecialityInfo()
        {

        }
    }
}
