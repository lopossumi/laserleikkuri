using System.Text.Json;
using Newtonsoft.Json;

public class Calendar
{
    [JsonProperty("opening_hours")]
    public required List<OpeningHours> OpeningHours { get; set; }
    [JsonProperty("reservations")]
    public required List<TimeSlot> Reservations { get; set; }
}
