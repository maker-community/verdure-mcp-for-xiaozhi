// Licensed under the MIT License.
// Copyright (c) 2025 Verdure.McpPlatform

using System.Reflection;

namespace Verdure.McpPlatform.Web.Utils;

/// <summary>
/// Extension methods for <see cref="Assembly"/> to retrieve version information.
/// </summary>
internal static class AssemblyExtensions
{
    /// <summary>
    /// Gets the display version of the assembly.
    /// Priority: AssemblyInformationalVersion > AssemblyFileVersion > AssemblyVersion
    /// Removes commit hash (everything after '+') from InformationalVersion.
    /// </summary>
    /// <param name="assembly">The assembly to get version from.</param>
    /// <returns>The display version string, or null if no version found.</returns>
    public static string? GetDisplayVersion(this Assembly assembly)
    {
        // The package version is stamped into the assembly's AssemblyInformationalVersionAttribute at build time,
        // followed by a '+' and the commit hash, e.g.:
        // [assembly: AssemblyInformationalVersion("1.0.0-preview.1.20240115.1+abc123def456")]
        
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (version is not null)
        {
            // Remove commit hash if present
            var plusIndex = version.IndexOf('+');
            if (plusIndex > 0)
            {
                return version[..plusIndex];
            }
            return version;
        }

        // Fallback to file version (based on CI build number), then assembly version (product stable version)
        version = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
            ?? assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version;

        return version;
    }
}
