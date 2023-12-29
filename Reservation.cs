using Newtonsoft.Json;

public class Reservation
{
    [JsonProperty("begin")]
    public DateTime Begin { get; set; }

    [JsonProperty("end")]
    public DateTime End { get; set; }
}

