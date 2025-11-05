# WebSocket MCP Connection Features

## Overview

This document describes the WebSocket connection management features implemented for the Verdure MCP Platform, enabling real-time tool injection from MCP services to Xiaozhi AI.

## Architecture

### Backend Components

#### 1. **McpSessionService**
- Location: `src/Verdure.McpPlatform.Api/Services/WebSocket/McpSessionService.cs`
- Manages a single WebSocket connection to Xiaozhi AI
- Handles MCP protocol communication (initialize, tools/list, tools/call, ping)
- Supports automatic reconnection with exponential backoff
- Aggregates tools from multiple MCP services bound to one node

#### 2. **McpSessionManager**
- Location: `src/Verdure.McpPlatform.Api/Services/WebSocket/McpSessionManager.cs`
- Manages multiple WebSocket sessions (one per MCP server node)
- Provides lifecycle management (start, stop, restart)
- Registered as a singleton service
- Monitors connection status and statistics

#### 3. **Domain Model Updates**
- Added `IsEnabled` property to control WebSocket connection state
- Added `IsConnected` property to track real-time connection status
- Added `LastConnectedAt` and `LastDisconnectedAt` timestamps

### Frontend Components

#### 1. **Updated Theme**
- Vibrant violet color scheme (#7c3aed primary)
- No gradients - solid colors for clean look
- Light mode: White/light violet backgrounds
- Dark mode: Deep violet backgrounds

#### 2. **Connection Status Indicators**
- **Connected** (Green chip with check icon) - WebSocket is active
- **Disconnected** (Warning chip) - Enabled but not connected
- **Disabled** (Gray chip) - Connection disabled by user

#### 3. **Control Actions**
- **Enable** button - Starts WebSocket connection
- **Disable** button - Stops WebSocket connection

## Usage

### Configuring MCP Server Node

1. Navigate to **Servers** page
2. Click **Add Server** to create a new MCP node
3. Enter configuration:
   - **Name**: Descriptive name for the node
   - **Address**: Xiaozhi WebSocket endpoint (e.g., `wss://api.xiaozhi.me/mcp/?token=YOUR_TOKEN`)
   - **Description**: Optional description

### Binding MCP Services

1. Click **View Bindings** icon for a server
2. Click **Add Binding** to associate an MCP service
3. Enter:
   - **Service Name**: Name of the MCP service
   - **Node Address**: HTTP/HTTPS endpoint of the MCP service (e.g., `http://localhost:5000`)
   - **Description**: Optional description

### Enabling WebSocket Connection

1. On the **Servers** page, locate your server
2. Click the **Play** (▶) button to enable the connection
3. Status will change from "Disabled" to "Disconnected" then "Connected"
4. Once connected, tools from all bound MCP services are injected to Xiaozhi

### Disabling WebSocket Connection

1. Click the **Power** (⏻) button on an enabled server
2. Connection will be stopped immediately
3. Status changes to "Disabled"

## Technical Details

### WebSocket Protocol

The implementation follows the MCP (Model Context Protocol) specification:

1. **Initialize**: Handshake with Xiaozhi to establish protocol version
2. **Tools/List**: Returns aggregated tools from all bound MCP services
3. **Tools/Call**: Routes tool calls to appropriate MCP service
4. **Ping**: Heartbeat to maintain connection

### Automatic Reconnection

- Initial backoff: 1 second
- Maximum backoff: 30 seconds
- Max attempts: 10
- Exponential backoff with jitter

### Session Lifecycle

1. **Enable Server** → Triggers `StartSessionAsync()`
2. **WebSocket connects** → Updates `IsConnected = true`
3. **MCP clients initialized** → Connects to each bound MCP service
4. **Tools aggregated** → Combined tool list ready
5. **Xiaozhi requests tools** → Returns complete tool list
6. **Connection lost** → Auto-reconnect with backoff
7. **Disable Server** → Graceful shutdown and cleanup

### Binding Updates

When bindings are added or removed:
1. Session is stopped (`StopSessionAsync()`)
2. Brief 500ms delay
3. Session is restarted with new binding configuration (`StartSessionAsync()`)
4. New tools are loaded from updated service list

## API Endpoints

### Enable Server
```
POST /api/mcp-servers/{id}/enable
```
Starts WebSocket connection for the specified server.

### Disable Server
```
POST /api/mcp-servers/{id}/disable
```
Stops WebSocket connection for the specified server.

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "mcpdb": "Host=localhost;Database=verdure_mcp;Username=postgres;Password=postgres"
  }
}
```

### Xiaozhi Token
The WebSocket address should include your Xiaozhi token:
```
wss://api.xiaozhi.me/mcp/?token=YOUR_XIAOZHI_TOKEN_HERE
```

## Monitoring

### Logs
WebSocket connections log important events:
- Connection established/lost
- Tool list requests
- Tool calls
- Reconnection attempts
- Errors

### Status Indicators
- UI shows real-time connection status
- Chips use color coding for quick status identification
- Timestamps show last connection/disconnection times

## Troubleshooting

### Server Not Connecting
1. Check WebSocket address format
2. Verify Xiaozhi token is valid
3. Check network connectivity
4. Review logs for error messages

### Tools Not Appearing
1. Ensure MCP service endpoints are accessible
2. Verify bindings are active
3. Check MCP service is responding to tools/list requests
4. Review logs for connection errors to MCP services

### Connection Keeps Dropping
1. Check network stability
2. Review reconnection logs
3. Verify MCP service endpoints are stable
4. Consider increasing reconnection max attempts

## Limitations

- Simple URL-based authentication only
- No advanced MCP features (prompts, resources) yet
- All tools from all services are injected (no selective injection)
- Connection status updates may have slight delay

## Future Enhancements

- [ ] Selective tool injection (choose which tools to expose)
- [ ] Support for MCP prompts and resources
- [ ] Advanced authentication mechanisms
- [ ] Connection health metrics and monitoring dashboard
- [ ] Bulk enable/disable operations
- [ ] WebSocket connection pooling
