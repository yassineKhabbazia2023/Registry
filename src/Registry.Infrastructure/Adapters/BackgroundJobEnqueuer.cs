using Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Adapters
{
    public class BackgroundJobEnqueuer : IBackgroundJobEnqueuer
    {
        private readonly IBackgroundJobClient backgroundJobClient;

        public BackgroundJobEnqueuer(IBackgroundJobClient backgroundJobClient)
        {
            this.backgroundJobClient = backgroundJobClient;
        }

        public string Enqueue<T>(Expression<Action<T>> methodCall)
        {
            return BackgroundJob.Enqueue<T>(methodCall);
        }
    }
}
