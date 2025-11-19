# ModelScope MCP 服务器 - C# SDK 使用指南

## 📋 概述

本指南说明如何正确使用 C# MCP SDK 连接 ModelScope MCP 服务器。

## 🔑 关键概念

### Session ID 管理

根据 MCP 协议标准，session ID 的工作流程：

1. **首次请求** (initialize)
   - 客户端：不发送 `mcp-session-id` header
   - 服务器：生成 session ID，在响应头 `mcp-session-id` 返回

2. **后续请求** (tools/list, tools/call 等)
   - 客户端：在请求头中携带 `mcp-session-id`
   - 服务器：验证 session ID，处理请求

### ⚠️ 常见误区

❌ **错误做法**：将 URL 中的 ID 作为 session ID
```csharp
// ❌ 错误！URL 中的 ID 不是 session ID
var url = "https://mcp.api-inference.modelscope.net/4fbe8c9a28e148/mcp";
var urlId = "4fbe8c9a28e148";  // 这是端点标识符，不是 session ID！

var transportOptions = new HttpClientTransportOptions
{
    AdditionalHeaders = new Dictionary<string, string>
    {
        ["mcp-session-id"] = urlId  // ❌ 错误！
    }
};
```

✅ **正确做法**：让 SDK 自动管理 session ID
```csharp
// ✅ 正确！SDK 自动处理 session ID
var transportOptions = new HttpClientTransportOptions
{
    Endpoint = new Uri("https://mcp.api-inference.modelscope.net/4fbe8c9a28e148/mcp"),
    Name = "ModelScope Server",
    TransportMode = HttpTransportMode.StreamableHttp
    // 不要手动设置 session ID
};
```

## 📖 正确用法示例

### 示例 1: 基本连接

```csharp
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

// 创建 HTTP 客户端
var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(30)
};

// 配置传输选项
var transportOptions = new HttpClientTransportOptions
{
    Endpoint = new Uri("https://mcp.api-inference.modelscope.net/4fbe8c9a28e148/mcp"),
    Name = "ModelScope Pozansky Stock Server",
    TransportMode = HttpTransportMode.StreamableHttp
};

// 创建传输层
var transport = new HttpClientTransport(
    transportOptions, 
    httpClient, 
    loggerFactory, 
    ownsHttpClient: true);

// 配置客户端选项
var clientOptions = new McpClientOptions
{
    ClientInfo = new Implementation
    {
        Name = "My MCP Client",
        Version = "1.0.0"
    }
};

// 连接到服务器（SDK 自动处理 session ID）
await using var client = await McpClient.CreateAsync(
    transport, 
    clientOptions, 
    loggerFactory);

// 此时 SDK 已自动：
// 1. 发送 initialize 请求
// 2. 接收服务器返回的 session ID
// 3. 在内部保存该 session ID
// 4. 后续请求将自动携带该 session ID

Console.WriteLine($"已连接到: {client.ServerInfo?.Name}");
```

### 示例 2: 完整工作流程

```csharp
// 1. 连接服务器
await using var client = await McpClient.CreateAsync(transport, clientOptions, loggerFactory);

// 2. 列出可用工具
var tools = await client.ListToolsAsync(cancellationToken: CancellationToken.None);

Console.WriteLine($"发现 {tools.Count} 个工具:");
foreach (var tool in tools)
{
    Console.WriteLine($"  - {tool.Name}: {tool.Description}");
}

// 3. 调用工具
if (tools.Count > 0)
{
    var tool = tools[0];
    var arguments = new Dictionary<string, object>
    {
        ["symbol"] = "AAPL"
    };

    var result = await client.CallToolAsync(
        tool.Name,
        arguments,
        cancellationToken: CancellationToken.None);

    Console.WriteLine($"工具调用结果: {JsonSerializer.Serialize(result)}");
}
```

### 示例 3: 捕获 Session ID（用于调试）

```csharp
public class SessionCapturingHandler : DelegatingHandler
{
    public string? SessionId { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        // 捕获服务器返回的 session ID
        if (response.Headers.TryGetValues("mcp-session-id", out var values))
        {
            SessionId = values.FirstOrDefault();
            Console.WriteLine($"服务器返回 Session ID: {SessionId}");
        }

        return response;
    }
}

// 使用自定义 handler
var handler = new SessionCapturingHandler();
var httpClient = new HttpClient(handler);

// ... 创建 client ...

// 连接后，handler.SessionId 将包含服务器返回的 session ID
Console.WriteLine($"捕获的 Session ID: {handler.SessionId}");
```

## 🔍 Session ID 生命周期

```
客户端                                服务器
   |                                    |
   | POST /mcp (initialize)             |
   |  - 无 session ID header            |
   |------------------------------------->
   |                                    |
   | 200 OK                             |
   |  - mcp-session-id: <generated>     |
   |<-------------------------------------
   |                                    |
   | POST /mcp (tools/list)             |
   |  - mcp-session-id: <same>          |
   |------------------------------------->
   |                                    |
   | 200 OK                             |
   |  - 工具列表                         |
   |<-------------------------------------
   |                                    |
```

## ⚙️ Session ID 不过期

根据 MCP SDK 的设计：

- ✅ Session ID **没有内置过期时间**
- ✅ 只要连接保持，session ID 一直有效
- ✅ 服务器重启会导致 session 失效（返回 404）
- ✅ 客户端可主动终止 session（DELETE 请求）

## 🐛 常见问题

### 问题 1: 400 Bad Request

**原因**: 服务器要求 session ID，但客户端没有提供

**解决**: 
- 检查是否在首次 initialize 请求就发送了 session ID
- 标准流程不应该在首次请求发送 session ID

### 问题 2: 404 Not Found (Session not found)

**原因**: Session ID 不存在或已失效

**解决**:
- 服务器可能重启了，需要重新 initialize
- 不要缓存 session ID，每次启动重新连接

### 问题 3: 401 Unauthorized

**原因**: Session ID 无效或认证失败

**解决**:
- 检查是否有有效的认证 token
- ModelScope 可能需要额外的认证机制

## 📚 参考资料

- [MCP 协议规范](https://spec.modelcontextprotocol.io/)
- [TypeScript SDK Session 管理](https://github.com/modelcontextprotocol/typescript-sdk/blob/main/src/client/streamableHttp.ts)
- [C# MCP SDK 文档](https://github.com/modelcontextprotocol/dotnet-sdk)
