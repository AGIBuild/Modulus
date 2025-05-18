# Modulus System Architecture

This document outlines the architectural design of the Modulus plugin system, explaining core components, interactions, and design decisions.

## Architecture Overview

Modulus follows a plugin-based architecture where the main application acts as a host for individual plugins. The system is designed to provide a balance between flexibility, isolation, and integration.

![System Architecture Diagram](../../../Images/layout.png)

## Core Components

### 1. Main Application (Modulus.App)

The host application provides the runtime environment, user interface framework, and core services. Its responsibilities include:

- Plugin discovery and loading
- Service registration and dependency injection
- Configuration management
- UI integration and layout management
- Cross-cutting concerns (logging, security, etc.)

### 2. Plugin Host (Modulus.PluginHost)

The plugin host manages the plugin lifecycle and provides isolation. Its responsibilities include:

- Loading plugins via AssemblyLoadContext
- Version compatibility checks
- Resource isolation
- Plugin lifecycle management (initialize, start, stop, unload)

### 3. Plugin Contract (Modulus.Plugin.Abstractions)

The contract library defines the interfaces and abstractions that plugins must implement. Key components include:

- `IPlugin`: The primary interface all plugins must implement
- `IPluginMeta`: Plugin metadata interface
- `ILocalizer`: Localization support interface
- Service abstractions and extension points

### 4. Plugins

Plugins are independently developed modules that extend the functionality of the main application. Each plugin:

- Implements the `IPlugin` interface
- Has its own directory with resources
- May provide UI components, services, or both
- Manages its own dependencies

## Design Principles

The architecture follows these key principles:

1. **Isolation**: Plugins run in isolation to prevent cascading failures and allow for independent lifecycle management.

2. **Contract-Based Integration**: All interactions between plugins and the host are through well-defined contracts.

3. **Configuration Over Convention**: System behavior is controlled through explicit configuration rather than implied conventions.

4. **Dependency Injection**: Services are registered and resolved through a central DI container.

5. **Adaptive UI Integration**: Plugins can contribute UI components that adapt to the host application's theming and layout.

## Plugin Lifecycle

![Plugin Lifecycle](../../../Images/PluginManger.png)

1. **Discovery**: The host scans plugin directories for potential plugins.
2. **Loading**: Plugin assemblies are loaded into isolated contexts.
3. **Validation**: The host verifies plugins implement the required interfaces and have compatible versions.
4. **Registration**: Plugin services are registered with the DI container.
5. **Initialization**: Plugins are initialized with required services.
6. **Execution**: Plugins run and interact with the system.
7. **Unloading**: Plugins are gracefully unloaded when no longer needed or during application shutdown.

## Project Structure

```
src/
  ├── Modulus.App/                   # Main application
  ├── Modulus.App.Desktop/           # Desktop platform integration
  ├── Modulus.Plugin.Abstractions/   # Plugin contract definitions
  ├── Modulus.PluginHost/            # Plugin loading and lifecycle management
  ├── samples/                       # Sample plugins
  │   ├── ExamplePlugin/
  │   ├── NavigationExamplePlugin/
  │   └── SimplePlugin/
  └── tools/                         # Development tools
      ├── modulus-app/               # Template for main app
      └── modulus-plugin/            # Template for plugins
```

## Service Architecture

The service architecture uses a hierarchical DI container approach:

1. **Root Container**: Contains application-wide services.
2. **Plugin Containers**: Each plugin has a child container scoped to its lifetime.

This design allows plugins to:
- Access shared services from the root container
- Register and override services within their own scope
- Remain isolated from other plugins' service registrations

## Configuration System

Configuration follows a layered approach:

1. **Application Configuration**: Base settings for the application.
2. **Plugin Configuration**: Settings specific to each plugin.
3. **User Configuration**: User-specific overrides.
4. **Runtime Configuration**: Dynamic settings applied at runtime.

Configuration sources are merged at runtime with a clear precedence order.

## Plugin Security

Security measures include:

1. **Assembly Isolation**: Plugins run in separate assembly load contexts.
2. **Resource Boundaries**: Plugins have limited access to system resources.
3. **Service Access Control**: Services can implement access control to restrict operations.
4. **Sandboxing**: Optional sandbox environments for untrusted plugins.

## Performance Considerations

The architecture includes several performance optimizations:

1. **Lazy Loading**: Plugins are loaded on-demand.
2. **Assembly Trimming**: Reduced assembly size through trimming unused code.
3. **Resource Caching**: Shared resources are cached to reduce memory usage.
4. **Unloading**: Plugins can be unloaded to free resources.

## Extension Points

The system provides these primary extension points:

1. **Services**: Plugins can register and consume services.
2. **UI Components**: Plugins can provide UI components for the main view or navigation.
3. **Commands**: Plugins can register commands that appear in menus and toolbars.
4. **Settings**: Plugins can contribute to application settings.

## Future Considerations

Planned architecture enhancements:

1. **Plugin Marketplaces**: Support for discovery and installation from remote sources.
2. **Enhanced Sandboxing**: More granular control over plugin capabilities.
3. **Hot Module Replacement**: Update plugins without restarting the application.
4. **Cross-Plugin Communication**: Standardized messaging between plugins.
