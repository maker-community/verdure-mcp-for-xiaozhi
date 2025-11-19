# ModelScope MCP 使用指南

## 📋 快速开始

### 第一步：获取 Session URL

1. **访问 ModelScope Studio**
   ```
   https://www.modelscope.cn/studios/pozansky/mcp-server-stock-price/summary
   ```

2. **登录 ModelScope 账号**（如果还没有账号，需要先注册）

3. **启动 MCP Server**
   - 在页面上找到"启动服务"或类似按钮
   - ModelScope 会生成一个带 session ID 的唯一 URL
   - 格式类似：`https://mcp.api-inference.modelscope.net/{SESSION_ID}/mcp`

4. **复制完整 URL**
   - 这个 URL 包含了你的专属 session ID
   - **不要分享这个 URL**，它包含你的访问凭证

### 第二步：在代码中使用

```csharp
// 1. 设置 session URL（从 ModelScope 网页复制的）
var sessionUrl = "https://mcp.api-inference.modelscope.net/YOUR_SESSION_HERE/mcp";

// 2. 提取 session ID
var uri = new Uri(sessionUrl);
var pathParts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
var sessionId = pathParts[0]; // 第一个路径部分就是 session ID

// 3. 配置 transport
var transportOptions = new HttpClientTransportOptions
{
    Endpoint = uri,
    TransportMode = HttpTransportMode.StreamableHttp,
    AdditionalHeaders = new Dictionary<string, string>
    {
        ["mcp-session-id"] = sessionId  // ✅ 必须添加这个 header
    }
};

// 4. 创建连接
var httpClient = new HttpClient();
var transport = new HttpClientTransport(transportOptions, httpClient, ownsHttpClient: true);
var client = await McpClient.CreateAsync(transport);

// 5. 使用 MCP client
var tools = await client.ListToolsAsync();
Console.WriteLine($"Available tools: {string.Join(", ", tools.Select(t => t.Name))}");
```

## 🎯 在 Verdure MCP Platform 中使用

### UI 配置流程

```
1. 添加 MCP Service Config
   ↓
2. 选择 "ModelScope" 类型
   ↓
3. 输入从网页获取的 Session URL
   ↓
4. 系统自动提取 session ID 并配置 headers
   ↓
5. 保存并测试连接
```

### 实现示例

```csharp
public class ModelScopeMcpServiceConfig
{
    /// <summary>
    /// 从 ModelScope 网页获取的完整 URL
    /// </summary>
    public string SessionUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// 自动提取的 session ID
    /// </summary>
    public string SessionId => ExtractSessionId(SessionUrl);
    
    /// <summary>
    /// Session 获取时间（用于检测过期）
    /// </summary>
    public DateTime ObtainedAt { get; set; }
    
    private static string ExtractSessionId(string url)
    {
        if (string.IsNullOrEmpty(url))
            return string.Empty;
            
        try
        {
            var uri = new Uri(url);
            var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
```

## ⚠️ 常见问题

### Q1: 为什么会返回 401 SessionExpired？

**A**: Session 已过期。每个 session 都有时效性，过期后需要：
1. 重新访问 ModelScope Studio
2. 重新启动 MCP Server
3. 获取新的 URL
4. 更新配置

### Q2: 为什么会返回 404 Not Found？

**A**: Session 记录已被完全删除。这通常发生在 session 过期很久之后。解决方法同 Q1。

### Q3: 为什么会返回 400 InvalidArgument？

**A**: 缺少 `mcp-session-id` header。确保在创建 transport 时添加了：
```csharp
AdditionalHeaders = new Dictionary<string, string>
{
    ["mcp-session-id"] = sessionId
}
```

### Q4: Session 有效期多久？

**A**: ModelScope 没有公开文档说明，根据测试：
- 大约几小时到一天
- 建议定期检查和更新
- 添加过期检测机制

### Q5: 能否自动刷新 session？

**A**: 目前 ModelScope 没有提供 API。未来可能的方案：
- OAuth 2.0 自动授权
- API Key 换取 session
- 监听 Web 事件自动更新

## 🔒 安全建议

### ❌ 不要这样做

```csharp
// ❌ 不要硬编码 session URL
private const string SessionUrl = "https://mcp.api-inference.modelscope.net/abc123/mcp";

// ❌ 不要提交包含 session 的配置文件到 git
{
  "modelscope_session": "abc123"  // 这是敏感信息！
}

// ❌ 不要分享 session URL
// 它等同于 API key，可以用来访问你的服务
```

### ✅ 应该这样做

