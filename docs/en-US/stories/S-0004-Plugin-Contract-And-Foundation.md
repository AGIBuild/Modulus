<!-- 优先级：P0 -->
<!-- 状态：进行中 -->
# S-0004-Plugin-Contract-And-Foundation

## User Story
As a plugin system architect, I want to define a unified plugin contract and foundational capabilities (such as dependency injection, configuration, localization, logging), so that all plugins can be safely and standardly integrated into the main program and enjoy a consistent development experience.

## Acceptance Criteria
- Provide Modulus.Plugin.Abstractions (or similar) SDK, defining the following interfaces:
  - IPlugin (main plugin interface, including metadata, service registration, initialization, UI extension points, etc.)
  - IPluginMeta (plugin metadata interface, Name/Version/Description/Author/Dependencies)
  - ILocalizer (localization interface, supporting multi-language resource access and switching)
- Both plugin template and main program depend on this SDK
- Standardized plugin directory structure, each plugin in a subdirectory, containing dll, pluginsettings.json, lang.xx.json, etc.
- Only load assemblies implementing IPlugin, ignore other dlls
- Plugins support registering services through ConfigureServices(IServiceCollection), injecting IServiceProvider during initialization
- Plugins support reading pluginsettings.json configuration through IConfiguration
- Plugins support injecting ILogger<T> for logging, with namespace isolation

## Technical Tasks
- [ ] Create Modulus.Plugin.Abstractions project, defining all foundational interfaces
- [ ] Upgrade plugin template and main program to depend on this SDK
- [ ] Standardize plugin directory structure and metadata
- [ ] Upgrade PluginLoader to only load assemblies implementing IPlugin
- [ ] Support plugin dependency injection, configuration, logging and other foundational capabilities
- [ ] Provide interface documentation and development examples
