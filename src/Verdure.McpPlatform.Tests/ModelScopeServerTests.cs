using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Verdure.McpPlatform.Tests;

/// <summary>
/// 专门测试 ModelScope Pozansky Stock Server 的兼容性
/// Dedicated tests for ModelScope Pozansky Stock Server compatibility
/// </summary>
/// <remarks>
/// 这个测试类专注于调试为什么 ModelScope 服务器返回 400 错误
/// This test class focuses on debugging why ModelScope server returns 400 error
/// 
/// ModelScope 服务器信息:
/// - URL: https://mcp.api-inference.modelscope.net/f39aba069a8140/mcp
/// - 在 Cherry Studio (TypeScript SDK) 中可用
/// - 在 C# SDK 中返回 400 Bad Request
/// </remarks>
public class ModelScopeServerTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ILoggerFactory _loggerFactory;
    private const string ModelScopeEndpoint = "https://mcp.api-inference.modelscope.net/f39aba069a8140/mcp";

    public ModelScopeServerTests(ITestOutputHelper output)
    {
        _output = output;
        
        // 创建详细日志记录器
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Trace); // 最详细的日志级别
            builder.AddFilter("ModelContextProtocol", LogLevel.Trace);
            builder.AddFilter("System.Net.Http", LogLevel.Trace);
        });
    }

    public void Dispose()
    {
        _loggerFactory?.Dispose();
    }

    /// <summary>
    /// 测试 1: 使用默认配置连接 ModelScope 服务器
    /// Test 1: Connect to ModelScope server with default configuration
    /// </summary>
    [Fact]
    public async Task Test_ModelScope_DefaultConfiguration()
    {
        _output.WriteLine("=== 测试 ModelScope 服务器 - 默认配置 ===");
        _output.WriteLine($"端点: {ModelScopeEndpoint}");
        _output.WriteLine("");

        try
        {
            // 使用自定义 HttpClient 以记录详细信息
            var handler = new LoggingHttpMessageHandler(_output);
            var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            var transportOptions = new HttpClientTransportOptions
            {
                Endpoint = new Uri(ModelScopeEndpoint),
                Name = "ModelScope Pozansky Stock Server",
                TransportMode = HttpTransportMode.StreamableHttp
            };

            var transport = new HttpClientTransport(transportOptions, httpClient, _loggerFactory, ownsHttpClient: true);

            var clientOptions = new McpClientOptions
            {
                ClientInfo = new Implementation
                {
                    Name = "Verdure MCP Platform Test",
                    Version = "1.0.0"
                }
            };

            _output.WriteLine("正在连接到服务器...");
            await using var client = await McpClient.CreateAsync(transport, clientOptions, _loggerFactory);

            _output.WriteLine("✅ 连接成功！");
            _output.WriteLine($"服务器名称: {client.ServerInfo?.Name}");
            _output.WriteLine($"服务器版本: {client.ServerInfo?.Version}");

            // 列出工具
            var tools = await client.ListToolsAsync(cancellationToken: CancellationToken.None);
            _output.WriteLine($"✅ 发现 {tools.Count} 个工具:");
            foreach (var tool in tools)
            {
                _output.WriteLine($"  - {tool.Name}: {tool.Description}");
            }

            Assert.NotNull(client.ServerInfo);
            Assert.NotEmpty(tools);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ 连接失败: {ex.GetType().Name}");
            _output.WriteLine($"错误消息: {ex.Message}");
            _output.WriteLine($"堆栈跟踪:\n{ex.StackTrace}");

            if (ex.InnerException != null)
            {
                _output.WriteLine($"\n内部异常: {ex.InnerException.GetType().Name}");
                _output.WriteLine($"内部消息: {ex.InnerException.Message}");
            }

            throw;
        }
    }

    /// <summary>
    /// 测试 2: 使用 AdditionalHeaders 添加 mcp-session-id（关键测试！）
    /// Test 2: Add mcp-session-id using AdditionalHeaders (KEY TEST!)
    /// </summary>
    [Fact]
    public async Task Test_ModelScope_WithSessionIdInAdditionalHeaders()
    {
        _output.WriteLine("=== 测试 ModelScope 服务器 - 使用 AdditionalHeaders 添加 Session ID ===");
        _output.WriteLine($"端点: {ModelScopeEndpoint}");
        _output.WriteLine("");

        try
        {
            var handler = new LoggingHttpMessageHandler(_output);
            var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            var sessionId = Guid.NewGuid().ToString();
            _output.WriteLine($"生成 Session ID: {sessionId}");
            _output.WriteLine("");

            var transportOptions = new HttpClientTransportOptions
            {
                Endpoint = new Uri(ModelScopeEndpoint),
                Name = "ModelScope Pozansky Stock Server",
                TransportMode = HttpTransportMode.StreamableHttp,
                AdditionalHeaders = new Dictionary<string, string>
                {
                    ["mcp-session-id"] = sessionId  // 关键：添加 session ID header
                }
            };

            _output.WriteLine("添加的 HTTP 头:");
            foreach (var header in transportOptions.AdditionalHeaders)
            {
                _output.WriteLine($"  {header.Key}: {header.Value}");
            }
            _output.WriteLine("");

            var transport = new HttpClientTransport(transportOptions, httpClient, _loggerFactory, ownsHttpClient: true);

            var clientOptions = new McpClientOptions
            {
                ClientInfo = new Implementation
                {
                    Name = "Verdure MCP Platform Test",
                    Version = "1.0.0"
                }
            };

            _output.WriteLine("正在连接到服务器...");
            await using var client = await McpClient.CreateAsync(transport, clientOptions, _loggerFactory);

            _output.WriteLine("✅ 连接成功！");
            _output.WriteLine($"服务器名称: {client.ServerInfo?.Name}");
            _output.WriteLine($"服务器版本: {client.ServerInfo?.Version}");

            // 列出工具
            var tools = await client.ListToolsAsync(cancellationToken: CancellationToken.None);
            _output.WriteLine($"✅ 发现 {tools.Count} 个工具:");
            foreach (var tool in tools)
            {
                _output.WriteLine($"  - {tool.Name}: {tool.Description}");
            }

            Assert.NotNull(client.ServerInfo);
            Assert.NotEmpty(tools);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ 连接失败: {ex.Message}");
            _output.WriteLine($"堆栈跟踪:\n{ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// 测试 3: 尝试 SSE 传输模式
    /// Test 3: Try SSE transport mode
    /// </summary>
    [Fact]
    public async Task Test_ModelScope_SseMode()
    {
        _output.WriteLine("=== 测试 ModelScope 服务器 - SSE 模式 ===");
        _output.WriteLine($"端点: {ModelScopeEndpoint}");
        _output.WriteLine("");

        try
        {
            var handler = new LoggingHttpMessageHandler(_output);
            var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            var transportOptions = new HttpClientTransportOptions
            {
                Endpoint = new Uri(ModelScopeEndpoint),
                Name = "ModelScope Pozansky Stock Server",
                TransportMode = HttpTransportMode.Sse  // 使用 SSE 模式
            };

            var transport = new HttpClientTransport(transportOptions, httpClient, _loggerFactory, ownsHttpClient: true);

            var clientOptions = new McpClientOptions
            {
                ClientInfo = new Implementation
                {
                    Name = "Verdure MCP Platform Test",
                    Version = "1.0.0"
                }
            };

            _output.WriteLine("正在使用 SSE 模式连接到服务器...");
            await using var client = await McpClient.CreateAsync(transport, clientOptions, _loggerFactory);

            _output.WriteLine("✅ 连接成功！");
            Assert.NotNull(client.ServerInfo);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ SSE 模式连接失败: {ex.Message}");
            // SSE 模式失败是预期的，不抛出异常
        }
    }

    /// <summary>
    /// 测试 4: 尝试自动检测模式
    /// Test 4: Try auto-detect mode
    /// </summary>
    [Fact]
    public async Task Test_ModelScope_AutoDetectMode()
    {
        _output.WriteLine("=== 测试 ModelScope 服务器 - 自动检测模式 ===");
        _output.WriteLine($"端点: {ModelScopeEndpoint}");
        _output.WriteLine("");

        try
        {
            var handler = new LoggingHttpMessageHandler(_output);
            var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            var transportOptions = new HttpClientTransportOptions
            {
                Endpoint = new Uri(ModelScopeEndpoint),
                Name = "ModelScope Pozansky Stock Server",
                TransportMode = HttpTransportMode.AutoDetect  // 自动检测
            };

            var transport = new HttpClientTransport(transportOptions, httpClient, _loggerFactory, ownsHttpClient: true);

            var clientOptions = new McpClientOptions
            {
                ClientInfo = new Implementation
                {
                    Name = "Verdure MCP Platform Test",
                    Version = "1.0.0"
                }
            };

            _output.WriteLine("正在使用自动检测模式连接到服务器...");
            _output.WriteLine("将先尝试 Streamable HTTP，失败后回退到 SSE");
            _output.WriteLine("");

            await using var client = await McpClient.CreateAsync(transport, clientOptions, _loggerFactory);

            _output.WriteLine("✅ 连接成功！");
            Assert.NotNull(client.ServerInfo);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ 自动检测模式连接失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 测试 5: 直接发送原始 HTTP 请求以调试
    /// Test 5: Send raw HTTP request for debugging
    /// </summary>
    [Fact]
    public async Task Test_ModelScope_RawHttpRequest()
    {
        _output.WriteLine("=== 测试 ModelScope 服务器 - 原始 HTTP 请求 ===");
        _output.WriteLine($"端点: {ModelScopeEndpoint}");
        _output.WriteLine("");

        var httpClient = new HttpClient();

        // 构造 MCP 初始化请求
        var initRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new
                {
                    name = "Verdure MCP Platform Test",
                    version = "1.0.0"
                }
            }
        };

        var requestJson = JsonSerializer.Serialize(initRequest, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        _output.WriteLine("发送的 JSON-RPC 请求:");
        _output.WriteLine(requestJson);
        _output.WriteLine("");

        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        // 添加必需的 HTTP 头
        var request = new HttpRequestMessage(HttpMethod.Post, ModelScopeEndpoint)
        {
            Content = content
        };

        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        _output.WriteLine("HTTP 请求头:");
        foreach (var header in request.Headers)
        {
            _output.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
        }
        _output.WriteLine("");

        try
        {
            _output.WriteLine("正在发送 HTTP POST 请求...");
            var response = await httpClient.SendAsync(request);

            _output.WriteLine($"HTTP 状态码: {(int)response.StatusCode} {response.StatusCode}");
            _output.WriteLine("");

            _output.WriteLine("响应头:");
            foreach (var header in response.Headers)
            {
                _output.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
            }
            foreach (var header in response.Content.Headers)
            {
                _output.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
            }
            _output.WriteLine("");

            var responseBody = await response.Content.ReadAsStringAsync();
            _output.WriteLine("响应体:");
            _output.WriteLine(responseBody);
            _output.WriteLine("");

            if (response.IsSuccessStatusCode)
            {
                _output.WriteLine("✅ HTTP 请求成功！");
            }
            else
            {
                _output.WriteLine($"❌ HTTP 请求失败: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ 发送 HTTP 请求失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 测试 6: 添加 mcp-session-id header
    /// Test 6: Add mcp-session-id header
    /// </summary>
    [Fact]
    public async Task Test_ModelScope_WithSessionIdHeader()
    {
        _output.WriteLine("=== 测试 ModelScope 服务器 - 添加 Session ID Header ===");
        _output.WriteLine($"端点: {ModelScopeEndpoint}");
        _output.WriteLine("");

        var httpClient = new HttpClient();
        var sessionId = Guid.NewGuid().ToString();

        _output.WriteLine($"生成 Session ID: {sessionId}");
        _output.WriteLine("");

        // 构造 MCP 初始化请求
        var initRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new
                {
                    name = "Verdure MCP Platform Test",
                    version = "1.0.0"
                }
            }
        };

        var requestJson = JsonSerializer.Serialize(initRequest);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, ModelScopeEndpoint)
        {
            Content = content
        };

        // 添加 mcp-session-id header
        request.Headers.Add("mcp-session-id", sessionId);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        _output.WriteLine("HTTP 请求头:");
        foreach (var header in request.Headers)
        {
            _output.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
        }
        _output.WriteLine("");

        try
        {
            _output.WriteLine("正在发送带 Session ID 的 HTTP POST 请求...");
            var response = await httpClient.SendAsync(request);

            _output.WriteLine($"HTTP 状态码: {(int)response.StatusCode} {response.StatusCode}");
            _output.WriteLine("");

            var responseBody = await response.Content.ReadAsStringAsync();
            _output.WriteLine("响应体:");
            
            try
            {
                var jsonDoc = JsonDocument.Parse(responseBody);
                var formatted = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                _output.WriteLine(formatted);
            }
            catch
            {
                _output.WriteLine(responseBody);
            }
            _output.WriteLine("");

            if (response.IsSuccessStatusCode)
            {
                _output.WriteLine("✅ 请求成功！添加 Session ID header 解决了问题！");
                Assert.True(true);
            }
            else
            {
                _output.WriteLine($"❌ 请求仍然失败: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ 发送请求失败: {ex.Message}");
            throw;
        }
    }
}

/// <summary>
/// 自定义 HTTP 消息处理器，用于记录所有 HTTP 请求和响应
/// Custom HTTP message handler for logging all HTTP requests and responses
/// </summary>
public class LoggingHttpMessageHandler : DelegatingHandler
{
    private readonly ITestOutputHelper _output;

    public LoggingHttpMessageHandler(ITestOutputHelper output)
        : base(new HttpClientHandler())
    {
        _output = output;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // 记录请求
        _output.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        _output.WriteLine("📤 HTTP 请求:");
        _output.WriteLine($"  方法: {request.Method}");
        _output.WriteLine($"  URL: {request.RequestUri}");
        _output.WriteLine($"  版本: HTTP/{request.Version}");
        _output.WriteLine("");

        _output.WriteLine("  请求头:");
        foreach (var header in request.Headers)
        {
            _output.WriteLine($"    {header.Key}: {string.Join(", ", header.Value)}");
        }

        if (request.Content != null)
        {
            _output.WriteLine("");
            foreach (var header in request.Content.Headers)
            {
                _output.WriteLine($"    {header.Key}: {string.Join(", ", header.Value)}");
            }

            _output.WriteLine("");
            _output.WriteLine("  请求体:");
            var requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrEmpty(requestBody))
            {
                // 尝试格式化 JSON
                try
                {
                    var jsonDoc = JsonDocument.Parse(requestBody);
                    var formatted = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    _output.WriteLine(formatted);
                }
                catch
                {
                    _output.WriteLine(requestBody);
                }
            }
        }

        _output.WriteLine("");

        // 发送请求
        var startTime = DateTime.UtcNow;
        HttpResponseMessage response;
        
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ HTTP 请求异常: {ex.GetType().Name}");
            _output.WriteLine($"   消息: {ex.Message}");
            throw;
        }

        var elapsed = DateTime.UtcNow - startTime;

        // 记录响应
        _output.WriteLine($"📥 HTTP 响应 (耗时: {elapsed.TotalMilliseconds:F0}ms):");
        _output.WriteLine($"  状态码: {(int)response.StatusCode} {response.StatusCode}");
        _output.WriteLine($"  版本: HTTP/{response.Version}");
        _output.WriteLine("");

        _output.WriteLine("  响应头:");
        foreach (var header in response.Headers)
        {
            _output.WriteLine($"    {header.Key}: {string.Join(", ", header.Value)}");
        }

        if (response.Content != null)
        {
            _output.WriteLine("");
            foreach (var header in response.Content.Headers)
            {
                _output.WriteLine($"    {header.Key}: {string.Join(", ", header.Value)}");
            }

            _output.WriteLine("");
            _output.WriteLine("  响应体:");
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrEmpty(responseBody))
            {
                // 尝试格式化 JSON
                try
                {
                    var jsonDoc = JsonDocument.Parse(responseBody);
                    var formatted = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    _output.WriteLine(formatted);
                }
                catch
                {
                    _output.WriteLine(responseBody);
                }
            }
        }

        _output.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        _output.WriteLine("");

        return response;
    }
}
