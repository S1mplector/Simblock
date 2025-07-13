namespace SimBlock.Presentation.Interfaces
{
    /// <summary>
    /// Interface for monitoring system resource usage
    /// </summary>
    public interface IResourceMonitor : IDisposable
    {
        /// <summary>
        /// Gets the current application CPU usage percentage
        /// </summary>
        float GetCpuUsage();

        /// <summary>
        /// Gets the current application memory usage in MB
        /// </summary>
        long GetApplicationMemoryUsage();

        /// <summary>
        /// Gets the current application working set usage in MB
        /// </summary>
        long GetApplicationWorkingSetUsage();

        /// <summary>
        /// Gets the current application memory usage in MB (matches Task Manager)
        /// </summary>
        long GetTaskManagerMemoryUsage();

        /// <summary>
        /// Gets a compact string representation of resource usage
        /// </summary>
        string GetCompactResourceString();
    }
}
