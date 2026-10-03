using Azure.Identity;
using Microsoft.Graph;



namespace StowellCoAPI.Services
{
    public class GraphCalendarService
    {
        private readonly GraphServiceClient _graphClient;
        private readonly ILogger<GraphCalendarService> _logger;
        public GraphCalendarService(IConfiguration configuration,ILogger<GraphCalendarService> logger)
        {
            _logger = logger;

            _logger.LogInformation("GraphCalendarService Constructor Start");

            var clientId = configuration["MicrosoftGraph:ClientId"];
            var tenantId = configuration["MicrosoftGraph:TenantId"];
            var clientSecret = configuration["MicrosoftGraph:ClientSecret"];

            var clientSecretCredential = new ClientSecretCredential(
                tenantId,
                clientId,
                clientSecret);

            var scopes = configuration["MicrosoftGraph:Scopes"]
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            _logger.LogInformation("Scopes: {Scopes}", string.Join(",", scopes));

            _graphClient = new GraphServiceClient(
                clientSecretCredential,
                scopes);

            _logger.LogInformation("GraphCalendarService Constructor End");
        }

        /// <summary>Creates an event in the user's calendar. Needs the Calendars.ReadWrite application permission.</summary>
        public async Task<MyEventViewModel> CreateEventAsync(string userEmail, string subject, DateTime start, DateTime end, bool isAllDay)
        {
            var startUtc = start.ToUniversalTime();
            var endUtc = end.ToUniversalTime();

            var created = await _graphClient
                .Users[userEmail]
                .Calendar
                .Events
                .Request()
                .AddAsync(new Event
                {
                    Subject = subject,
                    IsAllDay = isAllDay,
                    Start = new DateTimeTimeZone { DateTime = startUtc.ToString("yyyy-MM-ddTHH:mm:ss"), TimeZone = "UTC" },
                    End = new DateTimeTimeZone { DateTime = endUtc.ToString("yyyy-MM-ddTHH:mm:ss"), TimeZone = "UTC" }
                });

            return new MyEventViewModel
            {
                Subject = created.Subject,
                Start = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc),
                End = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc),
                Organizer = created.Organizer?.EmailAddress?.Name,
                IsAllDay = isAllDay
            };
        }

        public async Task<IEnumerable<MyEventViewModel>> GetUserCalendarAsync(string userEmail)
        {
            _logger.LogError("GetUserCalendarAsync CALLED for {UserEmail}", userEmail);
            try
            {
                var response = await _graphClient
                                .Users[userEmail]
                                .Calendar
                                .Events
                                .Request()
                                .Select("subject,start,end,organizer,isAllDay")
                                .Top(100)
                                .GetAsync();


                if (response == null)
                    return Array.Empty<MyEventViewModel>();

                return response.Select(e => new MyEventViewModel
                {
                    Subject = e.Subject,
                    // Graph returns UTC wall-clock times; mark them UTC so the JSON carries a Z and browsers convert to local time
                    Start = DateTime.SpecifyKind(DateTime.Parse(e.Start.DateTime), DateTimeKind.Utc),
                    End = DateTime.SpecifyKind(DateTime.Parse(e.End.DateTime), DateTimeKind.Utc),
                    Organizer = e.Organizer?.EmailAddress?.Name,
                    IsAllDay = e.IsAllDay ?? false
                });
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"GetUserCalendarAsync Message: {ex.Message}");
                _logger.LogInformation($"GetUserCalendarAsync InnerException: {ex.InnerException?.Message}");
                return Array.Empty<MyEventViewModel>();
            }
        }
    }

    public class MyEventViewModel
    {
        public string Subject { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Organizer { get; set; }
        public bool IsAllDay { get; set; }
    }

    public class CreateCalendarEventRequest
    {
        public string Subject { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool IsAllDay { get; set; }
    }
}