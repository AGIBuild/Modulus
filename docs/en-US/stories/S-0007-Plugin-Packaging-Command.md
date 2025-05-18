<!-- Priority: P1 -->
<!-- Status: Completed -->
# S-0007-Plugin-Packaging-Command

**User Story**
As a plugin developer, I want to use a unified Nuke command to package the sample plugins under the samples directory, so that plugin distribution and testing are simplified. The packaging process for each plugin should be independent, and a failure in one plugin should not affect the packaging of others.

**Acceptance Criteria**
- Provide a unified plugin command that supports different behaviors via operation parameters
- Support packaging all sample plugins, e.g. `nuke plugin` or `nuke plugin --op all`
- Support packaging a single plugin, e.g. `nuke plugin --op single --name SimplePlugin`
- Each plugin is packaged into the `artifacts/plugins/{PluginName}` directory
- Each plugin also generates a zip archive for distribution
- Errors during the process do not interrupt the packaging of other plugins
- If a plugin fails to build or package, no erroneous output files are generated
- After completion, a color summary is displayed in the console, including:
  - Number and list of successfully packaged plugins (green)
  - Number and list of failed plugins (red)
  - Output file locations (yellow)

**Technical Tasks**
- [x] Add a unified Plugin target in BuildTasks.cs
- [x] Implement operation parameter (--op) to distinguish different functions
- [x] Implement sample plugin directory discovery
- [x] Implement plugin build and packaging logic
- [x] Add detailed error handling and logging
- [x] Add color summary report generation
- [x] Clean output directory when plugin generation fails
- [x] Update documentation to explain how to use the command
- [x] Test execution results in different scenarios

**Usage**
To package all sample plugins (both ways are equivalent):

    nuke plugin
    nuke plugin --op all

To package a single specified plugin:

    nuke plugin --op single --name SimplePlugin

**Report Format**
After packaging, a color summary report will be displayed, for example:

==================================================
PLUGIN PACKAGING SUMMARY
==================================================
Total plugins processed: 3
Successfully packaged:   2
Failed to package:       1

Successful plugins:
  ✓ SimplePlugin
  ✓ ExamplePlugin

Failed plugins:
  ✗ NavigationExamplePlugin

Plugins output directory:
  C:\Projects\Modulus\artifacts\plugins
==================================================

**Notes**
- The packaging process depends on the Build target and will ensure the project is built first
- When using `--op single`, the `--name` parameter must be specified
- Packaging output path: artifacts/plugins/{PluginName}
- Zip archive path: artifacts/plugins/{PluginName}.zip
- If plugin build or packaging fails, related output files will be automatically cleaned up
