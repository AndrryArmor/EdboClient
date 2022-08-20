using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SilentThief
{
    public class AbiturientOfferManager
    {
        private static readonly HttpClient _client = new HttpClient();
        private static readonly List<int> _allowedStatuses = new List<int> 
        { 
            OfferStatus.CameFromSite,
            OfferStatus.Registered,
            OfferStatus.Admitted,
            OfferStatus.Recommended,
            OfferStatus.IncludedToOrder
        };
        private static readonly List<int> _greenStatuses = new List<int>()
        {
            OfferStatus.Recommended,
            OfferStatus.IncludedToOrder
        };

        public static async Task<SpecialityCompetitionStats> GetStatsFor(SpecialityInfo specialityInfo, double secondPriorityUpperLimit = -1)
        {
            var abiturients = await GetAbiturients(specialityInfo.Code);
            abiturients.RemoveAll(a => !_allowedStatuses.Contains(a.StatusId));
            // Вилучаємо контрактні заявки
            abiturients.RemoveAll(a => a.Priority == 0);

            // Співбесіда
            var interviewPassedAbiturients = abiturients.Where(a => _greenStatuses.Contains(a.StatusId)
                && a.Subjects.Any(s => s.Name.Contains("Співбесіда"))).ToList();
            abiturients.RemoveAll(a => interviewPassedAbiturients.Contains(a));

            // Квота-2
            var quota2Abiturients = abiturients.Where(a => a.Subjects.Any(s => s.Name.Contains("Квота 2"))).ToList();
            var quota2PassedAbiturients = quota2Abiturients.Where(a => _greenStatuses.Contains(a.StatusId)).ToList();
            if (!quota2PassedAbiturients.Any())
            {
                quota2PassedAbiturients = RunCompetition(quota2Abiturients, specialityInfo.Quota2BudgetPlaces,
                    secondPriorityUpperLimit);
            }
            abiturients.RemoveAll(a => quota2PassedAbiturients.Contains(a));
            //PrintAbiturients(quota2PassedAbiturients);

            // Квота 1
            var quota1Abiturients = abiturients.Where(a => a.Subjects.Any(s => s.Name.Contains("Квота 1"))).ToList();
            var quota1PassedAbiturients = quota1Abiturients.Where(a => _greenStatuses.Contains(a.StatusId)).ToList();
            if (!quota1PassedAbiturients.Any())
            {
                quota1PassedAbiturients = RunCompetition(quota1Abiturients, specialityInfo.Quota1BudgetPlaces);
            }
            abiturients.RemoveAll(a => quota1PassedAbiturients.Contains(a));
            //PrintAbiturients(quota1PassedAbiturients);

            // Звичайні абітурієнти
            var passedAbiturients = abiturients.Where(a => _greenStatuses.Contains(a.StatusId)).ToList();
            var freePlaces = (specialityInfo.Quota1BudgetPlaces - quota1PassedAbiturients.Count)
                    + (specialityInfo.Quota2BudgetPlaces - quota2PassedAbiturients.Count)
                    - interviewPassedAbiturients.Count;
            if (!passedAbiturients.Any())
            {                
                passedAbiturients = RunCompetition(abiturients, specialityInfo.BudgetPlaces + freePlaces,
                    secondPriorityUpperLimit);
            }
            //PrintAbiturients(passedAbiturients);

            return new SpecialityCompetitionStats()
            {
                SpecialityName = specialityInfo.Name,
                Quota1AbiturientsCount = quota1Abiturients.Count,
                Quota2AbiturientsCount = quota2Abiturients.Count,
                SimpleAbiturientsCount = abiturients.Count,
                SecondPriorityUpperLimit = secondPriorityUpperLimit,
                Quota1PassingScore = quota1PassedAbiturients.Any(a => _greenStatuses.Contains(a.StatusId)) || 
                    quota1PassedAbiturients.Count >= specialityInfo.Quota1BudgetPlaces
                    ? quota1PassedAbiturients.LastOrDefault()?.Score ?? -1
                    : -1,
                Quota2PassingScore = quota2PassedAbiturients.Any(a => _greenStatuses.Contains(a.StatusId)) ||
                    quota2PassedAbiturients.Count >= specialityInfo.Quota2BudgetPlaces
                    ? quota2PassedAbiturients.LastOrDefault()?.Score ?? -1
                    : -1,
                GeneralPassingScore = passedAbiturients.Any(a => _greenStatuses.Contains(a.StatusId)) ||
                    passedAbiturients.Count >= specialityInfo.BudgetPlaces + freePlaces
                    ? passedAbiturients.LastOrDefault()?.Score ?? -1
                    : -1
            };
        }

        public static void PrintStats(SpecialityCompetitionStats mainStats, SpecialityCompetitionStats? additionalStats = null)
        {
            var result = new StringBuilder();
            result.AppendLine($"===== Інформація про спеціальність {mainStats.SpecialityName} ФІОТ =====");
            result.AppendLine($"Абітурієнтів з квотою 1: {mainStats.Quota1AbiturientsCount}");
            result.AppendLine($"Абітурієнтів з квотою 2: {mainStats.Quota2AbiturientsCount}");
            result.AppendLine($"Звичайних абітурієнтів: {mainStats.SimpleAbiturientsCount}");

            var statsList = new List<SpecialityCompetitionStats>
            {
                mainStats
            };
            if (additionalStats != null)
            {
                statsList.Add(additionalStats);
            }
            foreach (var stats in statsList)
            {
                var lastAbiturientScore = 
                result.AppendLine($"Непрохідний по квоті 1: {stats.Quota1PassingScore}");
                if (stats.SecondPriorityUpperLimit == -1)
                {
                    result.AppendLine($"Непрохідний по квоті 2: {stats.Quota2PassingScore}");
                    result.AppendLine($"Непрохідний по загальному конкурсу: {stats.GeneralPassingScore}");
                }
                else
                {
                    result.AppendLine($"Непрохідний по квоті 2 (1-ий пріоритет усі, " +
                        $"2-ий пріоритет з балом <={stats.SecondPriorityUpperLimit}): " +
                        $"{stats.Quota2PassingScore}");
                    result.AppendLine($"Непрохідний по загальному конкурсу (1-ий пріоритет усі, " +
                        $"2-ий пріоритет з балом <={stats.SecondPriorityUpperLimit}): " +
                        $"{stats.GeneralPassingScore}");
                }
                result.AppendLine("----------------------------------------");
            }

            Console.WriteLine(result.Replace("-1", "відсутній"));
        }

        private static List<AbiturientOffer> RunCompetition(List<AbiturientOffer> abiturients,
            int availablePlaces, double secondPriorityUpperLimit = -1)
        {
            // Сортування за спаданням
            abiturients.Sort((x, y) => Math.Sign(y.Score - x.Score));
            var passedAbiturients = abiturients
                .Where(a => a.Priority == 1 || (a.Priority == 2 && a.Score <= secondPriorityUpperLimit))
                .Take(availablePlaces)
                .ToList();
            return passedAbiturients;
        }

        private static void PrintAbiturients(List<AbiturientOffer> abiturients)
        {
            var result = new StringBuilder();
            result.AppendLine($"{"Ім'я", 25} | {"Статус", 6} | {"Пріоритет", 9} | {"Бали", 7} | {"Квота", 22}");
            abiturients.ForEach(a =>
            {
                result.AppendLine($"{a.Name, 25} | {a.StatusId, 6} | {a.Priority, 9} | {a.Score, 7} | " +
                    $"{string.Join(", ", a.Subjects.Select(s => s.Name)), 22}");
            });
            result.AppendLine("---------------------------------------------------------------------------------");
            Console.WriteLine(result.ToString());
            //var filePath = Path.GetTempFileName();
            //Console.WriteLine(filePath);
            //using (var sw = new StreamWriter(filePath))
            //{
            //    sw.Write(result);
            //}
        }

        private static async Task<List<AbiturientOffer>> GetAbiturients(string specialityCode)
        {
            var lastAbiturient = 0;
            var abiturientRecords = new List<AbiturientOffer>();
            List<AbiturientOffer>? abiturientRecordsChunk;
            do
            {
                abiturientRecordsChunk = await GetAbiturientRecordsChunk(specialityCode, lastAbiturient);
                if (abiturientRecordsChunk == null) continue;
                abiturientRecords.AddRange(abiturientRecordsChunk);
                lastAbiturient += abiturientRecordsChunk.Count;
            } while (abiturientRecordsChunk == null || abiturientRecordsChunk.Any());

            return abiturientRecords;
        }

        private static async Task<List<AbiturientOffer>?> GetAbiturientRecordsChunk(string specialityCode, int lastAbiturient)
        {
            try
            {
                _client.DefaultRequestHeaders.Accept.Clear();
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _client.DefaultRequestHeaders.Referrer = new Uri($"https://vstup.edbo.gov.ua/offer/" + specialityCode);

                var edboRequestBody = new Dictionary<string, string>()
                {
                    ["id"] = specialityCode,
                    ["last"] = lastAbiturient.ToString()
                };
                var content = new FormUrlEncodedContent(edboRequestBody);

                var response = await _client.PostAsync(@"https://vstup.edbo.gov.ua/offer-requests/", content);
                var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonDocument.Parse(responseString).RootElement.GetProperty("requests")
                    .Deserialize<AbiturientOffer[]>();
                return result?.ToList() ?? new List<AbiturientOffer>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            return null;
        }
    }
}
