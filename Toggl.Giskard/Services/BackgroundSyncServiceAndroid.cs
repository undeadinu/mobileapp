using Android.App;
using Android.App.Job;
using Android.Content;
using Toggl.Giskard.Extensions;
using Toggl.Foundation.Services;

namespace Toggl.Giskard.Services
{
    public sealed class BackgroundSyncServiceAndroid : BaseBackgroundSyncService
    {
        public override void EnableBackgroundSync()
        {
            var context = Application.Context;
            var jobScheduler = (JobScheduler)context.GetSystemService(Context.JobSchedulerService);
            var periodicity = (long)MinimumBackgroundFetchInterval.TotalMilliseconds;
            var jobInfo = context.CreateBackgroundSyncJobInfo(periodicity);
            jobScheduler.Schedule(jobInfo);
        }

        public override void DisableBackgroundSync()
        {
            var context = Application.Context;
            var jobScheduler = (JobScheduler)context.GetSystemService(Context.JobSchedulerService);
            jobScheduler.Cancel(BackgroundSyncJobSchedulerService.JobId);
        }
    }
}
