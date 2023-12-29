using Newtonsoft.Json;

public class OpeningHour
{
    [JsonProperty("date")]
    public DateTime Date { get; set; }

    [JsonProperty("opens")]
    public DateTime? Opens { get; set; }

    [JsonProperty("closes")]
    public DateTime? Closes { get; set; }
}

