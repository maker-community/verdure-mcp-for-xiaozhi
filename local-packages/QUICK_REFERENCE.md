# 本地 NuGet 包快速参考

## 当前本地包列表

以下是当前 `local-packages/` 目录中的包：

- ✅ **ModelContextProtocol.0.4.0-verdure.4.nupkg** (129 KB)
- ✅ **ModelContextProtocol.AspNetCore.0.4.0-verdure.4.nupkg** (158 KB)  
- ✅ **ModelContextProtocol.Core.0.4.0-verdure.4.nupkg** (1.5 MB)

## 快速使用

### 1. 在项目中引用本地包

编辑项目文件（如 `Verdure.McpPlatform.Api.csproj`），将版本号改为本地包版本：

```xml
<ItemGroup>
  <PackageReference Include="ModelContextProtocol" Version="0.4.0-verdure.4" />
  <PackageReference Include="ModelContextProtocol.AspNetCore" Version="0.4.0-verdure.4" />
  <PackageReference Include="ModelContextProtocol.Core" Version="0.4.0-verdure.4" />
</ItemGroup>
```

### 2. 清除缓存并恢复

```powershell
# 清除 NuGet 缓存
dotnet nuget locals all --clear

# 恢复包（将从本地目录获取）
dotnet restore

# 验证包来源
dotnet list package --include-transitive | Select-String "ModelContextProtocol"
```

### 3. 更新本地包

当你重新编译 ModelContextProtocol 包后：

```powershell
# 1. 复制新包到 local-packages 目录（覆盖旧版本）
Copy-Item "C:\path\to\ModelContextProtocol.*.nupkg" -Destination ".\local-packages\" -Force

# 2. 清除缓存
dotnet nuget locals all --clear

# 3. 强制恢复
dotnet restore --force

# 4. 重新编译项目
dotnet build
```

## 验证本地包生效

```powershell
# 查看包源
dotnet nuget list source

# 检查包还原详情（查看是否从本地源获取）
dotnet restore --verbosity detailed | Select-String "Local Packages"

# 检查项目引用的包版本
dotnet list package
```

## 切换回官方包

如果需要切换回官方 NuGet 包：

```xml
<!-- 使用官方版本 -->
<PackageReference Include="ModelContextProtocol" Version="0.3.0-preview.3" />
```

然后：
```powershell
dotnet nuget locals all --clear
dotnet restore --force
```

## 注意事项

⚠️ **版本号必须完全匹配**：`0.4.0-verdure.1` 是完整版本号
⚠️ **缓存问题**：更新本地包后务必清除缓存
⚠️ **Git 忽略**：本地 `.nupkg` 文件已被 Git 忽略，不会提交到仓库

## 当前配置状态

✅ NuGet 配置文件：`nuget.config`
✅ 本地包目录：`local-packages/`  
✅ Git 忽略：`.gitignore` 已包含 `*.nupkg`
✅ 包源已启用：通过 `dotnet nuget list source` 验证
