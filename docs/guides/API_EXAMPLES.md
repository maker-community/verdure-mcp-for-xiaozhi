# API 使用示例

本文档提供了 Verdure MCP Platform API 的详细使用示例。

## 基础信息

- **Base URL**: `https://localhost:5001` (开发环境)
- **Content-Type**: `application/json`
- **认证**: Bearer Token (可选，取决于配置)

## MCP Server 管理

### 1. 获取所有服务器

获取当前用户的所有 MCP 服务器列表。

**请求**:
```http
GET /api/mcp-servers HTTP/1.1
Host: localhost:5001
```

**响应**:
```json
[
  {
    "id": 1,
    "name": "Production Server",
    "address": "https://xiaozhi-prod.example.com",
    "description": "Main production server for XiaoZhi AI",
    "createdAt": "2025-01-15T10:30:00Z",
    "updatedAt": "2025-01-15T10:30:00Z",
    "bindings": [
      {
        "id": 1,
        "serviceName": "Calculator",
        "nodeAddress": "ws://localhost:3000/calculator",
        "mcpServerId": 1,
        "description": "Math calculation service",
        "isActive": true,
        "createdAt": "2025-01-15T10:35:00Z",
        "updatedAt": null
      }
    ]
  }
]
```

### 2. 获取特定服务器

获取单个服务器的详细信息。

**请求**:
```http
GET /api/mcp-servers/1 HTTP/1.1
Host: localhost:5001
```

**响应**:
```json
{
  "id": 1,
  "name": "Production Server",
  "address": "https://xiaozhi-prod.example.com",
  "description": "Main production server for XiaoZhi AI",
  "createdAt": "2025-01-15T10:30:00Z",
  "updatedAt": "2025-01-15T10:30:00Z",
  "bindings": []
}
```

### 3. 创建服务器

创建一个新的 MCP 服务器配置。

**请求**:
```http
POST /api/mcp-servers HTTP/1.1
Host: localhost:5001
Content-Type: application/json

{
  "name": "Development Server",
  "address": "https://xiaozhi-dev.example.com",
  "description": "Development environment for testing"
}
```

**响应**:
```http
HTTP/1.1 201 Created
Location: /api/mcp-servers/2
Content-Type: application/json

{
  "id": 2,
  "name": "Development Server",
  "address": "https://xiaozhi-dev.example.com",
  "description": "Development environment for testing",
  "createdAt": "2025-01-15T11:00:00Z",
  "updatedAt": null,
  "bindings": []
}
```

### 4. 更新服务器

更新现有服务器的信息。

**请求**:
```http
PUT /api/mcp-servers/2 HTTP/1.1
Host: localhost:5001
Content-Type: application/json

{
  "name": "Development Server - Updated",
  "address": "https://xiaozhi-dev-v2.example.com",
  "description": "Updated development environment"
}
```

**响应**:
```http
HTTP/1.1 204 No Content
```

### 5. 删除服务器

删除一个服务器及其所有绑定。

**请求**:
```http
DELETE /api/mcp-servers/2 HTTP/1.1
Host: localhost:5001
```

**响应**:
```http
HTTP/1.1 204 No Content
```

## MCP Binding 管理

### 1. 获取服务器的所有绑定

获取特定服务器的所有服务绑定。

**请求**:
```http
GET /api/mcp-bindings/server/1 HTTP/1.1
Host: localhost:5001
```

**响应**:
```json
[
  {
    "id": 1,
    "serviceName": "Calculator",
    "nodeAddress": "ws://localhost:3000/calculator",
    "mcpServerId": 1,
    "description": "Math calculation service",
    "isActive": true,
    "createdAt": "2025-01-15T10:35:00Z",
    "updatedAt": null
  },
  {
    "id": 2,
    "serviceName": "Weather",
    "nodeAddress": "ws://localhost:3000/weather",
    "mcpServerId": 1,
    "description": "Weather information service",
    "isActive": true,
    "createdAt": "2025-01-15T10:40:00Z",
    "updatedAt": null
  }
]
```

### 2. 获取所有活跃绑定

获取系统中所有活跃的服务绑定。

**请求**:
```http
GET /api/mcp-bindings/active HTTP/1.1
Host: localhost:5001
```

**响应**:
```json
[
  {
    "id": 1,
    "serviceName": "Calculator",
    "nodeAddress": "ws://localhost:3000/calculator",
    "mcpServerId": 1,
    "description": "Math calculation service",
    "isActive": true,
    "createdAt": "2025-01-15T10:35:00Z",
    "updatedAt": null
  }
]
```

### 3. 创建绑定

为服务器创建新的服务绑定。

**请求**:
```http
POST /api/mcp-bindings HTTP/1.1
Host: localhost:5001
Content-Type: application/json

{
  "serviceName": "File Manager",
  "nodeAddress": "ws://localhost:3000/filemanager",
  "serverId": 1,
  "description": "File management service"
}
```

**响应**:
```http
HTTP/1.1 201 Created
Location: /api/mcp-bindings/3
Content-Type: application/json

{
  "id": 3,
  "serviceName": "File Manager",
  "nodeAddress": "ws://localhost:3000/filemanager",
  "mcpServerId": 1,
  "description": "File management service",
  "isActive": true,
  "createdAt": "2025-01-15T12:00:00Z",
  "updatedAt": null
}
```

### 4. 更新绑定

更新现有绑定的信息。

**请求**:
```http
PUT /api/mcp-bindings/3 HTTP/1.1
Host: localhost:5001
Content-Type: application/json

{
  "serviceName": "File Manager Pro",
  "nodeAddress": "ws://localhost:3001/filemanager",
  "description": "Enhanced file management service"
}
```

**响应**:
```http
HTTP/1.1 204 No Content
```

### 5. 激活绑定

