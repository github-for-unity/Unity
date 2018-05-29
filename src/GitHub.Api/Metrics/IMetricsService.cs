using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    public interface IMetricsService
    {
        /// <summary>
        /// Posts the provided usage model.
        /// </summary>
        Task PostUsage(List<Usage> model);
    }
}
