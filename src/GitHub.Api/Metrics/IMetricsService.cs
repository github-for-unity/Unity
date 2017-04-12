using System;
using System.Threading.Tasks;
using GitHub.Models;

namespace GitHub.Services
{
    public interface IMetricsService
    {
        /// <summary>
        /// Posts the provided usage model.
        /// </summary>
        Task PostUsage(UsageModel model);
        
        /// <summary>
        /// Sends an empty request that indicates that the user has chosen to opt out of usage
        /// tracking.
        /// </summary>
        Task SendOptOut();

        /// <summary>
        /// Sends an empty request that indicates that the user has chosen to opt back in to
        /// usage tracking.
        /// </summary>
        Task SendOptIn();
    }
}