```csharp
// ✅ 从环境变量或用户配置读取
var sessionUrl = Environment.GetEnvironmentVariable("MODELSCOPE_SESSION_URL");

// ✅ 使用 User Secrets（开发环境）
dotnet user-secrets set "ModelScope:SessionUrl" "https://..."

// ✅ 生产环境使用 Key Vault 或 Secrets Manager
var sessionUrl = await _secretsManager.GetSecretAsync("ModelScope/SessionUrl");
```

## 📊 Session 状态监控

### 实现健康检查

```csharp
public class ModelScopeHealthCheck : IHealthCheck
{
    private readonly string _sessionUrl;
    private readonly IHttpClientFactory _httpClientFactory;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = new Uri(_sessionUrl);
            var sessionId = ExtractSessionId(uri);
            
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("mcp-session-id", sessionId);
            
            var response = await client.PostAsync(_sessionUrl,
                new StringContent("{\"jsonrpc\":\"2.0\",\"method\":\"ping\"}"),
                cancellationToken);
            
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return HealthCheckResult.Unhealthy(
                    "ModelScope session expired. Please update session URL.");
            }
            
            if (!response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Degraded(
                    $"ModelScope returned {response.StatusCode}");
            }
            
            return HealthCheckResult.Healthy("ModelScope session is valid");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Failed to check ModelScope session", ex);
        }
    }
}
```

### 在 Startup 中注册

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<ModelScopeHealthCheck>(
        "modelscope",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "mcp", "external" });
```

## 🎨 UI 示例

### 配置页面

```razor
<MudCard>
    <MudCardHeader>
        <CardHeaderContent>
            <MudText Typo="Typo.h6">ModelScope MCP Server</MudText>
        </CardHeaderContent>
    </MudCardHeader>
    <MudCardContent>
        <MudAlert Severity="Severity.Info" Class="mb-4">
            <MudText Typo="Typo.body2">
                请先访问 
                <MudLink Href="https://www.modelscope.cn/studios/pozansky/mcp-server-stock-price/summary" 
                         Target="_blank">
                    ModelScope Studio
                </MudLink>
                启动服务并获取 URL
            </MudText>
        </MudAlert>
        
        <MudTextField 
            @bind-Value="_sessionUrl"
            Label="Session URL"
            Variant="Variant.Outlined"
            HelperText="从 ModelScope 复制的完整 URL"
            Adornment="Adornment.End"
            AdornmentIcon="@Icons.Material.Filled.ContentPaste"
            OnAdornmentClick="PasteFromClipboard" />
        
        @if (!string.IsNullOrEmpty(_detectedSessionId))
        {
            <MudAlert Severity="Severity.Success" Class="mt-2">
                检测到 Session ID: @_detectedSessionId
            </MudAlert>
        }
        
        <MudAlert Severity="Severity.Warning" Class="mt-4" Variant="Variant.Filled">
            ⚠️ Session 会过期！过期后请重新获取 URL
        </MudAlert>
    </MudCardContent>
    <MudCardActions>
        <MudButton Variant="Variant.Filled" 
                   Color="Color.Primary" 
                   OnClick="TestConnection"
                   Disabled="_isLoading">
            @if (_isLoading)
            {
                <MudProgressCircular Size="Size.Small" Indeterminate="true" />
                <MudText Class="ml-2">测试连接中...</MudText>
            }
            else
            {
                <MudText>测试连接</MudText>
            }
        </MudButton>
        <MudButton Variant="Variant.Text" 
                   OnClick="SaveConfiguration">
            保存
        </MudButton>
    </MudCardActions>
</MudCard>

@code {
    private string _sessionUrl = string.Empty;
    private string _detectedSessionId = string.Empty;
    private bool _isLoading = false;
    
    private void OnSessionUrlChanged(string value)
    {
        _sessionUrl = value;
        _detectedSessionId = ExtractSessionId(value);
    }
    
    private async Task TestConnection()
    {
        _isLoading = true;
        try
        {
            // 实现连接测试逻辑
        }
        finally
        {
            _isLoading = false;
        }
    }
}
```

## 📈 最佳实践总结

### ✅ 做这些

1. **定期检查** session 状态
2. **提供清晰的指引**帮助用户获取 URL
3. **自动检测**URL 格式和 session ID
4. **实现健康检查**监控 session 有效性
5. **安全存储** session 信息

### ❌ 避免这些

1. **硬编码** session URL
2. **假设** session 永不过期
3. **忽略错误**消息（401/404）
4. **共享** session URL
5. **跳过验证**直接使用用户输入

---

**记住**: ModelScope MCP 不是传统的自服务 API，它需要通过 Web 界面管理 session。这是设计选择，不是缺陷。正确理解这一点，才能正确使用。
