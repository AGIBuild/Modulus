<!-- 优先级：P0 -->
<!-- 状态：已完成 -->
# S-0001-Project-Templates-And-Build-System

## User Story
As a developer using Modulus templates to build cross-platform plugin-based tool applications,
I want to quickly create main application or plugin template projects through simple CLI commands
(such as `dotnet new modulus-app` and `dotnet new modulus-plugin`) and use a consistent build tool
(like Nuke) to run, debug, and package the entire project, so that I can focus on business logic
development without worrying about underlying project structure or build details.

## Acceptance Criteria

| ID | Description |
|----|-------------|
| AC1 | Successfully install necessary .NET SDK (>= .NET 8) and Avalonia environment |
| AC2 | Ability to create main application project through `dotnet new modulus-app` |
| AC3 | Ability to create plugin template project through `dotnet new modulus-plugin` |
| AC4 | Clear project structure following layered architecture, supporting modular development |
| AC5 | Use Nuke to provide unified build scripts, supporting the following commands:<br>- `nuke run`: Run the main program<br>- `nuke build`: Compile the project<br>- `nuke pack`: Package the main program and plugins<br>- `nuke clean`: Clean build artifacts |
| AC6 | Project template includes basic plugin loading logic (can be empty implementation) |

## Technical Tasks

| ID | Description |
|----|-------------|
| T1 | [x] Create Git repository and solution structure, divided into src/, build/, templates/, tools/, etc. |
| T2 | [x] Install and initialize Nuke build system |
| T3 | [x] Write modulus-app project template, placed in templates/modulus-app |
| T4 | [x] Write modulus-plugin plugin template, placed in templates/modulus-plugin |
| T5 | [x] Configure dotnet new template structure and template.json metadata |
| T6 | [x] Add PluginLoader empty implementation class to the template |
| T7 | [x] Provide README example explaining usage methods and creation process |
| T8 | [x] Add basic .editorconfig and naming convention configuration |

## Initial Project Directory Structure
```
Modulus/
│
├── build/                     # Nuke build script directory
│   └── Build.cs               # Main build script
│
├── src/
│   ├── Modulus.App/           # Main program template generated directory (example)
│   └── Modulus.PluginHost/    # Plugin loading/management functionality (can be abstracted)
│
├── templates/
│   ├── modulus-app/           # dotnet new template: main program project
│   └── modulus-plugin/        # dotnet new template: plugin project
│
├── tools/
│   └── nuke/                  # nuke configuration added after generation
│
├── .config/dotnet-tools.json  # dotnet tool configuration
├── global.json                # Fixed SDK version
└── README.md                  # Project description document
```

## Dependencies and Recommended Tool Versions
- .NET SDK ≥ 8.0
- Avalonia UI ≥ 11.0
- Nuke.Build ≥ 6.2
- dotnet CLI ≥ 8.0
- OS: Windows/macOS (CLI support required)

## Additional Notes
Template design should support future evolution of plugin signing, localization, configuration, hot updates, and other features.

The modulus-plugin template can be designed for independent plugin building, facilitating third-party integration.

Nuke can serve as the core entry point for building, testing, and publishing, replacing cumbersome multi-platform scripts.
