// Licensed under the MIT License.
// Copyright (c) 2025 Verdure.McpPlatform

using System.Reflection;
using System.Runtime.InteropServices;

namespace Verdure.McpPlatform.Web.Utils;

/// <summary>
/// Helper class for version-related information.
/// </summary>
public static class VersionHelpers
{
    private static readonly Lazy<string?> s_cachedRuntimeVersion = new(GetRuntimeVersion);

    /// <summary>
    /// Gets the display version of the Verdure MCP Platform Web.
    /// </summary>
    public static string? WebDisplayVersion { get; } = typeof(VersionHelpers).Assembly.GetDisplayVersion();

    /// <summary>
    /// Gets the .NET runtime version.
    /// </summary>
    public static string? RuntimeVersion => s_cachedRuntimeVersion.Value;

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
}
