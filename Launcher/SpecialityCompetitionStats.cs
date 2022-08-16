namespace SilentThief
{
    public class SpecialityCompetitionStats
    {
        public string SpecialityName { get; set; } = string.Empty;
        public int Quota1AbiturientsCount { get; set; }
        public int Quota2AbiturientsCount { get; set; }
        public int SimpleAbiturientsCount { get; set; }
        public double SecondPriorityUpperLimit { get; set; }
        public double Quota1PassingScore { get; set; }
        public double Quota2PassingScore { get; set; }
        public double GeneralPassingScore { get; set; }
    }
}
