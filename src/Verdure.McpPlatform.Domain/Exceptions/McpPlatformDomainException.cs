namespace Verdure.McpPlatform.Domain.Exceptions;

/// <summary>
/// Base exception for domain-level exceptions
/// </summary>
public class McpPlatformDomainException : Exception
{
    public McpPlatformDomainException()
    {
    }

    public McpPlatformDomainException(string message)
        : base(message)
    {
    }

    public McpPlatformDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
