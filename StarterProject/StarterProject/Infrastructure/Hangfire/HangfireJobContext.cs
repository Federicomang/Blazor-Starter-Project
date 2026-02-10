using Hangfire.Server;

namespace StarterProject.Infrastructure.Hangfire
{
    public interface IHangfireJobContext
    {
        PerformingContext? JobData { get; }
        string? JobId { get; }
        bool IsJobExecution { get; }
    }

    public class HangfireJobContext : IHangfireJobContext
    {
        private static readonly AsyncLocal<PerformingContext?> _jobData = new();

        public PerformingContext? JobData => _jobData.Value;
        public string? JobId => _jobData.Value?.BackgroundJob.Id;
        public bool IsJobExecution => _jobData.Value != null;

        internal static void SetJobData(PerformingContext? jobData)
        {
            _jobData.Value = jobData;
        }
    }
}
