using System.Text.Json.Serialization;

namespace EdboClient.Launcher;

public record AbiturientOffer(
    [property: JsonPropertyName("prid")] int Id,
    [property: JsonPropertyName("n")] int Position,
    [property: JsonPropertyName("fio")] string Name,
    [property: JsonPropertyName("prsid")] OfferStatus Status,
    [property: JsonPropertyName("p")] int Priority,
    [property: JsonPropertyName("kv")] double Score,
    [property: JsonPropertyName("rss")] Subject[] Subjects);

public record Subject([property: JsonPropertyName("sn")] string Name = "");