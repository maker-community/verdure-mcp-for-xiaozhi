param(
    [string]$BaseUrl = "https://localhost:7116/",
    [Parameter(Mandatory = $true)]
    [string]$Token,
    [int]$XiaozhiCount = 100,
    [int]$McpCount = 100,
    [int]$StartIndex = 1,
    [int]$ThrottleDelayMs = 0,
    [switch]$SkipUserSync,
    [switch]$SkipCertificateCheck
)

if ($SkipCertificateCheck) {
    # Development-only: bypass TLS validation for localhost.
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
}

$normalizedBaseUrl = $BaseUrl.TrimEnd('/')
$headers = @{ Authorization = "Bearer $Token"; Accept = "application/json" }

function Invoke-ApiRequest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Method,
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [object]$Body = $null
    )

    $uri = "$normalizedBaseUrl/$Path".Replace('//', '/').Replace('https:/', 'https://').Replace('http:/', 'http://')
    try {
        if ($null -eq $Body) {
            return Invoke-RestMethod -Method $Method -Uri $uri -Headers $headers
        }

        $json = $Body | ConvertTo-Json -Depth 6
        return Invoke-RestMethod -Method $Method -Uri $uri -Headers $headers -ContentType "application/json" -Body $json
    }
    catch {
        $message = $_.Exception.Message
        Write-Warning "Request failed: $Method $Path - $message"
        if ($_.Exception.Response) {
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $details = $reader.ReadToEnd()
                if ($details) {
                    Write-Warning "Response body: $details"
                }
            }
            catch {
                # Ignore response parsing errors.
            }
        }
        throw
    }
}

if (-not $SkipUserSync) {
    Write-Host "Syncing current user..."
    Invoke-ApiRequest -Method Post -Path "api/users/sync" -Body @{}
}

Write-Host "Seeding Xiaozhi connections ($XiaozhiCount)..."
for ($i = 0; $i -lt $XiaozhiCount; $i++) {
    $index = $StartIndex + $i
    $payload = @{
        name = ("Xiaozhi Connection {0:D3}" -f $index)
        address = ("wss://xiaozhi.example.com/mcp?token=seed-{0:D3}" -f $index)
        description = ("Seeded Xiaozhi connection {0:D3}" -f $index)
    }

    Invoke-ApiRequest -Method Post -Path "api/xiaozhi-mcp-endpoints" -Body $payload | Out-Null
    Write-Host ("Created Xiaozhi connection {0:D3}" -f $index)

    if ($ThrottleDelayMs -gt 0) {
        Start-Sleep -Milliseconds $ThrottleDelayMs
    }
}

Write-Host "Seeding MCP services ($McpCount)..."
for ($i = 0; $i -lt $McpCount; $i++) {
    $index = $StartIndex + $i
    $payload = @{
        name = ("MCP Service {0:D3}" -f $index)
        endpoint = ("https://mcp-service-{0:D3}.example.com/" -f $index)
        description = ("Seeded MCP service {0:D3}" -f $index)
        protocol = "http"
    }

    Invoke-ApiRequest -Method Post -Path "api/mcp-services" -Body $payload | Out-Null
    Write-Host ("Created MCP service {0:D3}" -f $index)

    if ($ThrottleDelayMs -gt 0) {
        Start-Sleep -Milliseconds $ThrottleDelayMs
    }
}

Write-Host "Done."