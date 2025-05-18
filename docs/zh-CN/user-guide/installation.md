# Modulus 安装指南

本指南提供了在各种操作系统上安装 Modulus 的详细说明。

## 系统要求

- **操作系统**：Windows 10/11、macOS 10.15+ 或 Linux（Ubuntu 20.04+、Fedora 34+ 或类似系统）
- **内存**：最低 4GB，建议 8GB
- **磁盘空间**：应用程序和基本插件至少需要 500MB
- **框架**：.NET 8.0 SDK 或更高版本
- **开发环境**（可选）：Visual Studio 2022 或带有 C# 扩展的 VS Code

## 安装方法

安装 Modulus 有几种方法：

### 方法 1：使用预构建包

1. 访问我们的 [GitHub 发布页面](https://github.com/Agibuild/modulus/releases)
2. 下载适合您操作系统的软件包
3. 安装方式因平台而异：
   - **Windows**：运行安装程序（.exe 或 .msi）并按照屏幕上的说明进行操作
   - **macOS**：打开 .dmg 文件并将应用程序拖到"应用程序"文件夹中
   - **Linux**：解压 .tar.gz 文件或使用特定发行版的软件包（.deb、.rpm）

### 方法 2：从源代码构建

#### 前提条件

- Git
- .NET 8.0 SDK 或更高版本

#### 步骤

1. 克隆仓库：
   ```
   git clone https://github.com/Agibuild/modulus.git
   ```

2. 导航到项目目录：
   ```
   cd Modulus
   ```

3. 构建应用程序：
   ```
   dotnet build
   ```

4. 运行应用程序：
   ```
   dotnet run --project src/Modulus.App.Desktop/Modulus.App.Desktop.csproj
   ```

### 方法 3：使用 Nuke 构建系统

对于想要使用我们自定义构建系统的开发者：

1. 克隆仓库：
   ```
   git clone https://github.com/Agibuild/modulus.git
   ```

2. 安装 Nuke 全局工具（如果尚未安装）：
   ```
   dotnet tool install Nuke.GlobalTool --global
   ```

3. 导航到项目目录：
   ```
   cd Modulus
   ```

4. 构建应用程序：
   ```
   nuke build
   ```

5. 运行应用程序：
   ```
   nuke run
   ```

## 插件安装

Modulus 支持插件来扩展功能。以下是安装方法：

1. 打开 Modulus
2. 转到 **设置 > 插件 > 浏览插件**
3. 选择要安装的插件并点击"安装"
4. 在提示时重启 Modulus

### 手动安装插件

您也可以手动安装插件：

1. 下载插件包（.zip 或 .mpkg）
2. 将内容解压到：
   - Windows：`%USERPROFILE%\.modulus\plugins\[插件名称]`
   - macOS/Linux：`~/.modulus/plugins/[插件名称]`
3. 重启 Modulus

## 首次设置

安装 Modulus 后，请按照以下步骤进行初始设置：

1. 启动应用程序
2. 完成欢迎向导设置您的偏好
3. 在设置菜单中配置任何必需的设置
4. 根据需要安装推荐的插件

## 安装问题疑难解答

如果在安装过程中遇到问题，请查看这些常见解决方案：

### 缺少 .NET SDK

**问题**：错误提示缺少 .NET SDK
**解决方案**：从 [Microsoft 的 .NET 下载页面](https://dotnet.microsoft.com/download) 安装 .NET 8.0 SDK

### 权限问题

**问题**：权限被拒绝错误
**解决方案**：使用管理员/sudo 权限运行安装程序

### 应用程序无法启动

**问题**：安装后应用程序无法启动
**解决方案**：检查以下位置的日志：
- Windows：`%USERPROFILE%\.modulus\logs`
- macOS/Linux：`~/.modulus/logs`

有关更多疑难解答帮助，请参阅[故障排除指南](./troubleshooting.md)或在我们的 GitHub 仓库上提出问题。

## 更新 Modulus

要将 Modulus 更新到最新版本：

1. 对于已安装的软件包，使用系统的更新机制
2. 对于源代码构建，拉取最新代码并重新构建：
   ```
   git pull
   dotnet build
   ```
3. 对于 Nuke 构建：
   ```
   git pull
   nuke build
   ```

## 卸载

要从系统中删除 Modulus：

- **Windows**：使用控制面板中的添加/删除程序
- **macOS**：将应用程序从"应用程序"拖到"废纸篓"
- **Linux**：使用您的包管理器或删除解压的目录

要完全删除所有用户数据：
- Windows：删除 `%USERPROFILE%\.modulus`
- macOS/Linux：删除 `~/.modulus`
