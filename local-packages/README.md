# 本地 NuGet 包目录使用说明

## 目录说明

`local-packages/` 目录用于存放本地编译的 NuGet 包（`.nupkg` 文件），方便在项目中引用临时编译或定制的包。

## 配置文件

项目根目录的 `nuget.config` 已配置本地包源，内容如下：

```xml
<packageSources>
  <add key="Local Packages" value="./local-packages" />
  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
</packageSources>
```

## 使用方法

### 1. 添加本地包到目录

将编译好的 `.nupkg` 文件复制到 `local-packages/` 目录：

```powershell
# 示例：复制 NuGet 包到本地目录
Copy-Item "C:\path\to\your\package\YourPackage.1.0.0.nupkg" -Destination ".\local-packages\"
```

### 2. 引用本地包

在项目文件（`.csproj`）中引用包，与引用普通 NuGet 包相同：

```xml
<ItemGroup>
  <PackageReference Include="YourPackage" Version="1.0.0" />
</ItemGroup>
```

或使用命令行：

```powershell
# 在项目目录下执行
dotnet add package YourPackage --version 1.0.0
```

### 3. 清除 NuGet 缓存（如果需要）

如果更新了本地包但项目仍使用旧版本，需要清除 NuGet 缓存：

```powershell
# 清除所有 NuGet 缓存
dotnet nuget locals all --clear

# 或只清除 http-cache
dotnet nuget locals http-cache --clear
```

### 4. 恢复包

```powershell
# 恢复所有包
dotnet restore

# 强制重新下载包
dotnet restore --force
```

## 打包本地 NuGet 包示例

如果你需要将自己的项目打包成 NuGet 包：

```powershell
# 在项目目录下执行
dotnet pack -c Release -o ..\..\local-packages

# 或指定版本号
dotnet pack -c Release -o ..\..\local-packages /p:Version=1.0.0-preview.1
```

## 包源优先级

NuGet 会按以下顺序查找包：
1. 本地包源 (`local-packages/`)
2. NuGet 官方源 (nuget.org)

如果本地包存在，将优先使用本地包。

## 注意事项

1. **版本号**：确保本地包的版本号与项目引用的版本号一致
2. **包ID**：包ID 区分大小写，需要完全匹配
3. **缓存**：更新本地包后记得清除 NuGet 缓存
4. **Git忽略**：`local-packages/` 目录下的 `.nupkg` 文件已被 Git 忽略（通过 `.gitignore`）

## 查看包源列表

```powershell
# 列出所有配置的包源
dotnet nuget list source
```

## 临时禁用某个包源

如果需要临时禁用某个包源：

```powershell
# 禁用本地包源
dotnet nuget disable source "Local Packages"

# 启用本地包源
dotnet nuget enable source "Local Packages"
```

## 故障排查

### 问题：找不到本地包

**解决方案**：
1. 确认 `.nupkg` 文件在 `local-packages/` 目录下
2. 检查包ID和版本号是否匹配
3. 清除 NuGet 缓存：`dotnet nuget locals all --clear`
4. 重新恢复包：`dotnet restore --force`

### 问题：使用了旧版本的本地包

**解决方案**：
1. 清除缓存：`dotnet nuget locals all --clear`
2. 删除项目的 `bin/` 和 `obj/` 目录
3. 重新编译：`dotnet build`

### 问题：包冲突

**解决方案**：
查看详细日志：
```powershell
dotnet restore --verbosity detailed
```

## 示例工作流程

```powershell
# 1. 编译你的自定义包
cd C:\path\to\your\custom\package
dotnet pack -c Release -o C:\github-verdure\verdure-mcp-for-xiaozhi\local-packages

# 2. 返回主项目
cd C:\github-verdure\verdure-mcp-for-xiaozhi

# 3. 清除缓存（如果是更新包）
dotnet nuget locals all --clear

# 4. 恢复包
dotnet restore

# 5. 编译项目
dotnet build
```
