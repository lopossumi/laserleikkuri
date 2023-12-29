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
// When the program is started, it should periodically fetch the data from the API every 10 minutes and compare it to the data in the json file.
// If there are new available times in the API output that do not exist in the file, the program shall do the following:
// * output the new time to the console
// * update the json file
// * send a telegram message with a telegram bot to a predefined chat id, containing the new time.
//
// Program code starts here:

using Newtonsoft.Json;

namespace Laserleikkuri
{
    class Program
    {
        static readonly HttpClient client = new HttpClient();
        static readonly string url = "https://api.hel.fi/respa/v1/resource/axwzr3i57yba/?start={0}&end={1}&format=json";
        static readonly string availableTimesFile = "availableTimes.json";
        static readonly TimeSpan interval = TimeSpan.FromMinutes(10);
        private static string telegramBotToken = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN") ?? throw new Exception("TELEGRAM_BOT_TOKEN environment variable not set");
        private static string telegramChatId = Environment.GetEnvironmentVariable("TELEGRAM_CHAT_ID") ?? throw new Exception("TELEGRAM_CHAT_ID environment variable not set");

        static async Task Main(string[] args)
        {
            while (true)
            {
                try
                {
                    await CheckAvailability();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                await Task.Delay(interval);
            }
        }

        static async Task CheckAvailability()
        {
            var startDate = DateTime.Now;
            var endDate = startDate.AddDays(30);

            var calendar = await FetchCalendar(startDate, endDate);
            var availableTimes = ParseAvailableTimes(calendar);
            var json = JsonConvert.SerializeObject(availableTimes);

            List<TimeSlot> oldTimes;
            if (!File.Exists(availableTimesFile))
            {
                File.WriteAllText(availableTimesFile, json);
                oldTimes = new List<TimeSlot>();
            }
            else
            {
                var oldJson = File.ReadAllText(availableTimesFile);
                oldTimes = JsonConvert.DeserializeObject<List<TimeSlot>>(oldJson) ?? new List<TimeSlot>();
            }

            var newTimes = availableTimes.Except(oldTimes).ToList();

            if (newTimes.Any())
            {
                var output = newTimes.Select(x => x.ToString()).ToList();
                Console.WriteLine(string.Join(Environment.NewLine, output));
                await SendTelegramMessage($"New available times:{Environment.NewLine}{string.Join(Environment.NewLine, output)}");
            }
            File.WriteAllText(availableTimesFile, json);
            return;
        }

        private static async Task<Calendar> FetchCalendar(DateTime startDate, DateTime endDate)
        {
            var response = await client.GetAsync(string.Format(url, startDate.GetDateString(), endDate.GetIsoString()));
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            var calendar = JsonConvert.DeserializeObject<Calendar>(responseBody) 
                ?? throw new Exception("Failed to parse response");
            return calendar;
        }

        private static List<TimeSlot> ParseAvailableTimes(Calendar data)
        {
            var availableTimes = new List<TimeSlot>();
            foreach (var openingHours in data.OpeningHours)
            {
                if (openingHours.Opens == null || openingHours.Closes == null)
                {
                    continue;
                }
                var opens = (DateTime)openingHours.Opens;
                var closes = (DateTime)openingHours.Closes;
                var reservations = data.Reservations.Where(r => r.Begin >= opens && r.End <= closes);
                var available = new List<TimeSlot>();
                var begin = opens;
                
                foreach (var reservation in reservations)
                {
                    if (reservation.Begin > begin)
                    {
                        available.Add(new TimeSlot { Begin = begin, End = reservation.Begin });
                    }
                    begin = reservation.End;
                }
                if (begin < closes)
                {
                    available.Add(new TimeSlot { Begin = begin, End = closes });
                }
                availableTimes.AddRange(available);
            }
            return availableTimes;
        }

        static async Task SendTelegramMessage(string message)
        {
            var response = await client.GetAsync($"https://api.telegram.org/bot{telegramBotToken}/sendMessage?chat_id={telegramChatId}&text={message}");
            response.EnsureSuccessStatusCode();
        }
    }
}
