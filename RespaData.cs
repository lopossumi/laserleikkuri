using Newtonsoft.Json;

public class RespaData
{   
    [JsonProperty("opening_hours")]
    public required List<OpeningHour> OpeningHours { get; set; }

    [JsonProperty("reservations")]
    public required List<Reservation> Reservations { get; set; }
}

