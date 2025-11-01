using System.Text.Json;
using Verdure.McpPlatform.Domain.SeedWork;

namespace Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiConnectionAggregate;

/// <summary>
/// MCP Service Binding entity - represents an external MCP service that is bound to a Xiaozhi connection
/// Each binding configures one external MCP service node that will be injected into Xiaozhi AI
/// Now includes specific tool selection from the MCP service
/// </summary>
public class McpServiceBinding : Entity
{
    public string ServiceName { get; private set; }
    public string NodeAddress { get; private set; }
    public string XiaozhiConnectionId { get; private set; }
    public string? McpServiceConfigId { get; private set; }
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
        ServiceName = string.Empty;
        NodeAddress = string.Empty;
        XiaozhiConnectionId = string.Empty;
    }

    public McpServiceBinding(
        string serviceName, 
        string nodeAddress, 
        string xiaozhiConnectionId, 
        string? mcpServiceConfigId = null,
        string? description = null,
        IEnumerable<string>? selectedToolNames = null)
    {
        GenerateId(); // Generate Guid Version 7 ID
        ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        NodeAddress = nodeAddress ?? throw new ArgumentNullException(nameof(nodeAddress));
        XiaozhiConnectionId = xiaozhiConnectionId ?? throw new ArgumentNullException(nameof(xiaozhiConnectionId));
        McpServiceConfigId = mcpServiceConfigId;
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
        string serviceName, 
        string nodeAddress, 
        string? mcpServiceConfigId = null,
        string? description = null,
        IEnumerable<string>? selectedToolNames = null)
    {
        ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        NodeAddress = nodeAddress ?? throw new ArgumentNullException(nameof(nodeAddress));
        McpServiceConfigId = mcpServiceConfigId;
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
