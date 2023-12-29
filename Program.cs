using Newtonsoft.Json;

// Fetch data from API from address https://api.hel.fi/respa/v1/resource/axwzr3i57yba/?end={endDate}&format=json&start={startDate}
// and find the available times for the next month.
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
// The output should look something like this, with the available times for the next month:
// 2021-09-01   Free:       10:00 - 12:00 (2h)
// 2021-09-04   Free:       11:00 - 14:00 (3h)

var startDate = DateTime.Now;
var endDate = startDate.AddMonths(1);

var url = $"https://api.hel.fi/respa/v1/resource/axwzr3i57yba/?end={endDate:yyyy-MM-dd}&format=json&start={startDate:yyyy-MM-dd}";
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

foreach (var availableTime in availableTimes)
{
    Console.WriteLine($"{availableTime.Date:yyyy-MM-dd}   Free:       {availableTime.Start:HH:mm} - {availableTime.End:HH:mm} ({(availableTime.End - availableTime.Start).TotalHours}h)");
}