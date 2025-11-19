using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Verdure.McpPlatform.Tests;

/// <summary>
/// 测试不同MCP服务的兼容性
/// Tests compatibility with different MCP services
/// </summary>
/// <remarks>
/// 本测试类用于验证C# SDK对各种MCP服务端的兼容性，包括：
/// This test class validates C# SDK compatibility with various MCP servers including:
/// 
/// 1. Streamable HTTP 协议服务 (Streamable HTTP protocol servers)
/// 2. SSE (Server-Sent Events) 协议服务 (SSE protocol servers)  
/// 3. Stdio 协议服务 (Stdio protocol servers)
/// 4. 自动检测模式 (Auto-detection mode)
/// 
/// 对比分析：
/// Comparison analysis:
/// 
/// Cherry Studio 使用 TypeScript SDK (@modelcontextprotocol/sdk):
/// - 支持 stdio, SSE, streamableHttp, inMemory 四种传输类型
/// - 使用 Client, StdioClientTransport, SSEClientTransport, 
///   StreamableHTTPClientTransport, InMemoryTransport
/// 
/// C# SDK 差异：
/// C# SDK differences:
/// - 支持 Stdio, SSE, StreamableHttp 三种传输类型
/// - HttpClientTransport 支持 AutoDetect 模式（先尝试 StreamableHttp，失败则回退到 SSE）
/// - 使用 HttpClientTransportOptions.TransportMode 来指定传输模式
/// 
/// 已知兼容性问题：
/// Known compatibility issues:
/// - 某些 Streamable HTTP 服务器（如 pozansky-stock-server）在 Cherry Studio 可用但 C# SDK 失败
/// - 可能的原因：HTTP 头处理、内容协商、会话管理等差异
/// </remarks>
public class McpServiceCompatibilityTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ILoggerFactory _loggerFactory;

    public McpServiceCompatibilityTests(ITestOutputHelper output)
    {
        _output = output;
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
    }

    public void Dispose()
    {
        _loggerFactory?.Dispose();
    }

    /// <summary>
    /// 测试配置 - 包含各种 MCP 服务器的连接信息
    /// Test configurations - contains connection info for various MCP servers
    /// </summary>
    public static IEnumerable<object[]> GetTestConfigurations()
    {
        // Streamable HTTP 服务器
        yield return new object[]
        {
            "ModelScope Pozansky Stock Server",
            "streamable_http",
            "https://mcp.api-inference.modelscope.net/f39aba069a8140/mcp",
            null,
            null
        };

        // 本地测试服务器（如果有）
        yield return new object[]
        {
            "Local Test Server (Streamable HTTP)",
            "streamable_http",
            "http://localhost:3001",
            null,
            null
        };

        yield return new object[]
        {
            "Local Test Server (SSE)",
            "sse",
            "http://localhost:3001/sse",
            null,
            null
        };

        // 注意：Stdio 服务器需要特殊处理，见 TestStdioService 方法
    }

    /// <summary>
    /// 测试 Streamable HTTP 和 SSE 传输类型
    /// Tests Streamable HTTP and SSE transport types
    /// </summary>
    [Theory]
    [MemberData(nameof(GetTestConfigurations))]
    public async Task TestHttpBasedService(
        string serviceName,
        string transportType,
        string endpoint,
        string? username,
        string? password)
    {
        _output.WriteLine($"测试服务: {serviceName}");
        _output.WriteLine($"传输类型: {transportType}");
        _output.WriteLine($"端点: {endpoint}");

        try
        {
            // 创建 HTTP 客户端传输选项
            var transportOptions = new HttpClientTransportOptions
            {
                Endpoint = new Uri(endpoint),
                Name = serviceName,
                // 根据传输类型设置
                TransportMode = transportType switch
                {
                    "streamable_http" => HttpTransportMode.StreamableHttp,
                    "sse" => HttpTransportMode.Sse,
                    _ => HttpTransportMode.AutoDetect
                }
            };

            // 如果有认证信息，添加到 HTTP 头
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var credentials = Convert.ToBase64String(
                    System.Text.Encoding.ASCII.GetBytes($"{username}:{password}"));
                transportOptions.AdditionalHeaders = new Dictionary<string, string>
                {
                    ["Authorization"] = $"Basic {credentials}"
                };
            }

            // 创建传输层
            await using var transport = new HttpClientTransport(transportOptions, _loggerFactory);

            // 创建 MCP 客户端
            var clientOptions = new McpClientOptions
            {
                ClientInfo = new Implementation
                {
                    Name = "Verdure MCP Platform Compatibility Test",
                    Version = "1.0.0"
                }
            };

            await using var client = await McpClient.CreateAsync(
                transport,
                clientOptions,
                _loggerFactory);

            _output.WriteLine($"✓ 成功连接到 {serviceName}");

            // 验证服务器信息
            Assert.NotNull(client.ServerInfo);
            _output.WriteLine($"服务器名称: {client.ServerInfo.Name}");
            _output.WriteLine($"服务器版本: {client.ServerInfo.Version}");

            // 验证协议版本
            Assert.NotNull(client.NegotiatedProtocolVersion);
            _output.WriteLine($"协议版本: {client.NegotiatedProtocolVersion}");

            // 测试 Ping
            try
            {
                await client.PingAsync(CancellationToken.None);
                _output.WriteLine("✓ Ping 成功");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"⚠ Ping 失败: {ex.Message}");
            }

            // 列出可用工具
            try
            {
                var tools = await client.ListToolsAsync(cancellationToken: CancellationToken.None);
                _output.WriteLine($"✓ 发现 {tools.Count} 个工具:");
                foreach (var tool in tools)
                {
                    _output.WriteLine($"  - {tool.Name}: {tool.Description}");
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"⚠ 列出工具失败: {ex.Message}");
            }

            // 列出可用提示
            try
            {
                var prompts = await client.ListPromptsAsync(cancellationToken: CancellationToken.None);
                _output.WriteLine($"✓ 发现 {prompts.Count} 个提示:");
                foreach (var prompt in prompts)
                {
                    _output.WriteLine($"  - {prompt.Name}: {prompt.Description}");
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"⚠ 列出提示失败: {ex.Message}");
            }

            // 列出可用资源
            try
            {
                var resources = await client.ListResourcesAsync(cancellationToken: CancellationToken.None);
                _output.WriteLine($"✓ 发现 {resources.Count} 个资源:");
                foreach (var resource in resources)
                {
                    _output.WriteLine($"  - {resource.Uri}: {resource.Name}");
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"⚠ 列出资源失败: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"✗ 连接失败: {ex.Message}");
            _output.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            
            // 记录详细的异常信息以便调试
            if (ex.InnerException != null)
            {
                _output.WriteLine($"内部异常: {ex.InnerException.Message}");
                _output.WriteLine($"内部堆栈: {ex.InnerException.StackTrace}");
            }

            throw;
        }
    }

    /// <summary>
    /// 测试 Stdio 传输类型（本地进程通信）
    /// Tests Stdio transport type (local process communication)
    /// </summary>
    [Fact(Skip = "需要本地安装 npx 和 MCP 服务器")]
    public async Task TestStdioService()
    {
        var serviceName = "Everything Server (Stdio)";
        _output.WriteLine($"测试服务: {serviceName}");

        try
        {
            // 创建 Stdio 传输选项
            var transportOptions = new StdioClientTransportOptions
            {
                Command = "npx",
                Arguments = new[] { "-y", "@modelcontextprotocol/server-everything" },
                Name = serviceName
            };

            // 创建传输层
            var transport = new StdioClientTransport(transportOptions, _loggerFactory);

            // 创建 MCP 客户端
            var clientOptions = new McpClientOptions
            {
                ClientInfo = new Implementation
                {
                    Name = "Verdure MCP Platform Compatibility Test",
                    Version = "1.0.0"
                }
            };

            await using var client = await McpClient.CreateAsync(
                transport,
                clientOptions,
                _loggerFactory);

            _output.WriteLine($"✓ 成功连接到 {serviceName}");

            // 验证服务器信息
            Assert.NotNull(client.ServerInfo);
            _output.WriteLine($"服务器名称: {client.ServerInfo.Name}");

            // 列出工具
            var tools = await client.ListToolsAsync(cancellationToken: CancellationToken.None);
            _output.WriteLine($"✓ 发现 {tools.Count} 个工具");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"✗ 连接失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 测试自动检测模式（先尝试 Streamable HTTP，失败则回退到 SSE）
    /// Tests auto-detection mode (tries Streamable HTTP first, falls back to SSE on failure)
    /// </summary>
    [Theory]
    [InlineData("http://localhost:3001")]
    public async Task TestAutoDetectMode(string endpoint)
    {
        _output.WriteLine($"测试自动检测模式: {endpoint}");

        try
        {
            var transportOptions = new HttpClientTransportOptions
            {
                Endpoint = new Uri(endpoint),
                Name = "Auto-Detect Test",
                TransportMode = HttpTransportMode.AutoDetect // 自动检测
            };

            await using var transport = new HttpClientTransport(transportOptions, _loggerFactory);

            var clientOptions = new McpClientOptions
            {
                ClientInfo = new Implementation
                {
                    Name = "Verdure MCP Platform Auto-Detect Test",
                    Version = "1.0.0"
                }
            };

            await using var client = await McpClient.CreateAsync(
                transport,
                clientOptions,
                _loggerFactory);

            _output.WriteLine("✓ 自动检测成功");
            _output.WriteLine($"使用的传输类型: {client.ServerInfo?.Name}");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"✗ 自动检测失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 测试连接错误处理
    /// Tests connection error handling
    /// </summary>
    [Fact]
    public async Task TestConnectionErrorHandling()
    {
        _output.WriteLine("测试连接错误处理");

        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = new Uri("http://localhost:65535"), // 不太可能使用的端口
            Name = "Error Test",
            TransportMode = HttpTransportMode.StreamableHttp,
            ConnectionTimeout = TimeSpan.FromSeconds(2) // 短超时
        };

        await using var transport = new HttpClientTransport(transportOptions, _loggerFactory);

        var clientOptions = new McpClientOptions
        {
            ClientInfo = new Implementation
            {
                Name = "Error Test Client",
                Version = "1.0.0"
            }
        };

        // 应该抛出异常
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await using var client = await McpClient.CreateAsync(
                transport,
                clientOptions,
                _loggerFactory);
        });

        _output.WriteLine("✓ 正确处理了连接错误");
    }

    /// <summary>
    /// 测试特定工具调用
    /// Tests specific tool invocation
    /// </summary>
    [Theory(Skip = "需要实际的 MCP 服务器")]
    [InlineData("http://localhost:3001", "echo", """{"message": "Hello, MCP!"}""")]
    public async Task TestToolInvocation(string endpoint, string toolName, string argumentsJson)
    {
        _output.WriteLine($"测试工具调用: {toolName}");

        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = new Uri(endpoint),
            Name = "Tool Invocation Test",
            TransportMode = HttpTransportMode.AutoDetect
        };

        await using var transport = new HttpClientTransport(transportOptions, _loggerFactory);

        var clientOptions = new McpClientOptions
        {
            ClientInfo = new Implementation
            {
                Name = "Tool Test Client",
                Version = "1.0.0"
            }
        };

        await using var client = await McpClient.CreateAsync(
            transport,
            clientOptions,
            _loggerFactory);

        // 解析参数
        var argumentsDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson);

        // 调用工具
        var result = await client.CallToolAsync(
            toolName,
            argumentsDict,
            cancellationToken: CancellationToken.None);

        _output.WriteLine($"工具调用结果: {result}");
        Assert.NotNull(result);
    }
}
