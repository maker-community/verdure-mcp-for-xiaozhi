# MCP SDK 兼容性分析报告

## 目录

1. [概述](#概述)
2. [C# SDK vs TypeScript SDK 对比](#c-sdk-vs-typescript-sdk-对比)
3. [传输协议分析](#传输协议分析)
4. [兼容性问题分析](#兼容性问题分析)
5. [测试框架说明](#测试框架说明)
6. [建议与解决方案](#建议与解决方案)

---

## 概述

本文档分析了 C# MCP SDK 与 TypeScript MCP SDK 的实现差异，特别针对您遇到的问题：某些 MCP 服务在 Cherry Studio (TypeScript SDK) 中可以正常工作，但在 C# SDK 中失败。

### 问题描述

配置示例：
```json
{
  "mcpServers": {
    "pozansky-stock-server": {
      "type": "streamable_http",
      "url": "https://mcp.api-inference.modelscope.net/f39aba069a8140/mcp"
    }
  }
}
```

- ✅ **Cherry Studio**: 可以正常使用
- ❌ **C# SDK**: 无法正常连接

---

## C# SDK vs TypeScript SDK 对比

### TypeScript SDK (@modelcontextprotocol/sdk)

#### 传输类型

Cherry Studio 使用的传输类型：

```typescript
// 1. Stdio Transport
const stdioTransport = new StdioClientTransport({
  command: 'npx',
  args: ['-y', '@modelcontextprotocol/server-memory'],
  env: process.env
});

// 2. SSE (Server-Sent Events) Transport
const sseTransport = new SSEClientTransport(new URL(server.baseUrl));

// 3. Streamable HTTP Transport
const httpTransport = new StreamableHTTPClientTransport(new URL(server.baseUrl));

// 4. InMemory Transport (内置服务器)
const [clientTransport, serverTransport] = InMemoryTransport.createLinkedPair();
```

#### Client 初始化

```typescript
const client = new Client({
  name: 'test-client',
  version: '1.0.0'
}, {
  capabilities: {}
});

await client.connect(transport);
```

---

### C# SDK (modelcontextprotocol/csharp-sdk)

#### 传输类型

C# SDK 支持的传输类型：

```csharp
// 1. Stdio Transport
var stdioOptions = new StdioClientTransportOptions
{
    Command = "npx",
    Arguments = new[] { "-y", "@modelcontextprotocol/server-memory" },
    Name = "memory-server"
};
var stdioTransport = new StdioClientTransport(stdioOptions, loggerFactory);

// 2. HTTP Transport (统一处理 SSE 和 Streamable HTTP)
var httpOptions = new HttpClientTransportOptions
{
    Endpoint = new Uri("http://localhost:3001"),
    Name = "http-server",
    TransportMode = HttpTransportMode.StreamableHttp  // 或 SSE 或 AutoDetect
};
var httpTransport = new HttpClientTransport(httpOptions, loggerFactory);

// 3. Stream Transport (用于自定义 I/O 流)
var streamTransport = new StreamClientTransport(inputStream, outputStream, loggerFactory);
```

#### Client 初始化

```csharp
var clientOptions = new McpClientOptions
{
    ClientInfo = new Implementation
    {
        Name = "test-client",
        Version = "1.0.0"
    }
};

var client = await McpClient.CreateAsync(
    transport,
    clientOptions,
    loggerFactory,
    cancellationToken);
```

---

## 传输协议分析

### 1. Streamable HTTP Protocol

#### 协议规范

根据 MCP 规范，Streamable HTTP 有以下关键特性：

1. **POST 请求**：客户端发送 JSON-RPC 消息到服务器
2. **Accept 头**：必须同时接受 `application/json` 和 `text/event-stream`
3. **Content-Type**：请求体为 `application/json`
4. **响应类型**：
   - `application/json`：单个 JSON-RPC 响应
   - `text/event-stream`：SSE 流式响应（用于多消息或进度更新）

#### C# SDK 实现分析

```csharp
// StreamableHttpClientSessionTransport.cs
internal async Task<HttpResponseMessage> SendHttpRequestAsync(JsonRpcMessage message, ...)
{
    using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
    {
        Headers =
        {
            // ✅ 正确设置了两个 Accept 类型
            Accept = { s_applicationJsonMediaType, s_textEventStreamMediaType },
        },
    };

    // ✅ 复制自定义 HTTP 头（包括会话 ID 和协议版本）
    CopyAdditionalHeaders(httpRequestMessage.Headers, _options.AdditionalHeaders, 
        SessionId, _negotiatedProtocolVersion);

    // 发送请求
    var response = await _httpClient.SendAsync(httpRequestMessage, message, cancellationToken);

    // ✅ 处理不同的响应类型
    if (response.Content.Headers.ContentType?.MediaType == "application/json")
    {
        // JSON 响应
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        rpcResponseOrError = await ProcessMessageAsync(responseContent, rpcRequest, cancellationToken);
    }
    else if (response.Content.Headers.ContentType?.MediaType == "text/event-stream")
    {
        // SSE 流响应
        using var responseBodyStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        rpcResponseOrError = await ProcessSseResponseAsync(responseBodyStream, rpcRequest, cancellationToken);
    }

    return response;
}
```

#### TypeScript SDK 实现分析

```typescript
// StreamableHTTPClientTransport from Cherry Studio
async request(request: JSONRPCRequest): Promise<JSONRPCResponse> {
  const response = await fetch(this.url, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      // ✅ 设置了 Accept 头
      'Accept': 'application/json, text/event-stream',
    },
    body: JSON.stringify(request)
  });

  // 处理响应...
}
```

### 2. 关键差异点

| 特性 | C# SDK | TypeScript SDK | 影响 |
|------|--------|----------------|------|
| **HTTP 头管理** | 通过 `CopyAdditionalHeaders` 自动添加 `Mcp-Session-Id` 和 `MCP-Protocol-Version` | 可能不自动添加这些头 | ⚠️ 某些服务器可能依赖这些头 |
| **会话管理** | 自动从响应中提取 `Mcp-Session-Id` 并在后续请求中使用 | 手动管理或不使用 | ⚠️ 有状态服务器需要会话 ID |
| **GET 请求（未请求消息）** | 自动发送 GET 请求接收未请求的服务器消息 | 可能不支持 | ℹ️ 影响服务器推送功能 |
| **DELETE 请求（会话终止）** | 在 Dispose 时自动发送 DELETE 请求 | 可能不支持 | ℹ️ 影响资源清理 |

---

## 兼容性问题分析

### 问题 1: ModelScope 服务器兼容性

#### 可能的原因

1. **缺少必需的 HTTP 头**
   ```
   ModelScope 服务器可能要求：
   - User-Agent
   - 特定的认证头
   - CORS 相关头
   ```

2. **内容协商问题**
   ```
   服务器可能：
   - 只支持特定的 Content-Type
   - 要求特定的 Accept 头顺序
   - 不支持某些协议版本
   ```

3. **协议版本不匹配**
   ```csharp
   // C# SDK 默认协议版本
   public const string LatestProtocolVersion = "2024-11-05";
   
   // ModelScope 可能期望不同的版本
   ```

4. **证书或 HTTPS 问题**
   ```
   ModelScope 使用 HTTPS，可能涉及：
   - SSL/TLS 证书验证
   - HTTP/2 vs HTTP/1.1
   ```

### 问题 2: 自动检测模式的行为

```csharp
// AutoDetectingClientSessionTransport.cs
private async Task InitializeAsync(JsonRpcMessage message, ...)
{
    // 1️⃣ 先尝试 Streamable HTTP
    var streamableHttpTransport = new StreamableHttpClientSessionTransport(...);
    using var response = await streamableHttpTransport.SendHttpRequestAsync(message, ...);

    if (response.IsSuccessStatusCode)
    {
        // ✅ Streamable HTTP 成功
        ActiveTransport = streamableHttpTransport;
    }
    else
    {
        // 2️⃣ 回退到 SSE
        LogStreamableHttpFailed(_name, response.StatusCode);
        await streamableHttpTransport.DisposeAsync();
        await InitializeSseTransportAsync(message, cancellationToken);
    }
}
```

**潜在问题**：
- 如果服务器返回非 200 状态码但实际上支持 Streamable HTTP，会错误地回退到 SSE
- 某些服务器可能对初始化请求返回非标准状态码

---

## 测试框架说明

### McpServiceCompatibilityTests.cs

我创建了一个全面的兼容性测试框架：

```csharp
public class McpServiceCompatibilityTests
{
    // 1. 测试 HTTP 传输（Streamable HTTP 和 SSE）
    [Theory]
    [MemberData(nameof(GetTestConfigurations))]
    public async Task TestHttpBasedService(string serviceName, ...)
    {
        // 测试连接、工具列表、Ping 等
    }

    // 2. 测试 Stdio 传输
    [Fact]
    public async Task TestStdioService() { }

    // 3. 测试自动检测模式
    [Theory]
    public async Task TestAutoDetectMode(string endpoint) { }

    // 4. 测试错误处理
    [Fact]
    public async Task TestConnectionErrorHandling() { }

    // 5. 测试工具调用
    [Theory]
    public async Task TestToolInvocation(...) { }
}
```

### 使用方法

#### 1. 添加测试配置

在 `GetTestConfigurations()` 中添加您的服务器：

```csharp
public static IEnumerable<object[]> GetTestConfigurations()
{
    // ModelScope 服务器
    yield return new object[]
    {
        "ModelScope Pozansky Stock Server",
        "streamable_http",
        "https://mcp.api-inference.modelscope.net/f39aba069a8140/mcp",
        null,  // username
        null   // password
    };

    // 本地测试服务器
    yield return new object[]
    {
        "Local Test Server",
        "streamable_http",
        "http://localhost:3001",
        null,
        null
    };
}
```

#### 2. 运行测试

```powershell
# 运行所有兼容性测试
dotnet test --filter "FullyQualifiedName~McpServiceCompatibilityTests"

# 运行特定测试
dotnet test --filter "FullyQualifiedName~TestHttpBasedService"

# 查看详细输出
dotnet test --logger "console;verbosity=detailed"
```

#### 3. 分析结果

测试会输出详细信息：
```
测试服务: ModelScope Pozansky Stock Server
传输类型: streamable_http
端点: https://mcp.api-inference.modelscope.net/f39aba069a8140/mcp
✓ 成功连接到 ModelScope Pozansky Stock Server
服务器名称: pozansky-stock-server
服务器版本: 1.0.0
协议版本: 2024-11-05
✓ Ping 成功
✓ 发现 3 个工具:
  - get_stock_price: Get current stock price
  - get_stock_history: Get historical stock prices
  - search_stocks: Search for stocks by symbol or name
```

---

## 建议与解决方案

### 方案 1: 增强 HTTP 头配置

```csharp
var transportOptions = new HttpClientTransportOptions
{
    Endpoint = new Uri("https://mcp.api-inference.modelscope.net/f39aba069a8140/mcp"),
    Name = "ModelScope Server",
    TransportMode = HttpTransportMode.StreamableHttp,
    
    // ⭐ 添加自定义 HTTP 头
    AdditionalHeaders = new Dictionary<string, string>
    {
        ["User-Agent"] = "Verdure-MCP-Platform/1.0",
        ["X-Custom-Header"] = "value",
        // 如果需要认证
        // ["Authorization"] = "Bearer YOUR_TOKEN"
    }
};
```

### 方案 2: 使用 HttpClient 自定义配置

```csharp
var handler = new HttpClientHandler
{
    // 配置 SSL/TLS
    ServerCertificateCustomValidationCallback = 
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
    
    // 配置代理
    UseProxy = true,
    Proxy = new WebProxy("http://proxy:8080")
};

var httpClient = new HttpClient(handler)
{
    Timeout = TimeSpan.FromSeconds(30)
};

var transport = new HttpClientTransport(
    transportOptions, 
    httpClient, 
    loggerFactory,
    ownsHttpClient: true);  // 自动管理 HttpClient 生命周期
```

### 方案 3: 启用详细日志

```csharp
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Trace);  // ⭐ 启用 Trace 级别
    
    // 过滤特定类别
    builder.AddFilter("ModelContextProtocol", LogLevel.Debug);
});
```

### 方案 4: 实现重试逻辑

```csharp
public class RetryHttpClientTransport : IClientTransport
{
    private readonly HttpClientTransportOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly int _maxRetries;

    public async Task<ITransport> ConnectAsync(CancellationToken cancellationToken)
    {
        int attempt = 0;
        Exception lastException = null;

        while (attempt < _maxRetries)
        {
            try
            {
                var transport = new HttpClientTransport(_options, _loggerFactory);
                return await transport.ConnectAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempt++;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
            }
        }

        throw new Exception($"Failed after {_maxRetries} retries", lastException);
    }
}
```

### 方案 5: 调试 HTTP 通信

使用 Fiddler 或 Wireshark 捕获 HTTP 请求/响应：

```csharp
// 配置 HttpClient 使用代理（Fiddler）
var handler = new HttpClientHandler
{
    UseProxy = true,
    Proxy = new WebProxy("http://localhost:8888")  // Fiddler 默认端口
};

var httpClient = new HttpClient(handler);
var transport = new HttpClientTransport(transportOptions, httpClient, loggerFactory);
```

### 方案 6: 实现降级策略

```csharp
public async Task<McpClient> CreateResilientClientAsync(string endpoint)
{
    var strategies = new[]
    {
        HttpTransportMode.StreamableHttp,
        HttpTransportMode.Sse,
        HttpTransportMode.AutoDetect
    };

    foreach (var mode in strategies)
    {
        try
        {
            var options = new HttpClientTransportOptions
            {
                Endpoint = new Uri(endpoint),
                TransportMode = mode
            };

            var transport = new HttpClientTransport(options, _loggerFactory);
            return await McpClient.CreateAsync(transport, _clientOptions, _loggerFactory);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed with {mode}: {ex.Message}");
        }
    }

    throw new Exception("All transport modes failed");
}
```

---

## 下一步行动

### 立即行动

1. ✅ **运行兼容性测试**
   ```powershell
   dotnet test --filter "FullyQualifiedName~McpServiceCompatibilityTests" --logger "console;verbosity=detailed"
   ```

2. ✅ **收集详细日志**
   - 启用 Trace 级别日志
   - 捕获 HTTP 请求/响应详情

3. ✅ **对比实现**
   - 使用 Fiddler 对比 Cherry Studio 和 C# SDK 的 HTTP 通信
   - 查找差异点（HTTP 头、请求体、响应处理）

### 中期计划

1. **增强 SDK 兼容性**
   - 提交 Issue 到 C# SDK 仓库
   - 如果发现 Bug，提交 PR 修复

2. **创建适配层**
   - 针对特定服务器的适配器
   - 统一的错误处理和重试逻辑

3. **文档化已知问题**
   - 维护兼容性矩阵
   - 记录各服务器的特殊要求

### 长期目标

1. **贡献上游**
   - 改进 C# SDK 的服务器兼容性
   - 完善文档和示例

2. **建立测试套件**
   - 持续集成测试各种 MCP 服务器
   - 自动化兼容性报告

---

## 参考资源

### 官方文档

- [MCP Specification](https://modelcontextprotocol.io/specification/)
- [C# SDK GitHub](https://github.com/modelcontextprotocol/csharp-sdk)
- [TypeScript SDK GitHub](https://github.com/modelcontextprotocol/typescript-sdk)

### 相关项目

- [Cherry Studio](https://github.com/CherryHQ/cherry-studio) - TypeScript 实现参考
- [eShop](https://github.com/dotnet/eShop) - .NET 架构参考

### 测试工具

- [Fiddler](https://www.telerik.com/fiddler) - HTTP 调试代理
- [Postman](https://www.postman.com/) - API 测试工具
- [xUnit](https://xunit.net/) - .NET 测试框架

---

## 附录

### A. MCP 协议版本历史

| 版本 | 发布日期 | 主要变更 |
|------|---------|---------|
| 2024-11-05 | 2024-11-05 | 当前稳定版本 |
| 2025-03-26 | 未来版本 | 计划中的更新 |

### B. HTTP 状态码处理

| 状态码 | C# SDK 行为 | 建议 |
|--------|-------------|------|
| 200 | ✅ 成功 | 正常处理 |
| 202 | ⚠️ Accepted | 异步处理 |
| 400 | ❌ Bad Request | 检查请求格式 |
| 401 | ❌ Unauthorized | 添加认证 |
| 404 | ❌ Not Found | 检查端点 |
| 500 | ❌ Server Error | 服务器问题 |

### C. 常见错误代码

```csharp
// JSON-RPC 错误代码
public static class JsonRpcErrorCodes
{
    public const int ParseError = -32700;      // 解析错误
    public const int InvalidRequest = -32600;  // 无效请求
    public const int MethodNotFound = -32601;  // 方法未找到
    public const int InvalidParams = -32602;   // 无效参数
    public const int InternalError = -32603;   // 内部错误
}
```

---

**生成时间**: 2025-01-XX  
**版本**: 1.0  
**作者**: GitHub Copilot
