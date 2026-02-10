using Hangfire.Server;

namespace StarterProject.Infrastructure.Hangfire
{
    public class HangfireJobContextFilter : IServerFilter
    {
        public void OnPerforming(PerformingContext context)
        {
            HangfireJobContext.SetJobData(context);
        }

        public void OnPerformed(PerformedContext context)
        {
            HangfireJobContext.SetJobData(null);
        }
    }
}
