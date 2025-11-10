// Licensed under the MIT License.
// Copyright (c) 2025 Verdure.McpPlatform

using System.Reflection;
using System.Runtime.InteropServices;

namespace Verdure.McpPlatform.Api.Utils;

/// <summary>
/// Helper class for version-related information.
/// </summary>
public static class VersionHelpers
{
    private static readonly Lazy<string?> s_cachedRuntimeVersion = new(GetRuntimeVersion);

    /// <summary>
    /// Gets the display version of the Verdure MCP Platform API.
    /// </summary>
    public static string? ApiDisplayVersion { get; } = typeof(VersionHelpers).Assembly.GetDisplayVersion();

    /// <summary>
    /// Gets the .NET runtime version.
    /// </summary>
    public static string? RuntimeVersion => s_cachedRuntimeVersion.Value;

    /// <summary>
    /// Gets the build timestamp (if available).
    /// </summary>
    public static string? BuildTimestamp { get; } = typeof(VersionHelpers).Assembly
        .GetCustomAttribute<AssemblyMetadataAttribute>()?.Value;

    private static string? GetRuntimeVersion()
    {
        var description = RuntimeInformation.FrameworkDescription;

        // Example inputs:
        // ".NET 9.0.0"
        // ".NET 8.0.3"
        // ".NET Core 3.1.32"

        int lastSpace = description.LastIndexOf(' ');
        if (lastSpace >= 0 && lastSpace < description.Length - 1)
        {
            return description[(lastSpace + 1)..];
        }

        return null;
    }

    /// <summary>
    /// Gets the OS description.
    /// </summary>
    public static string OsDescription => RuntimeInformation.OSDescription;

    /// <summary>
    /// Gets the OS architecture.
    /// </summary>
    public static string OsArchitecture => RuntimeInformation.OSArchitecture.ToString();
}
