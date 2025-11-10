// Licensed under the MIT License.
// Copyright (c) 2025 Verdure.McpPlatform

using Verdure.McpPlatform.Api.Utils;

namespace Verdure.McpPlatform.Api.Apis;

/// <summary>
/// API endpoints for version information.
/// </summary>
public static class VersionApi
{
    /// <summary>
    /// Maps the version API endpoints.
    /// </summary>
    public static RouteGroupBuilder MapVersionApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/version")
            .WithTags("Version")
            .WithOpenApi();

        api.MapGet("/", GetVersionInfo)
            .WithName("GetVersionInfo")
            .WithSummary("Get API version information")
            .Produces<VersionInfoResponse>();

        return api;
    }

    private static IResult GetVersionInfo()
    {
        var response = new VersionInfoResponse
        {
            Version = VersionHelpers.ApiDisplayVersion ?? "Unknown",
            RuntimeVersion = VersionHelpers.RuntimeVersion,
            BuildTimestamp = VersionHelpers.BuildTimestamp,
            OsDescription = VersionHelpers.OsDescription,
            OsArchitecture = VersionHelpers.OsArchitecture,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        };

        return Results.Ok(response);
    }
}

/// <summary>
/// Response DTO for version information.
/// </summary>
public record VersionInfoResponse
{
    /// <summary>
    /// Gets or sets the API version (e.g., "1.0.0", "1.0.0-preview.1").
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets or sets the .NET runtime version (e.g., "9.0.0").
    /// </summary>
    public string? RuntimeVersion { get; init; }

    /// <summary>
    /// Gets or sets the build timestamp (if available).
    /// </summary>
    public string? BuildTimestamp { get; init; }

    /// <summary>
    /// Gets or sets the OS description.
    /// </summary>
    public string? OsDescription { get; init; }

    /// <summary>
    /// Gets or sets the OS architecture (e.g., "X64", "Arm64").
    /// </summary>
    public string? OsArchitecture { get; init; }

    /// <summary>
    /// Gets or sets the current environment (Development, Staging, Production).
    /// </summary>
    public string? Environment { get; init; }
}
