using SimBlock.Core.Application.Interfaces;
using System;
using System.Text.RegularExpressions;

namespace SimBlock.Core.Application.Services
{
    public class VersionComparator : IVersionComparator
    {
        private static readonly Regex VersionRegex = new(@"^v?(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z0-9\-\.]+))?(?:\+([a-zA-Z0-9\-\.]+))?$",
            RegexOptions.Compiled);

        public int Compare(string version1, string version2)
        {
            if (string.IsNullOrWhiteSpace(version1) && string.IsNullOrWhiteSpace(version2))
                return 0;

            if (string.IsNullOrWhiteSpace(version1))
                return -1;

            if (string.IsNullOrWhiteSpace(version2))
                return 1;

            var normalizedVersion1 = NormalizeVersion(version1);
            var normalizedVersion2 = NormalizeVersion(version2);

            var parsedVersion1 = ParseVersion(normalizedVersion1);
            var parsedVersion2 = ParseVersion(normalizedVersion2);

            // Compare major version
            if (parsedVersion1.Major != parsedVersion2.Major)
                return parsedVersion1.Major.CompareTo(parsedVersion2.Major);

            // Compare minor version
            if (parsedVersion1.Minor != parsedVersion2.Minor)
                return parsedVersion1.Minor.CompareTo(parsedVersion2.Minor);

            // Compare patch version
            if (parsedVersion1.Patch != parsedVersion2.Patch)
                return parsedVersion1.Patch.CompareTo(parsedVersion2.Patch);

            // Compare pre-release versions
            return ComparePreRelease(parsedVersion1.PreRelease, parsedVersion2.PreRelease);
        }

        public bool IsNewerVersion(string currentVersion, string newVersion)
        {
            return Compare(currentVersion, newVersion) < 0;
        }

        public string NormalizeVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return "0.0.0";

            // Remove 'v' prefix if present
            var normalized = version.Trim();
            if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(1);

            return normalized;
        }

        private ParsedVersion ParseVersion(string version)
        {
            var match = VersionRegex.Match(version);
            if (!match.Success)
            {
                // Fallback for simple version formats
                var parts = version.Split('.');
                if (parts.Length >= 3 &&
                    int.TryParse(parts[0], out var major) &&
                    int.TryParse(parts[1], out var minor) &&
                    int.TryParse(parts[2], out var patch))
                {
                    return new ParsedVersion(major, minor, patch, string.Empty);
                }

                throw new ArgumentException($"Invalid version format: {version}");
            }

            return new ParsedVersion(
                int.Parse(match.Groups[1].Value),
                int.Parse(match.Groups[2].Value),
                int.Parse(match.Groups[3].Value),
                match.Groups[4].Value
            );
        }

        private int ComparePreRelease(string preRelease1, string preRelease2)
        {
            // If both are empty (stable releases), they are equal
            if (string.IsNullOrEmpty(preRelease1) && string.IsNullOrEmpty(preRelease2))
                return 0;

            // Stable release is greater than pre-release
            if (string.IsNullOrEmpty(preRelease1))
                return 1;

            if (string.IsNullOrEmpty(preRelease2))
                return -1;

            // Compare pre-release versions lexicographically
            return string.Compare(preRelease1, preRelease2, StringComparison.OrdinalIgnoreCase);
        }

        private record ParsedVersion(int Major, int Minor, int Patch, string PreRelease);
    }
}