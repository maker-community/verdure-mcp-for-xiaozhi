using System.Text.Json;
using Verdure.McpPlatform.Domain.SeedWork;

namespace Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiMcpEndpointAggregate;

/// <summary>
/// MCP Service Binding entity - represents an external MCP service that is bound to a Xiaozhi connection
/// Each binding references a McpServiceConfig which contains the service name and endpoint
/// Now includes specific tool selection from the MCP service
/// </summary>
public class McpServiceBinding : Entity
{
    public string XiaozhiMcpEndpointId { get; private set; }
    public string McpServiceConfigId { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<string> _selectedToolNames;
    public IReadOnlyCollection<string> SelectedToolNames => _selectedToolNames.AsReadOnly();

    // Shadow property for EF Core JSON serialization
    private string _selectedToolNamesJson
    {
        get => JsonSerializer.Serialize(_selectedToolNames);
        set => _selectedToolNames.AddRange(
            string.IsNullOrEmpty(value) 
                ? Array.Empty<string>() 
                : JsonSerializer.Deserialize<List<string>>(value) ?? new List<string>()
        );
    }

    protected McpServiceBinding()
    {
        _selectedToolNames = new List<string>();
        XiaozhiMcpEndpointId = string.Empty;
        McpServiceConfigId = string.Empty;
    }

    public McpServiceBinding(
        string XiaozhiMcpEndpointId, 
        string mcpServiceConfigId,
        string? description = null,
        IEnumerable<string>? selectedToolNames = null)
    {
        GenerateId(); // Generate Guid Version 7 ID
        XiaozhiMcpEndpointId = XiaozhiMcpEndpointId ?? throw new ArgumentNullException(nameof(XiaozhiMcpEndpointId));
        McpServiceConfigId = mcpServiceConfigId ?? throw new ArgumentNullException(nameof(mcpServiceConfigId));
        Description = description;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        _selectedToolNames = selectedToolNames?.ToList() ?? new List<string>();
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateInfo(
        string mcpServiceConfigId,
        string? description = null,
        IEnumerable<string>? selectedToolNames = null)
    {
        McpServiceConfigId = mcpServiceConfigId ?? throw new ArgumentNullException(nameof(mcpServiceConfigId));
        Description = description;
        if (selectedToolNames != null)
        {
            _selectedToolNames.Clear();
            _selectedToolNames.AddRange(selectedToolNames);
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSelectedTools(IEnumerable<string> toolNames)
    {
        _selectedToolNames.Clear();
        _selectedToolNames.AddRange(toolNames);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddTool(string toolName)
    {
        if (!_selectedToolNames.Contains(toolName))
        {
            _selectedToolNames.Add(toolName);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveTool(string toolName)
    {
        if (_selectedToolNames.Remove(toolName))
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
