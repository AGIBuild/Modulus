using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Modulus.Build
{
    /// <summary>
    /// ManifestSync provides tools for automatically updating the AI manifest from codebase analysis
    /// </summary>
    public class ManifestSync
    {
        private readonly string _rootDirectory;
        private readonly string _manifestPath;
        private readonly bool _verbose;
        private readonly Dictionary<string, object> _manifest;

        public ManifestSync(string rootDirectory, string manifestPath, bool verbose = false)
        {
            _rootDirectory = rootDirectory;
            _manifestPath = manifestPath;
            _verbose = verbose;

            // Load existing manifest if it exists
            if (File.Exists(manifestPath))
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                var yamlContent = File.ReadAllText(manifestPath);
                _manifest = deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
            }
            else
            {
                _manifest = new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Update the directory structure section of the manifest
        /// </summary>
        public void UpdateDirectoryStructure()
        {
            Log("Updating directory structure...");

            // Create or update directoryStructure section
            if (!_manifest.ContainsKey("directoryStructure"))
            {
                _manifest["directoryStructure"] = new Dictionary<object, object>();
            }

            var directoryStructure = _manifest["directoryStructure"] as Dictionary<object, object>;
            if (directoryStructure == null)
            {
                directoryStructure = new Dictionary<object, object>();
                _manifest["directoryStructure"] = directoryStructure;
            }

            // Get root directories (only first level)
            var rootDirs = Directory.GetDirectories(_rootDirectory)
                .Select(d => new DirectoryInfo(d))
                .Where(d => !d.Name.StartsWith(".") && !d.Name.Equals("obj") && !d.Name.Equals("bin"))
                .ToList();

            // Create or update root directories
            var rootDirsDict = new Dictionary<object, object>();
            foreach (var dir in rootDirs)
            {
                // Try to determine purpose from examining files
                string purpose = InferDirectoryPurpose(dir.FullName);
                rootDirsDict[dir.Name] = purpose;
            }

            // Update directoryStructure section
            directoryStructure["root"] = rootDirsDict;

            // Update source structure if the src directory exists
            var srcDir = Path.Combine(_rootDirectory, "src");
            if (Directory.Exists(srcDir))
            {
                UpdateSourceStructure(directoryStructure, srcDir);
            }

            Log("Directory structure updated.");
        }

        /// <summary>
        /// Update the source directory structure
        /// </summary>
        private void UpdateSourceStructure(Dictionary<object, object> directoryStructure, string srcDir)
        {
            var srcDirsDict = new Dictionary<object, object>();
            var srcDirs = Directory.GetDirectories(srcDir)
                .Select(d => new DirectoryInfo(d))
                .Where(d => !d.Name.StartsWith(".") && !d.Name.Equals("obj") && !d.Name.Equals("bin"))
                .ToList();

            foreach (var dir in srcDirs)
            {
                // Try to determine purpose from examining files or project files
                string purpose = InferProjectPurpose(dir.FullName);
                srcDirsDict[dir.Name] = purpose;
            }

            directoryStructure["srcStructure"] = srcDirsDict;
        }

        /// <summary>
        /// Update naming conventions based on code analysis
        /// </summary>
        public void UpdateNamingConventions()
        {
            Log("Updating naming conventions...");

            // Create or update namingConventions section
            if (!_manifest.ContainsKey("namingConventions"))
            {
                _manifest["namingConventions"] = new Dictionary<object, object>();
            }

            var namingConventions = _manifest["namingConventions"] as Dictionary<object, object>;
            if (namingConventions == null)
            {
                namingConventions = new Dictionary<object, object>();
                _manifest["namingConventions"] = namingConventions;
            }

            // Keep existing conventions if available
            if (!namingConventions.ContainsKey("general"))
            {
                namingConventions["general"] = new List<object>
                {
                    "Use PascalCase for class names and public members",
                    "Use camelCase for local variables and parameters",
                    "Prefix private fields with _underscore",
                    "Use descriptive, full names instead of abbreviations"
                };
            }

            if (!namingConventions.ContainsKey("files"))
            {
                namingConventions["files"] = new List<object>
                {
                    "Use PascalCase for file names",
                    "Name files after the primary class they contain",
                    "Test files should be named [Class]Tests.cs"
                };
            }

            // Extract story naming conventions from docs
            var storyConventions = ExtractStoryNamingConventions();
            if (storyConventions.Count > 0)
            {
                namingConventions["stories"] = storyConventions;
            }

            // Extract plugin conventions if any plugin exists
            var pluginConventions = ExtractPluginNamingConventions();
            if (pluginConventions.Count > 0)
            {
                namingConventions["plugins"] = pluginConventions;
            }

            Log("Naming conventions updated.");
        }

        /// <summary>
        /// Update glossary based on code comments and documentation
        /// </summary>
        public void UpdateGlossary()
        {
            Log("Updating glossary...");

            // Create or update glossary section
            if (!_manifest.ContainsKey("glossary"))
            {
                _manifest["glossary"] = new Dictionary<object, object>();
            }

            var glossary = _manifest["glossary"] as Dictionary<object, object>;
            if (glossary == null)
            {
                glossary = new Dictionary<object, object>();
                _manifest["glossary"] = glossary;
            }

            // Extract terms from docs
            var extractedTerms = ExtractGlossaryTerms();
            
            // Merge with existing glossary (don't overwrite existing definitions)
            foreach (var term in extractedTerms)
            {
                if (!glossary.ContainsKey(term.Key))
                {
                    glossary[term.Key] = term.Value;
                }
            }

            Log("Glossary updated.");
        }

        /// <summary>
        /// Update project info from solution and README files
        /// </summary>
        public void UpdateProjectInfo()
        {
            Log("Updating project info...");

            // Create or update projectInfo section
            if (!_manifest.ContainsKey("projectInfo"))
            {
                _manifest["projectInfo"] = new Dictionary<object, object>();
            }

            var projectInfo = _manifest["projectInfo"] as Dictionary<object, object>;
            if (projectInfo == null)
            {
                projectInfo = new Dictionary<object, object>();
                _manifest["projectInfo"] = projectInfo;
            }

            // Extract project name from solution file or README
            var projectName = ExtractProjectName();
            if (!string.IsNullOrEmpty(projectName) && !projectInfo.ContainsKey("name"))
            {
                projectInfo["name"] = projectName;
            }

            // Extract description and vision from README
            var readmeInfo = ExtractReadmeInfo();
            if (readmeInfo.TryGetValue("description", out var description) && !projectInfo.ContainsKey("description"))
            {
                projectInfo["description"] = description;
            }

            if (readmeInfo.TryGetValue("vision", out var vision) && !projectInfo.ContainsKey("vision"))
            {
                projectInfo["vision"] = vision;
            }

            // Extract languages and frameworks if not already specified
            if (!projectInfo.ContainsKey("languages"))
            {
                var languages = DetectLanguages();
                if (languages.Count > 0)
                {
                    projectInfo["languages"] = languages;
                }
            }

            if (!projectInfo.ContainsKey("frameworks"))
            {
                var frameworks = DetectFrameworks();
                if (frameworks.Count > 0)
                {
                    projectInfo["frameworks"] = frameworks;
                }
            }

            Log("Project info updated.");
        }

        /// <summary>
        /// Save changes to the manifest file
        /// </summary>
        public void SaveManifest()
        {
            Log("Saving manifest...");

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            
            var yaml = serializer.Serialize(_manifest);
            
            // Add header comment
            var sb = new StringBuilder();
            sb.AppendLine("##################################");
            sb.AppendLine("# Modulus AI Context Manifest");
            sb.AppendLine("# This file provides context for AI tools (GitHub Copilot)");
            sb.AppendLine("# Last auto-updated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("##################################");
            sb.AppendLine();
            sb.Append(yaml);
            
            File.WriteAllText(_manifestPath, sb.ToString());
            
            Log("Manifest saved to " + _manifestPath);
        }

        /// <summary>
        /// Run the full manifest update process
        /// </summary>
        public void Run()
        {
            Log("Starting ManifestSync...");
            
            UpdateProjectInfo();
            UpdateDirectoryStructure();
            UpdateNamingConventions();
            UpdateGlossary();
            
            SaveManifest();
            
            Log("ManifestSync completed successfully.");
        }

        #region Helper Methods

        private string InferDirectoryPurpose(string dirPath)
        {
            var dirName = new DirectoryInfo(dirPath).Name.ToLower();
            
            // Common directory names and their purposes
            var commonPurposes = new Dictionary<string, string>
            {
                { "src", "Source code for application and plugins" },
                { "test", "Test projects and files" },
                { "tests", "Test projects and files" },
                { "docs", "Documentation in multiple languages" },
                { "build", "Build scripts and Nuke build tasks" },
                { "artifacts", "Build outputs and packaged plugins" },
                { "tools", "Development and utility tools" },
                { "scripts", "Automation and utility scripts" },
                { "samples", "Sample projects and examples" }
            };

            // Check if it's a common directory
            if (commonPurposes.TryGetValue(dirName, out string purpose))
            {
                return purpose;
            }

            // Try to infer purpose from content
            if (Directory.GetFiles(dirPath, "*.md").Length > 0)
            {
                return "Documentation and reference files";
            }
            
            if (Directory.GetFiles(dirPath, "*.csproj").Length > 0 ||
                Directory.GetFiles(dirPath, "*.cs").Length > 0)
            {
                return ".NET project files";
            }

            return "Project resources";
        }

        private string InferProjectPurpose(string dirPath)
        {
            var dirName = new DirectoryInfo(dirPath).Name;
            
            // Look for project files to determine purpose
            var projFiles = Directory.GetFiles(dirPath, "*.csproj");
            if (projFiles.Length > 0)
            {
                var projContent = File.ReadAllText(projFiles[0]);
                
                // Check project type
                if (projContent.Contains("<Project Sdk=\"Microsoft.NET.Sdk.Web\""))
                {
                    return "Web application project";
                }
                
                if (projContent.Contains("<OutputType>Exe</OutputType>") || 
                    projContent.Contains("<OutputType>WinExe</OutputType>"))
                {
                    return "Application entry point";
                }

                if (dirName.Contains("Test") || dirName.EndsWith("Tests"))
                {
                    return "Unit/integration test project";
                }

                if (dirName.Contains("Plugin") || dirName.Contains("Extension"))
                {
                    return "Plugin implementation";
                }

                if (dirName.Contains("Core") || dirName.Contains("Common"))
                {
                    return "Core library";
                }

                if (dirName.Contains("API") || dirName.Contains("Service"))
                {
                    return "API/service implementation";
                }
            }
            
            // Try to infer from directory name
            if (dirName.Contains("Sample") || dirName.Equals("samples"))
            {
                return "Sample code and examples";
            }

            return $"{dirName} component";
        }

        private List<object> ExtractStoryNamingConventions()
        {
            var conventions = new List<object>();
            
            // Look for story files to determine conventions
            var storyDocDir = Path.Combine(_rootDirectory, "docs", "en-US", "stories");
            if (Directory.Exists(storyDocDir))
            {
                conventions.Add("Story files are named S-XXXX-Title.md");
                
                var storyFiles = Directory.GetFiles(storyDocDir, "S-*.md");
                if (storyFiles.Length > 0)
                {
                    var storyContent = File.ReadAllText(storyFiles[0]);
                    
                    // Check for bilingual requirement
                    if (storyContent.Contains("Chinese version") || 
                        storyContent.Contains("bilingual") ||
                        Directory.Exists(Path.Combine(_rootDirectory, "docs", "zh-CN", "stories")))
                    {
                        conventions.Add("All stories must have both English and Chinese versions");
                    }
                    
                    // Check for status headers
                    if (storyContent.Contains("<!-- Status:") || storyContent.Contains("<!-- Priority:"))
                    {
                        conventions.Add("Story documents should include priority and status metadata");
                    }
                }
            }
            
            return conventions;
        }

        private List<object> ExtractPluginNamingConventions()
        {
            var conventions = new List<object>();
            
            // Look for plugin implementations
            var srcDir = Path.Combine(_rootDirectory, "src");
            if (Directory.Exists(srcDir))
            {
                bool hasPlugins = Directory.GetDirectories(srcDir)
                    .Any(d => new DirectoryInfo(d).Name.Contains("Plugin"));
                
                if (hasPlugins)
                {
                    conventions.Add("Plugin projects should be named [PluginName].Plugin");
                    
                    // Look for plugin interface definitions
                    var pluginFiles = Directory.GetFiles(srcDir, "IPlugin*.cs", SearchOption.AllDirectories);
                    if (pluginFiles.Length > 0)
                    {
                        conventions.Add("Plugin main class should implement IPlugin interface");
                    }
                    
                    // Look for config files
                    var configFiles = Directory.GetFiles(srcDir, "*Config.cs", SearchOption.AllDirectories);
                    if (configFiles.Length > 0)
                    {
                        conventions.Add("Plugin configuration classes should be named [PluginName]Config");
                    }
                }
            }
            
            return conventions;
        }

        private Dictionary<object, object> ExtractGlossaryTerms()
        {
            var terms = new Dictionary<object, object>();
            
            // Common terms in plugin/extensibility projects
            terms["plugin"] = "A dynamically loadable module that extends application functionality";
            
            // Look for AssemblyLoadContext usage
            if (Directory.GetFiles(_rootDirectory, "*.cs", SearchOption.AllDirectories)
                .Any(f => File.ReadAllText(f).Contains("AssemblyLoadContext")))
            {
                terms["AssemblyLoadContext"] = ".NET mechanism for loading assemblies in isolation";
                terms["ALC"] = "Short for AssemblyLoadContext";
            }
            
            // Check for hot reload capability
            if (Directory.GetFiles(_rootDirectory, "*.cs", SearchOption.AllDirectories)
                .Any(f => File.ReadAllText(f).Contains("reload") || 
                           File.ReadAllText(f).Contains("Reload") ||
                           File.ReadAllText(f).Contains("hot-reload")))
            {
                terms["hot-reload"] = "Ability to update plugins without restarting the application";
            }
            
            // Check for DI usage
            if (Directory.GetFiles(_rootDirectory, "*.cs", SearchOption.AllDirectories)
                .Any(f => File.ReadAllText(f).Contains("ServiceProvider") || 
                          File.ReadAllText(f).Contains("IServiceCollection")))
            {
                terms["DI"] = "Dependency Injection - design pattern for managing object dependencies";
            }
            
            // Look for SDK projects
            if (Directory.GetDirectories(_rootDirectory, "*.SDK", SearchOption.AllDirectories).Length > 0 ||
                Directory.GetDirectories(_rootDirectory, "*SDK", SearchOption.AllDirectories).Length > 0)
            {
                terms["SDK"] = "Software Development Kit - tools for plugin developers";
            }
            
            // Check for Nuke build system
            if (File.Exists(Path.Combine(_rootDirectory, "build", "_build.csproj")))
            {
                terms["Nuke"] = "Build automation system used for this project";
            }
            
            return terms;
        }

        private string ExtractProjectName()
        {
            // Try to get from solution file
            var slnFiles = Directory.GetFiles(_rootDirectory, "*.sln");
            if (slnFiles.Length > 0)
            {
                return Path.GetFileNameWithoutExtension(slnFiles[0]);
            }
            
            // Try to get from readme
            var readmePath = Path.Combine(_rootDirectory, "README.md");
            if (File.Exists(readmePath))
            {
                var content = File.ReadAllText(readmePath);
                var match = Regex.Match(content, @"# (.*?)(\r?\n|$)");
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }
            
            // Use directory name as fallback
            return new DirectoryInfo(_rootDirectory).Name;
        }

        private Dictionary<string, string> ExtractReadmeInfo()
        {
            var info = new Dictionary<string, string>();
            
            var readmePath = Path.Combine(_rootDirectory, "README.md");
            if (!File.Exists(readmePath))
            {
                return info;
            }
            
            var content = File.ReadAllText(readmePath);
            
            // Extract first paragraph as description
            var descMatch = Regex.Match(content, @"# .*?(\r?\n)(.*?)(\r?\n\r?\n|\r?\n#|$)", RegexOptions.Singleline);
            if (descMatch.Success && descMatch.Groups.Count >= 3)
            {
                info["description"] = descMatch.Groups[2].Value.Trim();
                // Use first sentence as vision if it's concise
                var firstSentence = Regex.Match(info["description"], @"^(.*?\.)\s");
                if (firstSentence.Success && firstSentence.Groups[1].Value.Length < 150)
                {
                    info["vision"] = firstSentence.Groups[1].Value.Trim();
                }
                else
                {
                    info["vision"] = info["description"].Length > 150 
                        ? info["description"].Substring(0, 147) + "..." 
                        : info["description"];
                }
            }
            
            return info;
        }

        private List<object> DetectLanguages()
        {
            var languages = new HashSet<string>();
            
            // Count file extensions to determine used languages
            var fileTypes = new Dictionary<string, string>
            {
                { ".cs", "C#" },
                { ".fs", "F#" },
                { ".vb", "Visual Basic" },
                { ".js", "JavaScript" },
                { ".ts", "TypeScript" },
                { ".tsx", "TypeScript" },
                { ".jsx", "JavaScript" },
                { ".py", "Python" },
                { ".java", "Java" },
                { ".html", "HTML" },
                { ".css", "CSS" },
                { ".scss", "SCSS" },
                { ".less", "LESS" },
                { ".md", "Markdown" },
                { ".json", "JSON" },
                { ".xml", "XML" },
                { ".yaml", "YAML" },
                { ".yml", "YAML" },
                { ".axaml", "XAML" },
                { ".xaml", "XAML" },
                { ".go", "Go" },
                { ".rs", "Rust" },
                { ".swift", "Swift" },
                { ".kt", "Kotlin" },
                { ".rb", "Ruby" },
                { ".php", "PHP" },
                { ".sh", "Shell" },
                { ".ps1", "PowerShell" }
            };
            
            var srcDir = Path.Combine(_rootDirectory, "src");
            if (Directory.Exists(srcDir))
            {
                var files = Directory.GetFiles(srcDir, "*.*", SearchOption.AllDirectories);
                var extensions = files.Select(f => Path.GetExtension(f).ToLower())
                                 .Where(e => !string.IsNullOrEmpty(e))
                                 .GroupBy(e => e)
                                 .OrderByDescending(g => g.Count())
                                 .Select(g => g.Key)
                                 .ToList();
                
                foreach (var ext in extensions)
                {
                    if (fileTypes.TryGetValue(ext, out string lang))
                    {
                        languages.Add(lang);
                    }
                }
            }
            
            return languages.Take(5).Select(l => (object)l).ToList();
        }

        private List<object> DetectFrameworks()
        {
            var frameworks = new HashSet<string>();
            
            // Check for .NET
            if (Directory.GetFiles(_rootDirectory, "*.csproj", SearchOption.AllDirectories).Length > 0)
            {
                frameworks.Add(".NET");
                
                // Try to determine .NET version
                var projFiles = Directory.GetFiles(_rootDirectory, "*.csproj", SearchOption.AllDirectories);
                if (projFiles.Length > 0)
                {
                    var content = File.ReadAllText(projFiles[0]);
                    var versionMatch = Regex.Match(content, @"<TargetFramework>net(\d+\.\d+)</TargetFramework>");
                    if (versionMatch.Success)
                    {
                        frameworks.Remove(".NET");
                        frameworks.Add($".NET {versionMatch.Groups[1].Value}");
                    }
                }
            }
            
            // Check for Avalonia
            if (Directory.GetFiles(_rootDirectory, "*.axaml", SearchOption.AllDirectories).Length > 0 ||
                Directory.GetFiles(_rootDirectory, "*.csproj", SearchOption.AllDirectories)
                         .Any(f => File.ReadAllText(f).Contains("Avalonia")))
            {
                frameworks.Add("Avalonia UI");
            }
            
            // Check for Nuke Build
            if (File.Exists(Path.Combine(_rootDirectory, "build", "_build.csproj")))
            {
                frameworks.Add("Nuke Build");
            }
            
            // Check for React
            if (Directory.GetFiles(_rootDirectory, "package.json", SearchOption.AllDirectories)
                         .Any(f => File.ReadAllText(f).Contains("\"react\"")))
            {
                frameworks.Add("React");
            }
            
            // Check for ASP.NET Core
            if (Directory.GetFiles(_rootDirectory, "*.csproj", SearchOption.AllDirectories)
                         .Any(f => File.ReadAllText(f).Contains("Microsoft.AspNetCore")))
            {
                frameworks.Add("ASP.NET Core");
            }
            
            return frameworks.Take(5).Select(f => (object)f).ToList();
        }

        private void Log(string message)
        {
            if (_verbose)
            {
                Console.WriteLine($"[ManifestSync] {message}");
            }
        }

        #endregion
    }
}
