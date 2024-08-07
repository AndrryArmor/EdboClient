﻿using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EdboClient.Launcher;

public class AbiturientOfferManager
{
    private static readonly HttpClient _client = new();
    private static readonly OfferStatus[] _allowedStatuses =
    [
        OfferStatus.CameFromSite,
        OfferStatus.Registered,
        OfferStatus.Admitted,
        OfferStatus.Recommended,
        OfferStatus.IncludedToOrder
    ];
    private static readonly OfferStatus[] _greenStatuses =
    [
        OfferStatus.Recommended,
        OfferStatus.IncludedToOrder
    ];

    public static async Task<SpecialityCompetitionStats> GetStatsFor(SpecialtyInfo specialityInfo, double secondPriorityUpperLimit = -1)
    {
        var abiturients = (await GetAbiturients(specialityInfo.Code)).ToList();
        abiturients.RemoveAll(a => !_allowedStatuses.Contains(a.Status));
        // Вилучаємо контрактні заявки
        abiturients.RemoveAll(a => a.Priority == 0);
        // Сортування за спаданням
        abiturients.Sort((x, y) => Math.Sign(y.Score - x.Score));
        //PrintScoreStats(abiturients);

        // Співбесіда
        var interviewPassedAbiturients = abiturients
            .Where(a => _greenStatuses.Contains(a.Status) && a.Subjects.Any(s => s.Name.Contains("Співбесіда")))
            .ToList();
        abiturients.RemoveAll(a => interviewPassedAbiturients.Contains(a));

        // Квота-2
        var quota2Abiturients = abiturients
            .Where(a => a.Subjects.Any(s => s.Name.Contains("Квота 2")))
            .ToList();
        var quota2PassedAbiturients = quota2Abiturients
            .Where(a => _greenStatuses.Contains(a.Status))
            .ToList();
        if (quota2PassedAbiturients.Count == 0)
        {
            quota2PassedAbiturients = RunCompetition(quota2Abiturients, specialityInfo.Quota2BudgetPlaces,
                secondPriorityUpperLimit);
        }
        abiturients.RemoveAll(a => quota2PassedAbiturients.Contains(a));
        //PrintAbiturients(quota2PassedAbiturients);

        // Квота 1
        var quota1Abiturients = abiturients
            .Where(a => a.Subjects.Any(s => s.Name.Contains("Квота 1")))
            .ToList();
        var quota1PassedAbiturients = quota1Abiturients
            .Where(a => _greenStatuses.Contains(a.Status))
            .ToList();
        if (quota1PassedAbiturients.Count == 0)
        {
            quota1PassedAbiturients = RunCompetition(quota1Abiturients, specialityInfo.Quota1BudgetPlaces);
        }
        abiturients.RemoveAll(a => quota1PassedAbiturients.Contains(a));
        //PrintAbiturients(quota1PassedAbiturients);

        // Звичайні абітурієнти
        var passedAbiturients = abiturients
            .Where(a => _greenStatuses.Contains(a.Status))
            .ToList();
        var occupiedGeneralBudgetPlaces = interviewPassedAbiturients.Count
            + quota1PassedAbiturients.Count + quota2PassedAbiturients.Count;
        if (passedAbiturients.Count == 0)
        {
            passedAbiturients = RunCompetition(abiturients, specialityInfo.BudgetPlaces - occupiedGeneralBudgetPlaces,
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
            Quota1PassingScore = GetPassingScore(quota1PassedAbiturients, specialityInfo.Quota1BudgetPlaces),
            Quota2PassingScore = GetPassingScore(quota2PassedAbiturients, specialityInfo.Quota2BudgetPlaces),
            GeneralPassingScore = GetPassingScore(passedAbiturients, specialityInfo.BudgetPlaces - occupiedGeneralBudgetPlaces),
        };
    }

    public static void PrintStats(SpecialityCompetitionStats mainStats, SpecialityCompetitionStats? additionalStats = null)
    {
        var result = new StringBuilder();
        result.AppendLine($"===== Інформація про спеціальність {mainStats.SpecialityName} ФІОТ =====");
        result.AppendLine($"Абітурієнтів з квотою 1: {mainStats.Quota1AbiturientsCount}");
        result.AppendLine($"Абітурієнтів з квотою 2: {mainStats.Quota2AbiturientsCount}");
        result.AppendLine($"Звичайних абітурієнтів: {mainStats.SimpleAbiturientsCount}");

        List<SpecialityCompetitionStats> statsList = [mainStats];
        if (additionalStats != null)
        {
            statsList.Add(additionalStats);
        }
        foreach (var stats in statsList)
        {
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
        //abiturients.Sort((x, y) => Math.Sign(y.Score - x.Score));
        var passedAbiturients = abiturients
            .Where(a => a.Priority == 1 || a.Priority == 2 && a.Score <= secondPriorityUpperLimit)
            .Take(availablePlaces)
            .ToList();
        return passedAbiturients;
    }

    private static double GetPassingScore(ICollection<AbiturientOffer> abiturients, int budgetPlacesCount)
    {
        return abiturients.Any(a => _greenStatuses.Contains(a.Status)) || abiturients.Count >= budgetPlacesCount
            ? abiturients.LastOrDefault()?.Score ?? -1
            : -1;
    }

    private static void PrintAbiturients(IEnumerable<AbiturientOffer> abiturients)
    {
        Console.WriteLine($"{"Номер", 5} | {"Ім'я", 25} | {"Статус", 15} | {"Пріоритет", 9} | {"Бали", 7} | {"Квота", 22}");
        foreach (var (abiturient, index) in abiturients.Select((a, i)  => (a, i)))
        {
            var subjectsString = string.Join(", ", abiturient.Subjects
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .Select(s => s.Name));
            Console.WriteLine($"{index + 1, 5} | {abiturient.Name, 25} | {abiturient.Status, 15} | {abiturient.Priority, 9} | {abiturient.Score, 7} | {subjectsString, 22}");
        }
        Console.WriteLine("---------------------------------------------------------------------------------");
        //var filePath = Path.GetTempFileName();
        //Console.WriteLine(filePath);
        //using (var sw = new StreamWriter(filePath))
        //{
        //    sw.Write(result);
        //}
    }

    private static void PrintScoreStats(IEnumerable<AbiturientOffer> abiturients)
    {
        List<ScoreStatsRow> scoreStats = [];
        int lowerBound = 100;
        int higherBound = 104;
        int maxScore = 200;
        int step = 3;
        int prioritiesCount = 5;

        while (higherBound <= maxScore)
        {
            var statsByPriority = new int[prioritiesCount];
            var abiturientsInStatsRow = abiturients.Where(a => lowerBound < a.Score && a.Score <= higherBound);
            for (int priority = 1; priority <= prioritiesCount; priority++)
            {
                statsByPriority[priority - 1] = abiturientsInStatsRow.Count(a => a.Priority == priority);
            }

            scoreStats.Add(new (lowerBound, higherBound, statsByPriority));
            lowerBound = higherBound;
            higherBound += step;
        }

        List<ConsoleColor> priorityColors = [ConsoleColor.White, ConsoleColor.Blue, ConsoleColor.Cyan, ConsoleColor.DarkYellow, ConsoleColor.DarkRed];
        scoreStats.ForEach(s =>
        {
            var abiturientsCount = s.StatsByPriority.Aggregate((left, right) => left + right);
            Console.Write($"{s.LowerBound}-{s.HigherBound} | {abiturientsCount, 3} | ");

            for (int i = 0; i < s.StatsByPriority.Length; i++)
            {
                var priorityCount = s.StatsByPriority[i];
                var statsPart = new string(' ', priorityCount);
                if (priorityCount > 0)
                {
                    var value = priorityCount.ToString();
                    statsPart = string.Concat(statsPart.Take(statsPart.Length - value.Length)) + value;
                }

                Console.BackgroundColor = priorityColors[i];
                Console.ForegroundColor = ConsoleColor.Black;
                Console.Write(statsPart);
            }

            Console.ResetColor();
            Console.WriteLine();
        });
        Console.WriteLine("---------------------------------------------------------------------------------");
    }

    private static async Task<IEnumerable<AbiturientOffer>> GetAbiturients(string specialityCode)
    {
        List<AbiturientOffer> abiturientRecords = [];
        AbiturientOffer[]? abiturientRecordsChunk;
        do
        {
            abiturientRecordsChunk = await GetAbiturientRecordsChunk(specialityCode, abiturientRecords.Count);
            if (abiturientRecordsChunk is null)
            {
                continue;
            }
            abiturientRecords.AddRange(abiturientRecordsChunk);
        } while (abiturientRecordsChunk is null || abiturientRecordsChunk.Length != 0);

        return abiturientRecords;
    }

    private static async Task<AbiturientOffer[]?> GetAbiturientRecordsChunk(string specialityCode, int lastAbiturient)
    {
        try
        {
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Referrer = new Uri($"https://vstup.edbo.gov.ua/offer/{specialityCode}");

            var edboRequestBody = new Dictionary<string, string>()
            {
                ["id"] = specialityCode,
                ["last"] = lastAbiturient.ToString()
            };
            var content = new FormUrlEncodedContent(edboRequestBody);

            HttpResponseMessage httpResponse = await _client.PostAsync(@"https://vstup.edbo.gov.ua/offer-requests/", content);
            string jsonResponse = await httpResponse.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(jsonResponse).RootElement.GetProperty("requests")
                .Deserialize<AbiturientOffer[]>();
            return result;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Network error: {e.HttpRequestError}. Status code: {e.StatusCode ?? System.Net.HttpStatusCode.OK}. Message: {e.Message}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }

        return null;
    }

    public record ScoreStatsRow(int LowerBound, int HigherBound, int[] StatsByPriority);
}
