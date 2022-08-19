using System.Text;

namespace SilentThief
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine($"Результати станом на {DateTime.Now}");
            Console.WriteLine("----------------------------------------");
            var fice121Stats = await AbiturientOfferManager.GetStatsFor(Specialities.Fice121);
            AbiturientOfferManager.PrintStats(fice121Stats);
            var limit = fice121Stats.GeneralPassingScore - 1;

            var fice121CorrespondenceStats = await AbiturientOfferManager.GetStatsFor(Specialities.Fice121Correspondence);
            AbiturientOfferManager.PrintStats(fice121CorrespondenceStats);

            var fice126Stats = await AbiturientOfferManager.GetStatsFor(Specialities.Fice126);
            var fice126SpecialStats = await AbiturientOfferManager.GetStatsFor(Specialities.Fice126, limit);
            AbiturientOfferManager.PrintStats(fice126Stats, fice126SpecialStats);

            var fice123Stats = await AbiturientOfferManager.GetStatsFor(Specialities.Fice123);
            var fice123SpecialStats = await AbiturientOfferManager.GetStatsFor(Specialities.Fice123, limit);
            AbiturientOfferManager.PrintStats(fice123Stats, fice123SpecialStats);

            Console.ReadKey();
        }
    }
}