激活一个已停用的绑定。

**请求**:
```http
PUT /api/mcp-bindings/3/activate HTTP/1.1
Host: localhost:5001
```

**响应**:
```http
HTTP/1.1 204 No Content
```

### 6. 停用绑定

停用一个活跃的绑定。

**请求**:
```http
PUT /api/mcp-bindings/3/deactivate HTTP/1.1
Host: localhost:5001
```

**响应**:
```http
HTTP/1.1 204 No Content
```

### 7. 删除绑定

删除一个服务绑定。

**请求**:
```http
DELETE /api/mcp-bindings/3 HTTP/1.1
Host: localhost:5001
```

**响应**:
```http
HTTP/1.1 204 No Content
```

## 错误响应

### 404 Not Found

当请求的资源不存在时返回。

**响应**:
```http
HTTP/1.1 404 Not Found
Content-Type: application/json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "traceId": "00-..."
}
```

### 400 Bad Request

当请求数据验证失败时返回。

**响应**:
```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["Server name is required"],
    "Address": ["Invalid URL format"]
  },
  "traceId": "00-..."
}
```

### 401 Unauthorized

当用户未认证时返回。

**响应**:
```http
HTTP/1.1 401 Unauthorized
Content-Type: application/json

{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "traceId": "00-..."
}
```

## 使用 cURL 示例

### 创建服务器

```bash
curl -X POST https://localhost:5001/api/mcp-servers \
  -H "Content-Type: application/json" \
  -d '{
    "name": "My Server",
    "address": "https://xiaozhi.example.com",
    "description": "Test server"
  }'
```

### 获取所有服务器

```bash
curl -X GET https://localhost:5001/api/mcp-servers
```

### 创建绑定

```bash
curl -X POST https://localhost:5001/api/mcp-bindings \
  -H "Content-Type: application/json" \
  -d '{
    "serviceName": "Calculator",
    "nodeAddress": "ws://localhost:3000/calc",
    "serverId": 1,
    "description": "Math service"
  }'
```

### 激活/停用绑定

```bash
# 激活
curl -X PUT https://localhost:5001/api/mcp-bindings/1/activate

# 停用
curl -X PUT https://localhost:5001/api/mcp-bindings/1/deactivate
```

## 使用 JavaScript/Fetch 示例

### 创建服务器

```javascript
const createServer = async () => {
  const response = await fetch('https://localhost:5001/api/mcp-servers', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      name: 'My Server',
      address: 'https://xiaozhi.example.com',
      description: 'Test server'
    })
  });

  if (response.ok) {
    const server = await response.json();
    console.log('Server created:', server);
  } else {
    console.error('Error:', response.status);
  }
};
```

### 获取所有服务器

```javascript
const getServers = async () => {
  const response = await fetch('https://localhost:5001/api/mcp-servers');
  const servers = await response.json();
  console.log('Servers:', servers);
};
```

### 创建绑定

```javascript
const createBinding = async (serverId) => {
  const response = await fetch('https://localhost:5001/api/mcp-bindings', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      serviceName: 'Calculator',
      nodeAddress: 'ws://localhost:3000/calc',
      serverId: serverId,
      description: 'Math service'
    })
  });

  if (response.ok) {
    const binding = await response.json();
    console.log('Binding created:', binding);
  }
};
```

## 使用 C# HttpClient 示例

### 创建服务器

```csharp
using System.Net.Http.Json;

var client = new HttpClient { BaseAddress = new Uri("https://localhost:5001") };

var request = new CreateMcpServerRequest
{
    Name = "My Server",
    Address = "https://xiaozhi.example.com",
    Description = "Test server"
};

var response = await client.PostAsJsonAsync("/api/mcp-servers", request);
var server = await response.Content.ReadFromJsonAsync<McpServerDto>();
```

### 获取所有服务器

```csharp
var servers = await client.GetFromJsonAsync<List<McpServerDto>>("/api/mcp-servers");
foreach (var server in servers)
{
    Console.WriteLine($"{server.Name}: {server.Address}");
}
```

### 创建绑定

```csharp
var binding = new CreateMcpBindingRequest
{
    ServiceName = "Calculator",
    NodeAddress = "ws://localhost:3000/calc",
    ServerId = 1,
    Description = "Math service"
};

var response = await client.PostAsJsonAsync("/api/mcp-bindings", binding);
var createdBinding = await response.Content.ReadFromJsonAsync<McpBindingDto>();
```

## Postman 集合

可以导入以下 Postman 集合快速测试 API：

```json
{
  "info": {
    "name": "Verdure MCP Platform",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "MCP Servers",
      "item": [
        {
          "name": "Get All Servers",
          "request": {
            "method": "GET",
            "url": "{{baseUrl}}/api/mcp-servers"
          }
        },
        {
          "name": "Create Server",
          "request": {
            "method": "POST",
            "url": "{{baseUrl}}/api/mcp-servers",
            "body": {
              "mode": "raw",
              "raw": "{\n  \"name\": \"Test Server\",\n  \"address\": \"https://test.example.com\",\n  \"description\": \"Test\"\n}",
              "options": {
                "raw": {
                  "language": "json"
                }
              }
            }
          }
        }
      ]
    }
  ],
  "variable": [
    {
      "key": "baseUrl",
      "value": "https://localhost:5001"
    }
  ]
}
```

## Swagger/OpenAPI

在开发环境中，可以通过以下 URL 访问交互式 API 文档：

**Swagger UI**: `https://localhost:5001/swagger`

这提供了完整的 API 文档和测试界面。

## 健康检查

### 检查 API 健康状态

```bash
curl https://localhost:5001/health
```

**响应**:
```json
{
  "status": "healthy",
  "timestamp": "2025-01-15T12:00:00Z"
}
```
