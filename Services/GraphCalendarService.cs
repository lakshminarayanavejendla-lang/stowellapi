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
                                .Select("subject,start,end,organizer")
                                .Top(100)
                                .GetAsync();


                if (response == null)
                    return Array.Empty<MyEventViewModel>();

                return response.Select(e => new MyEventViewModel
                {
                    Subject = e.Subject,
                    Start = DateTime.Parse(e.Start.DateTime),
                    End = DateTime.Parse(e.End.DateTime),
                    Organizer = e.Organizer?.EmailAddress?.Name
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
    }
}