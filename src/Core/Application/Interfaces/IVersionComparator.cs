namespace SimBlock.Core.Application.Interfaces
{
    public interface IVersionComparator
    {
        /// <summary>
        /// Compares two version strings
        /// </summary>
        /// <param name="version1">First version to compare</param>
        /// <param name="version2">Second version to compare</param>
        /// <returns>
        /// -1 if version1 is less than version2
        /// 0 if versions are equal
        /// 1 if version1 is greater than version2
        /// </returns>
        int Compare(string version1, string version2);

        /// <summary>
        /// Checks if the new version is newer than the current version
        /// </summary>
        /// <param name="currentVersion">Current version</param>
        /// <param name="newVersion">New version to check</param>
        /// <returns>True if new version is newer</returns>
        bool IsNewerVersion(string currentVersion, string newVersion);

        /// <summary>
        /// Normalizes a version string (removes 'v' prefix, handles pre-release tags)
        /// </summary>
        /// <param name="version">Version string to normalize</param>
        /// <returns>Normalized version string</returns>
        string NormalizeVersion(string version);
    }
}