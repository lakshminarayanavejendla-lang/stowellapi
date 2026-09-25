namespace StowellCoAPI.DTO
{
    // Job access audit trail (new feature, not a Blazor port). Backed by dbo.JobAccessLog +
    // JobAccess_Log (insert) / JobAccess_GetLog (read, joins StowellSandbox.dbo.actrec for
    // JobName) - see JobAccessController.cs. Logged from ProjectOverviewComponent.ngOnInit
    // (Angular) each time a job's overview page is opened.

    public class JobAccessLogRequestDto
    {
        public string JobId { get; set; }
        public string UserEmail { get; set; }
    }

    public class JobAccessLogRecord
    {
        public long Id { get; set; }
        public string JobId { get; set; }
        public string JobName { get; set; }
        public string UserEmail { get; set; }
        public DateTime AccessTime { get; set; }
    }
}
