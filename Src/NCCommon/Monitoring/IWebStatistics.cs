using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    public interface IWebStatistics
    {
        /// <summary>
        /// Get Perfmon categories currently residing on the machine
        /// </summary>
        /// <returns>list of category names and descriptions</returns>
        Dictionary<string, string> GetPerfmonCategoryNames();

        /// <summary>
        /// Get instance names of a Perfmon category
        /// </summary>
        /// <param name="categoryName">Name of perfmon category from <seealso cref="GetPerfmonCategories"/></param>
        /// <returns></returns>
        string[] GetPerfmonInstanceNames(string categoryName);

        /// <summary>
        /// Get counter names of a perfmon category
        /// </summary>
        /// <param name="categoryName">name of perfmon category</param>
        /// <param name="instanceName">name of instance of perfmon category</param>
        /// <returns></returns>
        string[] GetPerfmonCounterNames(string categoryName, string instanceName);

        /// <summary>
        /// Get Perfmon Value
        /// This is used by web monitor
        /// </summary>
        /// <param name="cacheId">name of cache</param>
        /// <param name="counterName">perfmon counter name</param>
        /// <param name="category">perfmon counter category</param>
        /// <param name="processInstance">perfmoncache instance</param>
        /// <param name="replica">is the node a replica or not</param>
        /// <returns>value of perfmon counter</returns>
        double GetPerfmonValue(string cacheId, string counterName, string category, string processInstance, bool replica = false);

        /// <summary>
        /// Get Perfmon values for counters
        /// </summary>
        /// <param name="counterDetails"></param>
        /// <returns></returns>
        List<PerfmonCounterDetails> GetPerfmonValues(List<PerfmonCounterDetails> counterDetails, string cacheId);
    }
}
