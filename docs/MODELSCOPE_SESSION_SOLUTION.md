# ModelScope MCP Session 真正的解决方案

## 🎯 问题的本质

我一直在尝试让 C# SDK "自动获取" session ID，但这是**错误的理解**！

## ✅ Cherry Studio 是如何工作的

Cherry Studio **并没有**实现任何神奇的 session 获取机制。它的工作方式非常简单：

### 正确的使用流程

1. **用户访问 ModelScope Studio 页面**
   ```
   https://www.modelscope.cn/studios/pozansky/mcp-server-stock-price/summary
   ```

2. **ModelScope 生成一个带 Session ID 的 URL**
   - 用户登录 ModelScope
   - 启动 MCP Server
   - ModelScope 返回一个完整的 URL，例如：
     ```
     https://mcp.api-inference.modelscope.net/{NEW_SESSION_ID}/mcp
     ```
   - 这个 SESSION_ID 已经预先分配好了

3. **用户复制这个 URL 到 Cherry Studio 配置**
   - Cherry Studio 只是**使用**这个 URL
   - 没有额外的 session 创建逻辑

4. **Cherry Studio 的实际行为**
   ```javascript
   // Cherry Studio 配置文件中：
   {
     "name": "ModelScope Stock Server",
     "url": "https://mcp.api-inference.modelscope.net/xxx/mcp",
     "mcp-session-id": "xxx"  // 从 URL 中提取的 ID
   }
   ```

## 🔧 C# SDK 的正确用法

我们的 SDK **完全正确**！只是使用方式不对。

### 正确的实现

```csharp
public class ModelScopeConnector
{
    /// <summary>
    /// ⚠️ 重要：这个 URL 必须从 ModelScope 网页获取，不是自动生成的
    /// </summary>
    private string GetSessionUrl()
    {
        // 选项 1: 从配置文件读取（用户手动配置）
        return Configuration.GetValue<string>("ModelScope:SessionUrl");
        
        // 选项 2: 提示用户输入
        // Console.WriteLine("请访问 https://www.modelscope.cn/studios/... 并复制 URL");
        // return Console.ReadLine();
    }
    
    public async Task ConnectAsync()
    {
        var sessionUrl = GetSessionUrl();
        // 示例: https://mcp.api-inference.modelscope.net/4fbe8c9a28e148/mcp
        
        // 从 URL 中提取 session ID
        var uri = new Uri(sessionUrl);
        var sessionId = uri.AbsolutePath.Split('/')[1];
        
        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = uri,
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["mcp-session-id"] = sessionId
            }
        };
        
        // 创建并连接
        var transport = new HttpClientTransport(transportOptions, new HttpClient());
        var client = await McpClient.CreateAsync(transport);
        
        // 成功！现在可以使用了
    }
}
```

## 📱 UI/UX 设计建议

### 在 Verdure MCP Platform 中的实现

#### 方案 1: 引导用户获取 URL

```
┌─────────────────────────────────────────────┐
│  添加 ModelScope MCP 服务器                   │
├─────────────────────────────────────────────┤
│                                             │
│  1. 访问 ModelScope Studio 页面              │
│     🔗 打开 ModelScope                       │
│                                             │
│  2. 启动 MCP 服务器并复制 URL                 │
│     ┌──────────────────────────────────┐   │
│     │ https://mcp.api-inference.       │   │
│     │ modelscope.net/xxx/mcp           │   │
│     └──────────────────────────────────┘   │
│                                             │
│  3. 粘贴到下方                               │
│     [                                  ]   │
│                                             │
│     [自动检测 Session ID]                   │
│                                             │
│                [保存]  [取消]                │
└─────────────────────────────────────────────┘
```

#### 方案 2: 集成 OAuth（高级）

```csharp
// 未来可以实现的增强版本
public class ModelScopeOAuthConnector
{
    public async Task<string> AuthorizeAndGetSessionUrlAsync()
    {
        // 1. 打开浏览器到 ModelScope 授权页面
        // 2. 用户登录并授权
        // 3. 回调获取 access token
        // 4. 使用 token 请求 MCP session URL
        // 5. 返回完整的 URL
    }
}
```

## 🎓 测试更新

### 更新测试说明

