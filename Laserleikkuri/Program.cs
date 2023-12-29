// # Laserleikkuri
//
// Instructions for the task:
// Fetch data from API from address https://api.hel.fi/respa/v1/resource/axwzr3i57yba/?start={startDate}&end={endDate}&format=json
// and find the available times for the next two weeks.
//
// The input looks something like this:
// {
//     "opening_hours": [
//         {
//             "date": "2023-12-01",
//             "opens": "2023-12-01T10:00:00+02:00",
//             "closes": "2023-12-01T14:00:00+02:00"
//         },
//         {
//             "date": "2023-12-02",
//             "opens": "2023-12-02T16:00:00+02:00",
//             "closes": "2023-12-02T19:00:00+02:00"
//         },
//         {
//             "date": "2024-01-01",
//             "opens": null,
//             "closes": null
//         }
//     ],
//     "reservations": [
//         {
//             "begin": "2023-12-01T10:00:00+02:00",
//             "end": "2023-12-01T11:00:00+02:00",
//         },
//         {
//             "begin": "2023-12-01T11:00:00+02:00",
//             "end": "2023-12-01T14:00:00+02:00",
//         }
//     ]
// }
//
// The available times should be stored into a json file on disk.
//
// When the program is started, it should fetch the data from the API and compare it to the data in the json file.
// If there are new available times in the API output that do not exist in the file, the program shall output them to the console.
// Additionally, the program shall always print out all available times to the console.
//
// Program shall output the available times to the console as follows:
//
// (example 1)
// New times:
// *2023-12-01  Free:       10:00 - 11:00 (1h)
//
// Calendar:
//  2023-12-01  Free:       10:00 - 11:00 (1h)
//  2023-12-04  Free:       11:00 - 14:00 (3h)
// (end of example 1)
//
// (example 2)
// No new times available.
//
// Calendar:
//  2023-12-01  Free:       10:00 - 11:00 (1h)
//  2023-12-04  Free:       11:00 - 14:00 (3h)
// (end of example 2)
//
// The file shall be updated with the available times fetched from API.
//
// Program code starts here:

using System.Globalization;
using Newtonsoft.Json;

var startDate = DateTime.Now;
var endDate = startDate.AddDays(14);

var endpoint = $"https://api.hel.fi/respa/v1/resource/axwzr3i57yba/?start={{0}}&end={{1}}&format=json";
var url = string.Format(
    endpoint, 
    startDate.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture), 
    endDate.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));

var client = new HttpClient();
var response = await client.GetAsync(url);
var content = await response.Content.ReadAsStringAsync();
var data = JsonConvert.DeserializeObject<RespaData>(content) ?? throw new Exception("Deserialization failed");

var openingHours = data.OpeningHours
    .Where(x => x.Date >= startDate && x.Date <= endDate)
    .Where(x => x.Opens != null && x.Closes != null)
    .ToList();

var reservations = data.Reservations
    ?.Where(x => x.Begin >= startDate && x.End <= endDate)
    .ToList();

var availableTimes = new List<AvailableTime>();
foreach (var openingHour in openingHours)
{
    var reservationsForDay = reservations?.Where(x => x.Begin.Date == openingHour.Date.Date).ToList() 
        ?? new List<Reservation>();

    if (reservationsForDay.Count == 0)
    {
        availableTimes.Add(new AvailableTime
        {
            Date = openingHour.Date,
            Start = openingHour.Opens ?? throw new Exception("Opening hour opens is null"),
            End = openingHour.Closes ?? throw new Exception("Opening hour closes is null")
        });
    }
    else
    {
        var reservationsForDayOrdered = reservationsForDay
            .OrderBy(x => x.Begin)
            .ToList();

        var lastReservation = reservationsForDayOrdered[0];
        if (lastReservation.Begin > openingHour.Opens)
        {
            availableTimes.Add(new AvailableTime
            {
                Date = openingHour.Date,
                Start = openingHour.Opens ?? throw new Exception("Opening hour opens is null"),
                End = lastReservation.Begin
            });
        }

        for (var i = 0; i < reservationsForDayOrdered.Count - 1; i++)
        {
            var currentReservation = reservationsForDayOrdered[i];
            var nextReservation = reservationsForDayOrdered[i + 1];

            if (nextReservation.Begin > currentReservation.End)
            {
                availableTimes.Add(new AvailableTime
                {
                    Date = openingHour.Date,
                    Start = currentReservation.End,
                    End = nextReservation.Begin
                });
            }
        }

        var firstReservation = reservationsForDayOrdered[^1];
        if (firstReservation.End < openingHour.Closes)
        {
            availableTimes.Add(new AvailableTime
            {
                Date = openingHour.Date,
                Start = firstReservation.End,
                End = openingHour.Closes ?? throw new Exception("Opening hour closes is null")
            });
        }
    }
}

var availableTimesOrdered = availableTimes
    .OrderBy(x => x.Date)
    .ThenBy(x => x.Start)
    .ToList();

List<AvailableTime> availableTimesOld;
if (!File.Exists("availableTimes.json"))
{
    var availableTimesJsonString = JsonConvert.SerializeObject(availableTimesOrdered, Formatting.Indented);
    await File.WriteAllTextAsync("availableTimes.json", availableTimesJsonString);
    availableTimesOld = new List<AvailableTime>();
}
else
{
    var availableTimesJsonOld = await File.ReadAllTextAsync("availableTimes.json");
    availableTimesOld = JsonConvert.DeserializeObject<List<AvailableTime>>(availableTimesJsonOld) ?? throw new Exception("Deserialization failed");
}

var availableTimesJson = JsonConvert.SerializeObject(availableTimesOrdered, Formatting.Indented);
await File.WriteAllTextAsync("availableTimes.json", availableTimesJson);

var newAvailableTimes = availableTimesOrdered
    .Where(x => !availableTimesOld.Any(y => y.Date == x.Date && y.Start == x.Start && y.End == x.End))
    .ToList();

if (newAvailableTimes.Count > 0)
{
    Console.WriteLine("New times:");
    foreach (var newAvailableTime in newAvailableTimes)
    {
        Console.WriteLine($"*{newAvailableTime.Date:yyyy-MM-dd}  Free:       {newAvailableTime.Start:HH:mm} - {newAvailableTime.End:HH:mm} ({(newAvailableTime.End - newAvailableTime.Start).TotalHours}h)");
    }
}
else
{
    Console.WriteLine("No new times available.");
}

Console.WriteLine();
Console.WriteLine("Calendar:");

foreach (var availableTime in availableTimesOrdered)
{
    Console.WriteLine($" {availableTime.Date:yyyy-MM-dd}  Free:       {availableTime.Start:HH:mm} - {availableTime.End:HH:mm} ({(availableTime.End - availableTime.Start).TotalHours}h)");
}