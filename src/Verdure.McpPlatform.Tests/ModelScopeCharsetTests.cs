using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;

namespace Verdure.McpPlatform.Tests;

/// <summary>
/// 测试 ModelScope 服务器的 Content-Type charset 参数敏感性
/// Tests for ModelScope server's sensitivity to Content-Type charset parameter
/// 
/// 关键发现 (Key Findings):
/// - ModelScope 服务器拒绝 Content-Type: application/json; charset=utf-8
/// - 必须使用 Content-Type: application/json (不带 charset 参数)
/// - C# HttpClient 的 StringContent 默认会添加 charset=utf-8
/// - 需要自定义 HttpMessageHandler 来移除 charset 参数
/// 
/// ModelScope Server: https://mcp.api-inference.modelscope.net/4fbe8c9a28e148/mcp
/// </summary>
public class ModelScopeCharsetTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILoggerFactory _loggerFactory;
    private const string ModelScopeEndpoint = "https://mcp.api-inference.modelscope.net/4fbe8c9a28e148/mcp";

    public ModelScopeCharsetTests(ITestOutputHelper output)
    {
        _output = output;
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
    }

    /// <summary>
    /// 测试：移除 charset 参数后 SDK 可以成功连接
    /// Test: SDK can successfully connect after removing charset parameter
    /// </summary>
    [Fact]
    public async Task Test_SdkWithoutCharset_ShouldConnect()
    {
        _output.WriteLine("=== 测试 ModelScope 服务器 - SDK 移除 charset 参数 ===");
        _output.WriteLine($"端点: {ModelScopeEndpoint}");
        _output.WriteLine("");
        _output.WriteLine("🔍 验证假设：ModelScope 服务器拒绝 Content-Type: application/json; charset=utf-8");
        _output.WriteLine("📝 解决方案：使用自定义 HttpHandler 移除 charset 参数");
        _output.WriteLine("");

        try
        {
            // 使用移除 charset 的 HttpHandler
            var charsetRemoverHandler = new RemoveCharsetHttpHandler(_output);
            var loggingHandler = new LoggingHttpMessageHandler(_output)
            {
                InnerHandler = charsetRemoverHandler
            };
            
            var httpClient = new HttpClient(loggingHandler)
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

            _output.WriteLine("📋 正在连接...");
            await using var client = await McpClient.CreateAsync(transport, clientOptions, _loggerFactory);

            _output.WriteLine($"✅ 连接成功！");
            _output.WriteLine($"服务器名称: {client.ServerInfo?.Name}");
            _output.WriteLine($"服务器版本: {client.ServerInfo?.Version}");
            _output.WriteLine("");

            // 测试列出工具
            _output.WriteLine("📋 测试 tools/list...");
            var tools = await client.ListToolsAsync(cancellationToken: CancellationToken.None);
            _output.WriteLine($"✅ 发现 {tools.Count} 个工具:");
            foreach (var tool in tools.Take(3))
            {
                _output.WriteLine($"  - {tool.Name}: {tool.Description}");
            }

            _output.WriteLine("");
            _output.WriteLine("🎉🎉🎉 成功！移除 charset=utf-8 解决了问题！ 🎉🎉🎉");
            _output.WriteLine("");
            _output.WriteLine("💡 结论：");
            _output.WriteLine("   ModelScope 服务器不接受 Content-Type: application/json; charset=utf-8");
            _output.WriteLine("   必须使用 Content-Type: application/json（不带 charset 参数）");
            
            Assert.True(true, "成功连接到 ModelScope 服务器");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ 测试失败: {ex.Message}");
            _output.WriteLine($"异常类型: {ex.GetType().Name}");
            
            if (ex.InnerException != null)
            {
                _output.WriteLine($"内部异常: {ex.InnerException.Message}");
            }
            
            throw;
        }
    }

    /// <summary>
    /// 自定义 HttpMessageHandler：移除 Content-Type 头中的 charset 参数
    /// Custom HttpMessageHandler: Removes charset parameter from Content-Type header
    /// </summary>
    private class RemoveCharsetHttpHandler : DelegatingHandler
    {
        private readonly ITestOutputHelper _output;

        public RemoveCharsetHttpHandler(ITestOutputHelper output)
        {
            _output = output;
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // 如果有 Content 且有 Content-Type，移除 charset 参数
            if (request.Content?.Headers.ContentType != null)
            {
                var contentType = request.Content.Headers.ContentType;
                var originalContentType = contentType.ToString();
                
                // 创建新的 MediaTypeHeaderValue，不包含 charset
                var newContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType.MediaType!);
                
                // 复制除 charset 外的所有参数
                foreach (var param in contentType.Parameters)
                {
                    if (!param.Name.Equals("charset", StringComparison.OrdinalIgnoreCase))
                    {
                        newContentType.Parameters.Add(param);
                    }
                }
                
                request.Content.Headers.ContentType = newContentType;
                
                _output.WriteLine($"🔧 移除 charset 参数:");
                _output.WriteLine($"   原始: {originalContentType}");
                _output.WriteLine($"   修改: {newContentType}");
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// 自定义日志 HttpMessageHandler：记录所有 HTTP 请求和响应
    /// Custom logging HttpMessageHandler: Logs all HTTP requests and responses
    /// </summary>
    private class LoggingHttpMessageHandler : DelegatingHandler
    {
        private readonly ITestOutputHelper _output;

        public LoggingHttpMessageHandler(ITestOutputHelper output)
        {
            _output = output;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _output.WriteLine($"📤 HTTP Request: {request.Method} {request.RequestUri}");
            _output.WriteLine($"   Headers:");
            foreach (var header in request.Headers)
            {
                _output.WriteLine($"     {header.Key}: {string.Join(", ", header.Value)}");
            }

            if (request.Content != null)
            {
                _output.WriteLine($"   Content-Type: {request.Content.Headers.ContentType}");
                var content = await request.Content.ReadAsStringAsync(cancellationToken);
                if (content.Length <= 500)
                {
                    _output.WriteLine($"   Content: {content}");
                }
                else
                {
                    _output.WriteLine($"   Content: {content.Substring(0, 500)}... (truncated)");
                }
            }

            var response = await base.SendAsync(request, cancellationToken);

            _output.WriteLine($"📥 HTTP Response: {(int)response.StatusCode} {response.StatusCode}");
            _output.WriteLine($"   Headers:");
            foreach (var header in response.Headers)
            {
                _output.WriteLine($"     {header.Key}: {string.Join(", ", header.Value)}");
            }

            if (response.Content != null)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                if (responseContent.Length <= 500)
                {
                    _output.WriteLine($"   Content: {responseContent}");
                }
                else
                {
                    _output.WriteLine($"   Content: {responseContent.Substring(0, 500)}... (truncated)");
                }
            }

            return response;
        }
    }
}