```csharp
/// <summary>
/// ModelScope MCP Server 测试
/// 
/// ⚠️ 使用前必读：
/// 1. 访问 https://www.modelscope.cn/studios/pozansky/mcp-server-stock-price/summary
/// 2. 登录并启动 MCP Server
/// 3. 复制生成的 URL（包含 session ID）
/// 4. 更新下面的 ModelScopeEndpoint 常量
/// 
/// Session 会过期，过期后需要重新获取 URL！
/// </summary>
[Fact]
public async Task Test_ModelScope_WithValidSession()
{
    // ⚠️ 这个 URL 必须是从 ModelScope 页面获取的最新有效 URL
    var sessionUrl = "https://mcp.api-inference.modelscope.net/YOUR_SESSION_HERE/mcp";
    
    // 从 URL 提取 session ID
    var uri = new Uri(sessionUrl);
    var sessionId = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries)[0];
    
    var transportOptions = new HttpClientTransportOptions
    {
        Endpoint = uri,
        TransportMode = HttpTransportMode.StreamableHttp,
        AdditionalHeaders = new Dictionary<string, string>
        {
            ["mcp-session-id"] = sessionId
        }
    };
    
    var httpClient = new HttpClient();
    var transport = new HttpClientTransport(transportOptions, httpClient, ownsHttpClient: true);
    
    var client = await McpClient.CreateAsync(transport);
    
    // 如果到这里没有异常，说明连接成功！
    var tools = await client.ListToolsAsync();
    Assert.NotEmpty(tools);
}
```

## 🆚 与其他 MCP 服务器的对比

### 标准 MCP 服务器（如 Everything Server）

```typescript
// 服务器自动分配 session
POST /mcp
→ 200 OK
   mcp-session-id: abc123

// 后续使用
POST /mcp
mcp-session-id: abc123
→ 200 OK
```

### ModelScope MCP 服务器

```typescript
// 用户先从网页获取 URL: .../xyz789/mcp
// 然后直接使用

POST /xyz789/mcp
mcp-session-id: xyz789
→ 200 OK
```

## 📝 最佳实践

### 1. 配置文件结构

```json
{
  "McpServers": [
    {
      "name": "ModelScope Stock Server",
      "type": "modelscope",
      "sessionUrl": "https://mcp.api-inference.modelscope.net/xxx/mcp",
      "lastUpdated": "2025-11-19T14:30:00Z",
      "notes": "请定期从 ModelScope 更新 URL"
    }
  ]
}
```

### 2. UI 提示

```
⚠️ ModelScope MCP Session 提示
────────────────────────────────
您的 session 已过期！

请执行以下步骤：
1. 访问 ModelScope Studio
2. 重新启动 MCP Server
3. 复制新的 URL
4. 点击下方"更新 URL"按钮

[更新 URL]  [了解更多]
```

### 3. 自动检测过期

```csharp
public class SessionMonitor
{
    public async Task<bool> IsSessionValidAsync(string sessionUrl)
    {
        try
        {
            var uri = new Uri(sessionUrl);
            var sessionId = ExtractSessionId(uri);
            
            var response = await _httpClient.PostAsync(sessionUrl, 
                new StringContent("{}"));
            
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (content.Contains("SessionExpired"))
                {
                    return false; // Session 过期
                }
            }
            
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
```

## ✅ 结论

### 真相

1. ✅ C# SDK **没有任何问题**
2. ✅ Cherry Studio **没有魔法**
3. ✅ ModelScope 需要**预先分配的 session URL**
4. ✅ 用户必须从 ModelScope 网页**手动获取** URL

### 错误的理解

- ❌ SDK 应该自动获取 session
- ❌ 服务器会自动分配 session ID
- ❌ Cherry Studio 有特殊的协议实现

### 正确的理解

- ✅ ModelScope 使用 Web 界面管理 session
- ✅ URL 本身就包含了 session 信息
- ✅ 这是一种**基于 URL 的认证**机制
- ✅ Cherry Studio 只是让用户配置这个 URL

## 🎯 下一步行动

### 立即可做的

1. **更新文档** - 说明如何获取 ModelScope URL
2. **改进 UI** - 提供清晰的指引
3. **添加验证** - 检测 URL 格式和 session 有效性

### 未来增强

1. **OAuth 集成** - 自动化 session 获取
2. **过期提醒** - 主动通知用户更新 URL
3. **一键刷新** - 跳转到 ModelScope 并引导更新

---

**总结**: 问题从来不在于 SDK 或者协议理解，而在于对 **ModelScope 的使用方式**的误解。它不是一个标准的自服务 MCP 服务器，而是一个需要通过 Web 界面预先配置的托管服务。
