using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Instemic.PackageCreator.Editor
{
    public enum PackageTemplate
    {
        Universal,
        XRPluginProvider,
        EditorOnly,
        PlatformSpecific
    }
    
    public class PackageCreatorWindow : EditorWindow
    {
        private PackageTemplate selectedTemplate = PackageTemplate.Universal;
        private string companyName = "";
        private string packageName = "my-package";
        private string displayName = "My Package";
        private string description = "Description of my package";
        private string unityVersion = "";
        private string author = "";
        private string outputPath = "";
        
        private Vector2 scrollPosition;
        
        private readonly string[] templateDescriptions = new string[]
        {
            "Standard package with Runtime, Editor, and Tests folders",
            "XR Plugin Provider with XRLoader, subsystems, and native plugin support",
            "Editor-only tools and extensions (no Runtime code)",
            "Platform-specific package with Android/iOS support"
        };

        [MenuItem("Tools/Create UPM Package")]
        public static void ShowWindow()
        {
            var window = GetWindow<PackageCreatorWindow>("Package Creator");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        private void OnEnable()
        {
            // Set default output path to Packages folder (normalized)
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            outputPath = Path.Combine(projectPath, "Packages");

            // Set default company name from Unity project settings
            if (string.IsNullOrEmpty(companyName))
            {
                string sanitized = SanitizeForPackageId(PlayerSettings.companyName);
                companyName = !string.IsNullOrEmpty(sanitized) ? sanitized : "mycompany";
            }

            // Set default Unity version from current editor version
            if (string.IsNullOrEmpty(unityVersion))
            {
                unityVersion = GetDefaultUnityVersion();
            }

            // Set default author from company name if not set
            if (string.IsNullOrEmpty(author))
            {
                author = PlayerSettings.companyName;
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Unity Package Creator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Create a new UPM package with proper structure and configuration.", MessageType.Info);
            
            GUILayout.Space(15);
            EditorGUILayout.LabelField("Package Template", EditorStyles.boldLabel);
            
            selectedTemplate = (PackageTemplate)EditorGUILayout.EnumPopup(new GUIContent("Template Type", "Select the type of package to create"), selectedTemplate);
            EditorGUILayout.HelpBox(templateDescriptions[(int)selectedTemplate], MessageType.Info);
            
            GUILayout.Space(15);
            EditorGUILayout.LabelField("Package Information", EditorStyles.boldLabel);
            
            companyName = EditorGUILayout.TextField(new GUIContent("Company Name", "Your company or username (lowercase, no spaces)"), companyName);
            packageName = EditorGUILayout.TextField(new GUIContent("Package Name", "Package identifier (lowercase, use hyphens)"), packageName);
            displayName = EditorGUILayout.TextField(new GUIContent("Display Name", "Human-readable name shown in Package Manager"), displayName);
            description = EditorGUILayout.TextField(new GUIContent("Description", "Brief description of your package"), description, GUILayout.Height(60));
            unityVersion = EditorGUILayout.TextField(new GUIContent("Unity Version", "Minimum Unity version (e.g., 6000.2)"), unityVersion);
            author = EditorGUILayout.TextField(new GUIContent("Author (Optional)", "Author name or organization"), author);
            
            GUILayout.Space(15);
            EditorGUILayout.LabelField("Output Location", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("Output Path", outputPath);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Select Output Location", outputPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    outputPath = selectedPath;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            EditorGUILayout.HelpBox($"Package will be created at:\n{GetFullPackagePath()}", MessageType.None);
            
            GUILayout.Space(20);
            
            // Preview of package name
            EditorGUILayout.LabelField("Package ID", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel($"com.{companyName}.{packageName}", EditorStyles.textField, GUILayout.Height(20));
            
            GUILayout.Space(20);
            
            if (GUILayout.Button("Create Package", GUILayout.Height(40)))
            {
                CreatePackage();
            }
            
            GUILayout.Space(10);
            EditorGUILayout.EndScrollView();
        }

        private string GetFullPackagePath()
        {
            return Path.Combine(outputPath, $"com.{companyName}.{packageName}");
        }

        private void CreatePackage()
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(companyName) || string.IsNullOrWhiteSpace(packageName))
            {
                EditorUtility.DisplayDialog("Error", "Company name and package name are required.", "OK");
                return;
            }

            string packagePath = GetFullPackagePath();
            
            // Check if directory already exists
            if (Directory.Exists(packagePath))
            {
                if (!EditorUtility.DisplayDialog("Directory Exists", 
                    $"The directory already exists:\n{packagePath}\n\nDo you want to overwrite it?", 
                    "Yes", "No"))
                {
                    return;
                }
                Directory.Delete(packagePath, true);
            }

            try
            {
                CreatePackageStructure(packagePath);
                
                // Check if package was created in Packages folder
                bool isInPackagesFolder = outputPath.EndsWith("Packages") || outputPath.Contains(Path.DirectorySeparatorChar + "Packages" + Path.DirectorySeparatorChar);
                
                string message = isInPackagesFolder 
                    ? $"Package created successfully at:\n{packagePath}\n\nThe package is now available in Package Manager and ready to use!"
                    : $"Package created successfully at:\n{packagePath}\n\nYou can now move this to your Unity project's Packages folder or publish it to GitHub.";
                
                EditorUtility.DisplayDialog("Success", message, "OK");
                
                // Open the folder
                EditorUtility.RevealInFinder(packagePath);
                
                // Refresh the Asset Database if package is in Packages folder
                if (isInPackagesFolder)
                {
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to create package:\n{e.Message}", "OK");
                Debug.LogError($"Package creation failed: {e}");
            }
        }

        private void CreatePackageStructure(string packagePath)
        {
            string packageId = $"com.{companyName}.{packageName}";
            
            // Create base directory
            Directory.CreateDirectory(packagePath);
            
            // Create structure based on template
            switch (selectedTemplate)
            {
                case PackageTemplate.Universal:
                    CreateUniversalPackage(packagePath, packageId);
                    break;
                case PackageTemplate.XRPluginProvider:
                    CreateXRPluginProviderPackage(packagePath, packageId);
                    break;
                case PackageTemplate.EditorOnly:
                    CreateEditorOnlyPackage(packagePath, packageId);
                    break;
                case PackageTemplate.PlatformSpecific:
                    CreatePlatformSpecificPackage(packagePath, packageId);
                    break;
            }
            
            // Create common documentation files
            CreateReadme(packagePath);
            CreateChangelog(packagePath);
            CreateLicense(packagePath);
            CreateDocumentation(packagePath);
            
            Debug.Log($"Package created successfully: {packageId} (Template: {selectedTemplate})");
        }

        private void CreateUniversalPackage(string packagePath, string packageId)
        {
            // Create directories
            Directory.CreateDirectory(Path.Combine(packagePath, "Editor", "Scripts"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Runtime", "Scripts"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Tests", "Editor"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Tests", "Runtime"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Samples~"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Documentation~"));
            
            // Create package.json
            CreatePackageJson(packagePath, packageId, null);
            
            // Create assembly definition files
            CreateAsmdef(Path.Combine(packagePath, "Runtime"), $"{packageId}", packageId, false, null);
            CreateAsmdef(Path.Combine(packagePath, "Editor"), $"{packageId}.Editor", $"{packageId}.Editor", true, new[] { packageId });
            CreateTestAsmdef(Path.Combine(packagePath, "Tests", "Runtime"), $"{packageId}.Tests", packageId, false);
            CreateTestAsmdef(Path.Combine(packagePath, "Tests", "Editor"), $"{packageId}.Editor.Tests", $"{packageId}.Editor", true);
        }

        private void CreateXRPluginProviderPackage(string packagePath, string packageId)
        {
            // Create directories
            Directory.CreateDirectory(Path.Combine(packagePath, "Runtime", "Scripts"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Runtime", "Subsystems"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Runtime", "Plugins", "Android"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Runtime", "Plugins", "iOS"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Editor", "Scripts"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Tests", "Editor"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Tests", "Runtime"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Samples~"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Documentation~"));
            
            // Create package.json with XR dependencies
            var dependencies = new Dictionary<string, string>
            {
                { "com.unity.xr.management", "4.0.0" }
            };
            CreatePackageJson(packagePath, packageId, dependencies);
            
            // Create assembly definition files with XR references
            CreateAsmdef(Path.Combine(packagePath, "Runtime"), $"{packageId}", packageId, false, new[] { "Unity.XR.Management" });
            CreateAsmdef(Path.Combine(packagePath, "Editor"), $"{packageId}.Editor", $"{packageId}.Editor", true, new[] { packageId, "Unity.XR.Management.Editor" });
            CreateTestAsmdef(Path.Combine(packagePath, "Tests", "Runtime"), $"{packageId}.Tests", packageId, false);
            CreateTestAsmdef(Path.Combine(packagePath, "Tests", "Editor"), $"{packageId}.Editor.Tests", $"{packageId}.Editor", true);
            
            // Create XR-specific template files
            CreateXRLoaderTemplate(Path.Combine(packagePath, "Runtime", "Scripts"), packageId);
            CreateXRSettingsTemplate(Path.Combine(packagePath, "Runtime", "Scripts"), packageId);
            CreateXRPackageMetadataTemplate(Path.Combine(packagePath, "Editor", "Scripts"), packageId);
            CreateXRBuildProcessorTemplate(Path.Combine(packagePath, "Editor", "Scripts"), packageId);

            // Create example subsystem implementations
            CreateDisplaySubsystemExample(Path.Combine(packagePath, "Runtime", "Subsystems"), packageId);
            CreateInputSubsystemStub(Path.Combine(packagePath, "Runtime", "Subsystems"), packageId);

            // Create helper README files
            CreateSubsystemsReadme(Path.Combine(packagePath, "Runtime", "Subsystems"));
            CreatePluginsReadme(Path.Combine(packagePath, "Runtime", "Plugins"));

            // Create XR-specific documentation
            CreateGettingStarted(packagePath, packageId);
            CreateArchitectureDoc(packagePath, packageId);
        }

        private void CreateEditorOnlyPackage(string packagePath, string packageId)
        {
            // Create directories - Editor only!
            Directory.CreateDirectory(Path.Combine(packagePath, "Editor", "Scripts"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Editor", "Resources"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Documentation~"));
            
            // Create package.json
            CreatePackageJson(packagePath, packageId, null);
            
            // Create assembly definition files - Editor only
            CreateAsmdef(Path.Combine(packagePath, "Editor"), $"{packageId}.Editor", $"{packageId}.Editor", true, null);
            
            // Create editor window template
            CreateEditorWindowTemplate(Path.Combine(packagePath, "Editor", "Scripts"), packageId);
        }

        private void CreatePlatformSpecificPackage(string packagePath, string packageId)
        {
            // Create directories
            Directory.CreateDirectory(Path.Combine(packagePath, "Runtime", "Scripts"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Runtime", "Plugins", "Android"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Runtime", "Plugins", "iOS"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Editor", "Scripts"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Tests", "Editor"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Tests", "Runtime"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Samples~"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Documentation~"));
            
            // Create package.json
            CreatePackageJson(packagePath, packageId, null);
            
            // Create platform-specific assembly definitions
            CreatePlatformAsmdef(Path.Combine(packagePath, "Runtime"), $"{packageId}", packageId, "Android");
            CreateAsmdef(Path.Combine(packagePath, "Editor"), $"{packageId}.Editor", $"{packageId}.Editor", true, new[] { packageId });
            CreateTestAsmdef(Path.Combine(packagePath, "Tests", "Runtime"), $"{packageId}.Tests", packageId, false);
            CreateTestAsmdef(Path.Combine(packagePath, "Tests", "Editor"), $"{packageId}.Editor.Tests", $"{packageId}.Editor", true);
            
            // Create platform build processor
            CreatePlatformBuildProcessorTemplate(Path.Combine(packagePath, "Editor", "Scripts"), packageId);
            
            // Create AndroidManifest template
            CreateAndroidManifestTemplate(Path.Combine(packagePath, "Runtime", "Plugins", "Android"));
        }

        private void CreatePackageJson(string packagePath, string packageId, Dictionary<string, string> dependencies)
        {
            var json = new System.Text.StringBuilder();
            json.AppendLine("{");
            json.AppendLine($"  \"name\": \"{packageId}\",");
            json.AppendLine($"  \"version\": \"0.1.0\",");
            json.AppendLine($"  \"displayName\": \"{displayName}\",");
            json.AppendLine($"  \"description\": \"{description}\",");
            json.AppendLine($"  \"unity\": \"{unityVersion}\",");
            
            if (dependencies != null && dependencies.Count > 0)
            {
                json.AppendLine($"  \"dependencies\": {{");
                int count = 0;
                foreach (var dep in dependencies)
                {
                    count++;
                    string comma = count < dependencies.Count ? "," : "";
                    json.AppendLine($"    \"{dep.Key}\": \"{dep.Value}\"{comma}");
                }
                json.AppendLine($"  }},");
            }
            
            json.AppendLine($"  \"keywords\": [],");
            
            if (!string.IsNullOrWhiteSpace(author))
            {
                json.AppendLine($"  \"author\": {{");
                json.AppendLine($"    \"name\": \"{author}\"");
                json.AppendLine($"  }},");
            }
            
            json.AppendLine($"  \"license\": \"MIT\"");
            json.AppendLine("}");
            
            File.WriteAllText(Path.Combine(packagePath, "package.json"), json.ToString());
        }

        private void CreateAsmdef(string folder, string asmdefName, string rootNamespace, bool editorOnly, string[] references)
        {
            var asmdef = new System.Text.StringBuilder();
            asmdef.AppendLine("{");
            asmdef.AppendLine($"    \"name\": \"{asmdefName}\",");
            asmdef.AppendLine($"    \"rootNamespace\": \"{rootNamespace}\",");
            
            if (references != null && references.Length > 0)
            {
                asmdef.AppendLine($"    \"references\": [");
                for (int i = 0; i < references.Length; i++)
                {
                    string comma = i < references.Length - 1 ? "," : "";
                    asmdef.AppendLine($"        \"{references[i]}\"{comma}");
                }
                asmdef.AppendLine($"    ],");
            }
            else
            {
                asmdef.AppendLine($"    \"references\": [],");
            }
            
            if (editorOnly)
            {
                asmdef.AppendLine($"    \"includePlatforms\": [");
                asmdef.AppendLine($"        \"Editor\"");
                asmdef.AppendLine($"    ],");
            }
            else
            {
                asmdef.AppendLine($"    \"includePlatforms\": [],");
            }
            
            asmdef.AppendLine($"    \"excludePlatforms\": [],");
            asmdef.AppendLine($"    \"allowUnsafeCode\": false,");
            asmdef.AppendLine($"    \"overrideReferences\": false,");
            asmdef.AppendLine($"    \"precompiledReferences\": [],");
            asmdef.AppendLine($"    \"autoReferenced\": true,");
            asmdef.AppendLine($"    \"defineConstraints\": [],");
            asmdef.AppendLine($"    \"versionDefines\": [],");
            asmdef.AppendLine($"    \"noEngineReferences\": false");
            asmdef.AppendLine("}");
            
            File.WriteAllText(Path.Combine(folder, $"{asmdefName}.asmdef"), asmdef.ToString());
        }

        private void CreatePlatformAsmdef(string folder, string asmdefName, string rootNamespace, string platform)
        {
            var asmdef = new System.Text.StringBuilder();
            asmdef.AppendLine("{");
            asmdef.AppendLine($"    \"name\": \"{asmdefName}\",");
            asmdef.AppendLine($"    \"rootNamespace\": \"{rootNamespace}\",");
            asmdef.AppendLine($"    \"references\": [],");
            asmdef.AppendLine($"    \"includePlatforms\": [");
            asmdef.AppendLine($"        \"{platform}\"");
            asmdef.AppendLine($"    ],");
            asmdef.AppendLine($"    \"excludePlatforms\": [],");
            asmdef.AppendLine($"    \"allowUnsafeCode\": false,");
            asmdef.AppendLine($"    \"overrideReferences\": false,");
            asmdef.AppendLine($"    \"precompiledReferences\": [],");
            asmdef.AppendLine($"    \"autoReferenced\": true,");
            asmdef.AppendLine($"    \"defineConstraints\": [],");
            asmdef.AppendLine($"    \"versionDefines\": [],");
            asmdef.AppendLine($"    \"noEngineReferences\": false");
            asmdef.AppendLine("}");
            
            File.WriteAllText(Path.Combine(folder, $"{asmdefName}.asmdef"), asmdef.ToString());
        }

        private void CreateTestAsmdef(string folder, string asmdefName, string referenceAsmdef, bool editorOnly)
        {
            var asmdef = new System.Text.StringBuilder();
            asmdef.AppendLine("{");
            asmdef.AppendLine($"    \"name\": \"{asmdefName}\",");
            asmdef.AppendLine($"    \"rootNamespace\": \"{asmdefName}\",");
            asmdef.AppendLine($"    \"references\": [");
            asmdef.AppendLine($"        \"UnityEngine.TestRunner\",");
            asmdef.AppendLine($"        \"UnityEditor.TestRunner\",");
            asmdef.AppendLine($"        \"{referenceAsmdef}\"");
            asmdef.AppendLine($"    ],");
            
            if (editorOnly)
            {
                asmdef.AppendLine($"    \"includePlatforms\": [");
                asmdef.AppendLine($"        \"Editor\"");
                asmdef.AppendLine($"    ],");
            }
            else
            {
                asmdef.AppendLine($"    \"includePlatforms\": [],");
            }
            
            asmdef.AppendLine($"    \"excludePlatforms\": [],");
            asmdef.AppendLine($"    \"allowUnsafeCode\": false,");
            asmdef.AppendLine($"    \"overrideReferences\": true,");
            asmdef.AppendLine($"    \"precompiledReferences\": [");
            asmdef.AppendLine($"        \"nunit.framework.dll\"");
            asmdef.AppendLine($"    ],");
            asmdef.AppendLine($"    \"autoReferenced\": false,");
            asmdef.AppendLine($"    \"defineConstraints\": [");
            asmdef.AppendLine($"        \"UNITY_INCLUDE_TESTS\"");
            asmdef.AppendLine($"    ],");
            asmdef.AppendLine($"    \"versionDefines\": [],");
            asmdef.AppendLine($"    \"noEngineReferences\": false");
            asmdef.AppendLine("}");
            
            File.WriteAllText(Path.Combine(folder, $"{asmdefName}.asmdef"), asmdef.ToString());
        }

        // Template code generation methods
        
        private void CreateXRLoaderTemplate(string folder, string packageId)
        {
            string className = ToPascalCase(packageName) + "Loader";
            string settingsClassName = ToPascalCase(packageName) + "Settings";
            string settingsKey = packageId + ".settings";
            var code = new System.Text.StringBuilder();
            code.AppendLine("using System.Collections.Generic;");
            code.AppendLine("using UnityEngine;");
            code.AppendLine("using UnityEngine.XR;");
            code.AppendLine("using UnityEngine.XR.Management;");
            code.AppendLine();
            code.AppendLine($"namespace {GetNamespace(packageId)}");
            code.AppendLine("{");
            code.AppendLine("    /// <summary>");
            code.AppendLine($"    /// XR Loader for {displayName}.");
            code.AppendLine("    /// Manages the lifecycle of XR subsystems.");
            code.AppendLine("    /// </summary>");
            code.AppendLine($"    public class {className} : XRLoaderHelper");
            code.AppendLine("    {");
            code.AppendLine("        private static List<XRDisplaySubsystemDescriptor> displaySubsystemDescriptors = new List<XRDisplaySubsystemDescriptor>();");
            code.AppendLine("        private static List<XRInputSubsystemDescriptor> inputSubsystemDescriptors = new List<XRInputSubsystemDescriptor>();");
            code.AppendLine();
            code.AppendLine($"        private {settingsClassName} m_Settings;");
            code.AppendLine();
            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// Gets the current settings for this XR loader.");
            code.AppendLine("        /// </summary>");
            code.AppendLine($"        public {settingsClassName} Settings => m_Settings;");
            code.AppendLine();
            code.AppendLine("        public override bool Initialize()");
            code.AppendLine("        {");
            code.AppendLine("            // Load settings from XR Management");
            code.AppendLine($"            m_Settings = GetSettings();");
            code.AppendLine();
            code.AppendLine("            if (m_Settings != null && m_Settings.EnableDebugLogging)");
            code.AppendLine("            {");
            code.AppendLine("                Debug.Log(\"" + className + ": Initializing XR subsystems\");");
            code.AppendLine("                Debug.Log($\"  Target Refresh Rate: {m_Settings.TargetRefreshRate}Hz\");");
            code.AppendLine("                Debug.Log($\"  Positional Tracking: {m_Settings.EnablePositionalTracking}\");");
            code.AppendLine("                Debug.Log($\"  Rotational Tracking: {m_Settings.EnableRotationalTracking}\");");
            code.AppendLine("            }");
            code.AppendLine();
            code.AppendLine("            // TODO: Initialize your XR subsystems using settings");
            code.AppendLine("            // Example: Create and start display subsystem");
            code.AppendLine("            // CreateSubsystem<XRDisplaySubsystemDescriptor, XRDisplaySubsystem>(displaySubsystemDescriptors, \"YourDisplaySubsystemId\");");
            code.AppendLine();
            code.AppendLine("            return true;");
            code.AppendLine("        }");
            code.AppendLine();
            code.AppendLine("        public override bool Start()");
            code.AppendLine("        {");
            code.AppendLine("            if (m_Settings != null && m_Settings.EnableDebugLogging)");
            code.AppendLine("            {");
            code.AppendLine("                Debug.Log(\"" + className + ": Starting XR subsystems\");");
            code.AppendLine("            }");
            code.AppendLine();
            code.AppendLine("            // TODO: Start your XR subsystems");
            code.AppendLine("            // Example: Start display subsystem");
            code.AppendLine("            // StartSubsystem<XRDisplaySubsystem>();");
            code.AppendLine();
            code.AppendLine("            return true;");
            code.AppendLine("        }");
            code.AppendLine();
            code.AppendLine("        public override bool Stop()");
            code.AppendLine("        {");
            code.AppendLine("            if (m_Settings != null && m_Settings.EnableDebugLogging)");
            code.AppendLine("            {");
            code.AppendLine("                Debug.Log(\"" + className + ": Stopping XR subsystems\");");
            code.AppendLine("            }");
            code.AppendLine();
            code.AppendLine("            // TODO: Stop your XR subsystems");
            code.AppendLine("            // Example: Stop display subsystem");
            code.AppendLine("            // StopSubsystem<XRDisplaySubsystem>();");
            code.AppendLine();
            code.AppendLine("            return true;");
            code.AppendLine("        }");
            code.AppendLine();
            code.AppendLine("        public override bool Deinitialize()");
            code.AppendLine("        {");
            code.AppendLine("            if (m_Settings != null && m_Settings.EnableDebugLogging)");
            code.AppendLine("            {");
            code.AppendLine("                Debug.Log(\"" + className + ": Deinitializing XR subsystems\");");
            code.AppendLine("            }");
            code.AppendLine();
            code.AppendLine("            // TODO: Cleanup and destroy your XR subsystems");
            code.AppendLine("            // Example: Destroy display subsystem");
            code.AppendLine("            // DestroySubsystem<XRDisplaySubsystem>();");
            code.AppendLine();
            code.AppendLine("            m_Settings = null;");
            code.AppendLine("            return true;");
            code.AppendLine("        }");
            code.AppendLine();
            code.AppendLine($"        private {settingsClassName} GetSettings()");
            code.AppendLine("        {");
            code.AppendLine("            // At runtime, settings are retrieved from XRGeneralSettings");
            code.AppendLine($"            {settingsClassName} settings = null;");
            code.AppendLine();
            code.AppendLine("#if UNITY_EDITOR");
            code.AppendLine("            // In editor, get settings from EditorBuildSettings");
            code.AppendLine("            UnityEngine.Object settingsObj = null;");
            code.AppendLine($"            UnityEditor.EditorBuildSettings.TryGetConfigObject(\"{settingsKey}\", out settingsObj);");
            code.AppendLine($"            settings = settingsObj as {settingsClassName};");
            code.AppendLine("#else");
            code.AppendLine("            // At runtime, get settings from XRGeneralSettings");
            code.AppendLine("            var generalSettings = XRGeneralSettings.Instance;");
            code.AppendLine("            if (generalSettings != null && generalSettings.Manager != null)");
            code.AppendLine("            {");
            code.AppendLine("                var loader = generalSettings.Manager.activeLoader;");
            code.AppendLine($"                if (loader is {className} thisLoader)");
            code.AppendLine("                {");
            code.AppendLine("                    // Settings are serialized with the loader at build time");
            code.AppendLine("                }");
            code.AppendLine("            }");
            code.AppendLine("#endif");
            code.AppendLine();
            code.AppendLine("            return settings;");
            code.AppendLine("        }");
            code.AppendLine("    }");
            code.AppendLine("}");

            File.WriteAllText(Path.Combine(folder, $"{className}.cs"), code.ToString());
        }

        private void CreateXRSettingsTemplate(string folder, string packageId)
        {
            string className = ToPascalCase(packageName) + "Settings";
            string settingsKey = packageId + ".settings";
            var code = new System.Text.StringBuilder();
            code.AppendLine("using UnityEngine;");
            code.AppendLine("using UnityEngine.XR.Management;");
            code.AppendLine();
            code.AppendLine($"namespace {GetNamespace(packageId)}");
            code.AppendLine("{");
            code.AppendLine("    /// <summary>");
            code.AppendLine($"    /// XR Settings for {displayName}.");
            code.AppendLine("    /// These settings appear in Project Settings > XR Plug-in Management per platform.");
            code.AppendLine("    /// </summary>");
            code.AppendLine($"    [XRConfigurationData(\"{displayName}\", \"{settingsKey}\")]");
            code.AppendLine($"    public class {className} : ScriptableObject");
            code.AppendLine("    {");
            code.AppendLine("        [Header(\"General Settings\")]");
            code.AppendLine();
            code.AppendLine("        [SerializeField]");
            code.AppendLine("        [Tooltip(\"Enable verbose debug logging\")]");
            code.AppendLine("        private bool enableDebugLogging = false;");
            code.AppendLine();
            code.AppendLine("        /// <summary>Whether debug logging is enabled.</summary>");
            code.AppendLine("        public bool EnableDebugLogging => enableDebugLogging;");
            code.AppendLine();
            code.AppendLine("        [Header(\"Display Settings\")]");
            code.AppendLine();
            code.AppendLine("        [SerializeField]");
            code.AppendLine("        [Tooltip(\"Target display refresh rate in Hz\")]");
            code.AppendLine("        [Range(60, 144)]");
            code.AppendLine("        private int targetRefreshRate = 90;");
            code.AppendLine();
            code.AppendLine("        /// <summary>Target display refresh rate.</summary>");
            code.AppendLine("        public int TargetRefreshRate => targetRefreshRate;");
            code.AppendLine();
            code.AppendLine("        [Header(\"Tracking Settings\")]");
            code.AppendLine();
            code.AppendLine("        [SerializeField]");
            code.AppendLine("        [Tooltip(\"Enable positional tracking\")]");
            code.AppendLine("        private bool enablePositionalTracking = true;");
            code.AppendLine();
            code.AppendLine("        /// <summary>Whether positional tracking is enabled.</summary>");
            code.AppendLine("        public bool EnablePositionalTracking => enablePositionalTracking;");
            code.AppendLine();
            code.AppendLine("        [SerializeField]");
            code.AppendLine("        [Tooltip(\"Enable rotational tracking\")]");
            code.AppendLine("        private bool enableRotationalTracking = true;");
            code.AppendLine();
            code.AppendLine("        /// <summary>Whether rotational tracking is enabled.</summary>");
            code.AppendLine("        public bool EnableRotationalTracking => enableRotationalTracking;");
            code.AppendLine();
            code.AppendLine("#if UNITY_EDITOR");
            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// Gets the settings for the specified build target group.");
            code.AppendLine("        /// </summary>");
            code.AppendLine($"        public static {className} GetSettings(UnityEditor.BuildTargetGroup buildTargetGroup)");
            code.AppendLine("        {");
            code.AppendLine("            UnityEngine.Object settingsObj = null;");
            code.AppendLine("            UnityEditor.EditorBuildSettings.TryGetConfigObject(");
            code.AppendLine($"                \"{settingsKey}\",");
            code.AppendLine("                out settingsObj);");
            code.AppendLine($"            return settingsObj as {className};");
            code.AppendLine("        }");
            code.AppendLine("#endif");
            code.AppendLine();
            code.AppendLine("        // TODO: Add additional XR provider settings as needed");
            code.AppendLine("        // Examples:");
            code.AppendLine("        // - Foveated rendering level");
            code.AppendLine("        // - Hand tracking enable/disable");
            code.AppendLine("        // - Passthrough mode settings");
            code.AppendLine("        // - Controller tracking options");
            code.AppendLine("    }");
            code.AppendLine("}");

            File.WriteAllText(Path.Combine(folder, $"{className}.cs"), code.ToString());
        }

        private void CreateXRPackageMetadataTemplate(string folder, string packageId)
        {
            string className = ToPascalCase(packageName) + "PackageMetadata";
            string loaderClassName = ToPascalCase(packageName) + "Loader";
            string settingsClassName = ToPascalCase(packageName) + "Settings";
            string runtimeNamespace = GetNamespace(packageId);
            var code = new System.Text.StringBuilder();
            code.AppendLine("using System.Collections.Generic;");
            code.AppendLine("using UnityEditor;");
            code.AppendLine("using UnityEditor.XR.Management.Metadata;");
            code.AppendLine("using UnityEngine;");
            code.AppendLine();
            code.AppendLine($"namespace {GetNamespace(packageId)}.Editor");
            code.AppendLine("{");
            code.AppendLine("    /// <summary>");
            code.AppendLine($"    /// XR Package metadata for {displayName}.");
            code.AppendLine("    /// This class registers the plugin with Unity's XR Plugin Management system.");
            code.AppendLine("    /// </summary>");
            code.AppendLine($"    public class {className} : IXRPackage");
            code.AppendLine("    {");
            code.AppendLine("        private class LoaderMetadata : IXRLoaderMetadata");
            code.AppendLine("        {");
            code.AppendLine("            public string loaderName { get; set; }");
            code.AppendLine("            public string loaderType { get; set; }");
            code.AppendLine("            public List<BuildTargetGroup> supportedBuildTargets { get; set; }");
            code.AppendLine("        }");
            code.AppendLine();
            code.AppendLine("        private class PackageMetadata : IXRPackageMetadata");
            code.AppendLine("        {");
            code.AppendLine("            public string packageName { get; set; }");
            code.AppendLine("            public string packageId { get; set; }");
            code.AppendLine("            public string settingsType { get; set; }");
            code.AppendLine("            public List<IXRLoaderMetadata> loaderMetadata { get; set; }");
            code.AppendLine("        }");
            code.AppendLine();
            code.AppendLine("        private static IXRPackageMetadata s_Metadata = new PackageMetadata()");
            code.AppendLine("        {");
            code.AppendLine($"            packageName = \"{displayName}\",");
            code.AppendLine($"            packageId = \"{packageId}\",");
            code.AppendLine($"            settingsType = typeof({runtimeNamespace}.{settingsClassName}).FullName,");
            code.AppendLine("            loaderMetadata = new List<IXRLoaderMetadata>()");
            code.AppendLine("            {");
            code.AppendLine("                new LoaderMetadata()");
            code.AppendLine("                {");
            code.AppendLine($"                    loaderName = \"{displayName}\",");
            code.AppendLine($"                    loaderType = typeof({runtimeNamespace}.{loaderClassName}).FullName,");
            code.AppendLine("                    supportedBuildTargets = new List<BuildTargetGroup>()");
            code.AppendLine("                    {");
            code.AppendLine("                        BuildTargetGroup.Standalone,");
            code.AppendLine("                        BuildTargetGroup.Android,");
            code.AppendLine("                        BuildTargetGroup.iOS");
            code.AppendLine("                        // TODO: Add or remove supported platforms as needed");
            code.AppendLine("                    }");
            code.AppendLine("                }");
            code.AppendLine("            }");
            code.AppendLine("        };");
            code.AppendLine();
            code.AppendLine("        public IXRPackageMetadata metadata => s_Metadata;");
            code.AppendLine();
            code.AppendLine("        public bool PopulateNewSettingsInstance(ScriptableObject obj)");
            code.AppendLine("        {");
            code.AppendLine($"            var settings = obj as {runtimeNamespace}.{settingsClassName};");
            code.AppendLine("            if (settings != null)");
            code.AppendLine("            {");
            code.AppendLine("                // Initialize default settings for new instances");
            code.AppendLine("                // This is called when the user enables this plugin for a new platform");
            code.AppendLine("                return true;");
            code.AppendLine("            }");
            code.AppendLine("            return false;");
            code.AppendLine("        }");
            code.AppendLine("    }");
            code.AppendLine("}");

            File.WriteAllText(Path.Combine(folder, $"{className}.cs"), code.ToString());
        }

        private void CreateXRBuildProcessorTemplate(string folder, string packageId)
        {
            string className = ToPascalCase(packageName) + "BuildProcessor";
            var code = new System.Text.StringBuilder();
            code.AppendLine("using UnityEditor;");
            code.AppendLine("using UnityEditor.Build;");
            code.AppendLine("using UnityEditor.Build.Reporting;");
            code.AppendLine();
            code.AppendLine($"namespace {GetNamespace(packageId)}.Editor");
            code.AppendLine("{");
            code.AppendLine($"    public class {className} : IPreprocessBuildWithReport, IPostprocessBuildWithReport");
            code.AppendLine("    {");
            code.AppendLine("        public int callbackOrder => 0;");
            code.AppendLine();
            code.AppendLine("        public void OnPreprocessBuild(BuildReport report)");
            code.AppendLine("        {");
            code.AppendLine("            // TODO: Add any pre-build processing");
            code.AppendLine("            // Example: Configure platform-specific settings");
            code.AppendLine("        }");
            code.AppendLine();
            code.AppendLine("        public void OnPostprocessBuild(BuildReport report)");
            code.AppendLine("        {");
            code.AppendLine("            // TODO: Add any post-build processing");
            code.AppendLine("            // Example: Copy native plugins to build");
            code.AppendLine("        }");
            code.AppendLine("    }");
            code.AppendLine("}");
            
            File.WriteAllText(Path.Combine(folder, $"{className}.cs"), code.ToString());
        }

        private void CreateDisplaySubsystemExample(string folder, string packageId)
        {
            string className = ToPascalCase(packageName) + "DisplaySubsystem";
            string providerClassName = ToPascalCase(packageName) + "DisplayProvider";
            string subsystemId = ToPascalCase(packageName) + "-Display";
            var code = new System.Text.StringBuilder();
            code.AppendLine("using System;");
            code.AppendLine("using System.Collections.Generic;");
            code.AppendLine("using UnityEngine;");
            code.AppendLine("using UnityEngine.XR;");
            code.AppendLine();
            code.AppendLine($"namespace {GetNamespace(packageId)}");
            code.AppendLine("{");
            code.AppendLine("    /// <summary>");
            code.AppendLine($"    /// EXAMPLE Display Subsystem - Customize for your device");
            code.AppendLine("    /// ");
            code.AppendLine("    /// This is a template showing the display subsystem pattern.");
            code.AppendLine("    /// You MUST customize this for your specific XR device.");
            code.AppendLine("    /// ");
            code.AppendLine("    /// TODO:");
            code.AppendLine("    /// - Update display activation logic (Display 0, 1, 3, etc. depending on device)");
            code.AppendLine("    /// - Set correct resolution for your device");
            code.AppendLine("    /// - Add stereo rendering if needed (currently mono)");
            code.AppendLine("    /// - Implement device-specific render passes");
            code.AppendLine("    /// - Add native library integration if required");
            code.AppendLine("    /// </summary>");
            code.AppendLine($"    public class {className} : XRDisplaySubsystem");
            code.AppendLine("    {");
            code.AppendLine($"        /// <summary>Subsystem ID used for registration.</summary>");
            code.AppendLine($"        public const string ID = \"{subsystemId}\";");
            code.AppendLine();
            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// Registers the display subsystem descriptor with Unity.");
            code.AppendLine("        /// Call this from your XRLoader.Initialize() method.");
            code.AppendLine("        /// </summary>");
            code.AppendLine("        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]");
            code.AppendLine("        public static void RegisterDescriptor()");
            code.AppendLine("        {");
            code.AppendLine("            XRDisplaySubsystemDescriptor.RegisterDescriptor(new XRDisplaySubsystemDescriptor.Cinfo");
            code.AppendLine("            {");
            code.AppendLine("                id = ID,");
            code.AppendLine($"                providerType = typeof({providerClassName}),");
            code.AppendLine($"                subsystemTypeOverride = typeof({className}),");
            code.AppendLine("                // TODO: Configure display capabilities for your device");
            code.AppendLine("                // disablesLegacyVr = true,                    // Set true if not using legacy VR");
            code.AppendLine("                // enableXRMirrorView = true,                  // Enable mirror view on desktop");
            code.AppendLine("                // supportsTextureArrayProjection = false,    // Multi-view rendering support");
            code.AppendLine("            });");
            code.AppendLine("        }");
            code.AppendLine("    }");
            code.AppendLine();
            code.AppendLine("    /// <summary>");
            code.AppendLine("    /// Provider implementation for the display subsystem.");
            code.AppendLine("    /// This is where the actual display logic lives.");
            code.AppendLine("    /// </summary>");
            code.AppendLine($"    public class {providerClassName} : XRDisplaySubsystem.Provider");
            code.AppendLine("    {");
            code.AppendLine("        // TODO: Add display state tracking");
            code.AppendLine("        // private bool m_IsRunning;");
            code.AppendLine("        // private int m_TargetDisplayIndex = 0;");
            code.AppendLine();
            code.AppendLine("        #region Lifecycle");
            code.AppendLine();
            code.AppendLine("        public override void Start()");
            code.AppendLine("        {");
            code.AppendLine("            // TODO: Initialize and activate your display");
            code.AppendLine("            // ");
            code.AppendLine("            // Example for secondary display activation:");
            code.AppendLine("            // if (Display.displays.Length > m_TargetDisplayIndex)");
            code.AppendLine("            // {");
            code.AppendLine("            //     Display.displays[m_TargetDisplayIndex].Activate();");
            code.AppendLine("            //     Display.displays[m_TargetDisplayIndex].SetRenderingResolution(1920, 1080);");
            code.AppendLine("            // }");
            code.AppendLine("            // ");
            code.AppendLine("            // Example for AR glasses (often Display 3):");
            code.AppendLine("            // if (Display.displays.Length > 3)");
            code.AppendLine("            // {");
            code.AppendLine("            //     Display.displays[3].Activate(3840, 1200, 90);");
            code.AppendLine("            // }");
            code.AppendLine("            ");
            code.AppendLine("            Debug.Log(\"" + className + ": Display subsystem started\");");
            code.AppendLine("        }");
            code.AppendLine();
            code.AppendLine("        public override void Stop()");
            code.AppendLine("        {");
            code.AppendLine("            // TODO: Deactivate display and cleanup");
            code.AppendLine("            // ");
            code.AppendLine("            // Note: Unity doesn't provide Display.Deactivate()");
            code.AppendLine("            // You may need device-specific shutdown logic");
            code.AppendLine("            ");
            code.AppendLine("            Debug.Log(\"" + className + ": Display subsystem stopped\");");
            code.AppendLine("        }");
            code.AppendLine();
            code.AppendLine("        public override void Destroy()");
            code.AppendLine("        {");
            code.AppendLine("            // TODO: Final cleanup - release native resources");
            code.AppendLine("        }");
            code.AppendLine();
            code.AppendLine("        #endregion");
            code.AppendLine();
            code.AppendLine("        #region Rendering");
            code.AppendLine();
            code.AppendLine("        // TODO: Override rendering methods as needed for your device");
            code.AppendLine("        // ");
            code.AppendLine("        // public override bool TryGetRenderPass(int renderPassIndex, out XRRenderPass renderPass)");
            code.AppendLine("        // {");
            code.AppendLine("        //     // Configure render passes for stereo or mono rendering");
            code.AppendLine("        // }");
            code.AppendLine("        // ");
            code.AppendLine("        // public override int GetRenderPassCount() => 1; // or 2 for stereo");
            code.AppendLine();
            code.AppendLine("        #endregion");
            code.AppendLine("    }");
            code.AppendLine("}");

            File.WriteAllText(Path.Combine(folder, $"{className}Example.cs"), code.ToString());
        }

        private void CreateInputSubsystemStub(string folder, string packageId)
        {
            string className = ToPascalCase(packageName) + "InputSubsystem";
            string providerClassName = ToPascalCase(packageName) + "InputProvider";
            string subsystemId = ToPascalCase(packageName) + "-Input";
            var code = new System.Text.StringBuilder();
            code.AppendLine("using System;");
            code.AppendLine("using System.Collections.Generic;");
            code.AppendLine("using UnityEngine;");
            code.AppendLine("using UnityEngine.XR;");
            code.AppendLine();
            code.AppendLine($"namespace {GetNamespace(packageId)}");
            code.AppendLine("{");
            code.AppendLine("    /// <summary>");
            code.AppendLine("    /// STUB Input Subsystem - REQUIRES IMPLEMENTATION");
            code.AppendLine("    /// ");
            code.AppendLine("    /// This stub shows the input subsystem structure. You MUST implement:");
            code.AppendLine("    /// - TryGetFeatureValue for pose data (position, rotation)");
            code.AppendLine("    /// - Native library integration for sensor/IMU data");
            code.AppendLine("    /// - Device-specific tracking logic");
            code.AppendLine("    /// - Controller/hand tracking if applicable");
            code.AppendLine("    /// ");
            code.AppendLine("    /// See Unity XR Input Subsystem documentation for full API.");
            code.AppendLine("    /// </summary>");
            code.AppendLine($"    public class {className} : XRInputSubsystem");
            code.AppendLine("    {");
            code.AppendLine($"        /// <summary>Subsystem ID used for registration.</summary>");
            code.AppendLine($"        public const string ID = \"{subsystemId}\";");
            code.AppendLine();
            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// Registers the input subsystem descriptor with Unity.");
            code.AppendLine("        /// Call this from your XRLoader.Initialize() method.");
            code.AppendLine("        /// </summary>");
            code.AppendLine("        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]");
            code.AppendLine("        public static void RegisterDescriptor()");
            code.AppendLine("        {");
            code.AppendLine("            XRInputSubsystemDescriptor.RegisterDescriptor(new XRInputSubsystemDescriptor.Cinfo");
            code.AppendLine("            {");
            code.AppendLine("                id = ID,");
            code.AppendLine($"                providerType = typeof({providerClassName}),");
            code.AppendLine($"                subsystemTypeOverride = typeof({className}),");
            code.AppendLine("            });");
            code.AppendLine("        }");
            code.AppendLine("    }");
            code.AppendLine();
            code.AppendLine("    /// <summary>");
            code.AppendLine("    /// Provider implementation for the input subsystem.");
            code.AppendLine("    /// This is where you implement pose tracking and input handling.");
            code.AppendLine("    /// </summary>");
            code.AppendLine($"    public class {providerClassName} : XRInputSubsystem.Provider");
            code.AppendLine("    {");
            code.AppendLine("        // TODO: Add native library bindings");
            code.AppendLine("        // ");
            code.AppendLine("        // Example native bindings:");
            code.AppendLine("        // [DllImport(\"your_native_library\")]");
            code.AppendLine("        // private static extern bool GetHeadPose(out Vector3 position, out Quaternion rotation);");
            code.AppendLine("        // ");
            code.AppendLine("        // [DllImport(\"your_native_library\")]");
            code.AppendLine("        // private static extern bool GetIMUData(out Vector3 acceleration, out Vector3 gyroscope);");
            code.AppendLine();
            code.AppendLine("        #region Lifecycle");
            code.AppendLine();
            code.AppendLine("        public override void Start()");
            code.AppendLine("        {");
            code.AppendLine("            // TODO: Initialize input/tracking system");
            code.AppendLine("            // - Connect to native tracking library");
            code.AppendLine("            // - Start IMU/sensor polling");
            code.AppendLine("            // - Register input devices with Unity");
            code.AppendLine("            ");
            code.AppendLine("            Debug.Log(\"" + className + ": Input subsystem started\");");
            code.AppendLine("        }");
            code.AppendLine();
            code.AppendLine("        public override void Stop()");
            code.AppendLine("        {");
            code.AppendLine("            // TODO: Stop tracking and cleanup");
            code.AppendLine("            // - Disconnect from native library");
            code.AppendLine("            // - Stop sensor polling");
            code.AppendLine("            // - Unregister input devices");
            code.AppendLine("            ");
            code.AppendLine("            Debug.Log(\"" + className + ": Input subsystem stopped\");");
            code.AppendLine("        }");
            code.AppendLine();
            code.AppendLine("        public override void Destroy()");
            code.AppendLine("        {");
            code.AppendLine("            // TODO: Final cleanup - release all native resources");
            code.AppendLine("        }");
            code.AppendLine();
            code.AppendLine("        #endregion");
            code.AppendLine();
            code.AppendLine("        #region Input Tracking");
            code.AppendLine();
            code.AppendLine("        // TODO: Implement TryGetFeatureValue for your tracked devices");
            code.AppendLine("        // ");
            code.AppendLine("        // This is the core method for providing pose data to Unity.");
            code.AppendLine("        // You need to:");
            code.AppendLine("        // 1. Map your device's sensor data to Unity's coordinate system");
            code.AppendLine("        // 2. Return pose data when requested via InputDevice.TryGetFeatureValue");
            code.AppendLine("        // ");
            code.AppendLine("        // Example implementation approach:");
            code.AppendLine("        // ");
            code.AppendLine("        // private Quaternion m_HeadRotation = Quaternion.identity;");
            code.AppendLine("        // private Vector3 m_HeadPosition = Vector3.zero;");
            code.AppendLine("        // ");
            code.AppendLine("        // public void UpdateTracking()");
            code.AppendLine("        // {");
            code.AppendLine("        //     // Called each frame to update pose from native library");
            code.AppendLine("        //     if (GetHeadPose(out Vector3 pos, out Quaternion rot))");
            code.AppendLine("        //     {");
            code.AppendLine("        //         m_HeadPosition = pos;");
            code.AppendLine("        //         m_HeadRotation = rot;");
            code.AppendLine("        //     }");
            code.AppendLine("        // }");
            code.AppendLine();
            code.AppendLine("        #endregion");
            code.AppendLine();
            code.AppendLine("        #region Device Registration");
            code.AppendLine();
            code.AppendLine("        // TODO: Register your input devices with Unity's Input System");
            code.AppendLine("        // ");
            code.AppendLine("        // Use InputDevices.SendHapticImpulse, InputDevices.SendHapticBuffer");
            code.AppendLine("        // for haptic feedback if your device supports it.");
            code.AppendLine("        // ");
            code.AppendLine("        // Common device characteristics:");
            code.AppendLine("        // - InputDeviceCharacteristics.HeadMounted");
            code.AppendLine("        // - InputDeviceCharacteristics.TrackedDevice");
            code.AppendLine("        // - InputDeviceCharacteristics.Controller");
            code.AppendLine("        // - InputDeviceCharacteristics.Left / Right");
            code.AppendLine();
            code.AppendLine("        #endregion");
            code.AppendLine("    }");
            code.AppendLine("}");

            File.WriteAllText(Path.Combine(folder, $"{className}Stub.cs"), code.ToString());
        }

        private void CreateSubsystemsReadme(string folder)
        {
            var readme = new System.Text.StringBuilder();
            readme.AppendLine("# Subsystems Folder");
            readme.AppendLine();
            readme.AppendLine("This folder contains XR subsystem implementations for your device.");
            readme.AppendLine();
            readme.AppendLine("## Included Examples");
            readme.AppendLine();
            readme.AppendLine("- **DisplaySubsystemExample.cs** - Template for display/rendering subsystem");
            readme.AppendLine("- **InputSubsystemStub.cs** - Stub for input/tracking subsystem");
            readme.AppendLine();
            readme.AppendLine("## Available Subsystem Types");
            readme.AppendLine();
            readme.AppendLine("Unity provides several XR subsystem types you can implement:");
            readme.AppendLine();
            readme.AppendLine("| Subsystem | Purpose | Priority |");
            readme.AppendLine("|-----------|---------|----------|");
            readme.AppendLine("| `XRDisplaySubsystem` | Rendering, display activation | **Required** |");
            readme.AppendLine("| `XRInputSubsystem` | Tracking, controllers, input | **Required** |");
            readme.AppendLine("| `XRSessionSubsystem` | Session lifecycle management | Optional |");
            readme.AppendLine("| `XRCameraSubsystem` | Camera feed (AR) | AR devices |");
            readme.AppendLine("| `XRPlaneSubsystem` | Plane detection (AR) | AR devices |");
            readme.AppendLine("| `XRAnchorSubsystem` | World anchors (AR) | AR devices |");
            readme.AppendLine("| `XRRaycastSubsystem` | World raycasting (AR) | AR devices |");
            readme.AppendLine("| `XRMeshSubsystem` | Mesh generation | Advanced |");
            readme.AppendLine();
            readme.AppendLine("## Implementation Pattern");
            readme.AppendLine();
            readme.AppendLine("Each subsystem follows this pattern:");
            readme.AppendLine();
            readme.AppendLine("```csharp");
            readme.AppendLine("// 1. Define subsystem class (inherits from XR*Subsystem)");
            readme.AppendLine("public class MyDisplaySubsystem : XRDisplaySubsystem");
            readme.AppendLine("{");
            readme.AppendLine("    public const string ID = \"My-Display\";");
            readme.AppendLine("    ");
            readme.AppendLine("    // Register with Unity at startup");
            readme.AppendLine("    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]");
            readme.AppendLine("    public static void RegisterDescriptor() { ... }");
            readme.AppendLine("}");
            readme.AppendLine();
            readme.AppendLine("// 2. Define provider class (inherits from XR*Subsystem.Provider)");
            readme.AppendLine("public class MyDisplayProvider : XRDisplaySubsystem.Provider");
            readme.AppendLine("{");
            readme.AppendLine("    public override void Start() { }   // Initialize");
            readme.AppendLine("    public override void Stop() { }    // Pause");
            readme.AppendLine("    public override void Destroy() { } // Cleanup");
            readme.AppendLine("}");
            readme.AppendLine("```");
            readme.AppendLine();
            readme.AppendLine("## Connecting Subsystems to Loader");
            readme.AppendLine();
            readme.AppendLine("In your XRLoader, create and start subsystems:");
            readme.AppendLine();
            readme.AppendLine("```csharp");
            readme.AppendLine("public override bool Initialize()");
            readme.AppendLine("{");
            readme.AppendLine("    CreateSubsystem<XRDisplaySubsystemDescriptor, XRDisplaySubsystem>(");
            readme.AppendLine("        displayDescriptors, MyDisplaySubsystem.ID);");
            readme.AppendLine("    CreateSubsystem<XRInputSubsystemDescriptor, XRInputSubsystem>(");
            readme.AppendLine("        inputDescriptors, MyInputSubsystem.ID);");
            readme.AppendLine("    return true;");
            readme.AppendLine("}");
            readme.AppendLine("```");
            readme.AppendLine();
            readme.AppendLine("## Resources");
            readme.AppendLine();
            readme.AppendLine("- [Unity XR SDK Documentation](https://docs.unity3d.com/Manual/xr-sdk.html)");
            readme.AppendLine("- [XR Subsystem Lifecycle](https://docs.unity3d.com/Manual/xr-subsystems.html)");

            File.WriteAllText(Path.Combine(folder, "_README.md"), readme.ToString());
        }

        private void CreatePluginsReadme(string folder)
        {
            var readme = new System.Text.StringBuilder();
            readme.AppendLine("# Native Plugins Folder");
            readme.AppendLine();
            readme.AppendLine("This folder contains native libraries for platform-specific functionality.");
            readme.AppendLine();
            readme.AppendLine("## Folder Structure");
            readme.AppendLine();
            readme.AppendLine("```");
            readme.AppendLine("Plugins/");
            readme.AppendLine("├── Android/");
            readme.AppendLine("│   ├── libs/");
            readme.AppendLine("│   │   ├── arm64-v8a/");
            readme.AppendLine("│   │   │   └── libyourlib.so");
            readme.AppendLine("│   │   └── armeabi-v7a/");
            readme.AppendLine("│   │       └── libyourlib.so");
            readme.AppendLine("│   └── yourlib.aar         (optional: Android Archive)");
            readme.AppendLine("├── iOS/");
            readme.AppendLine("│   ├── yourlib.framework/   (iOS framework)");
            readme.AppendLine("│   └── yourlib.a            (static library)");
            readme.AppendLine("├── x86_64/");
            readme.AppendLine("│   └── yourlib.dll          (Windows)");
            readme.AppendLine("│   └── yourlib.so           (Linux)");
            readme.AppendLine("└── macOS/");
            readme.AppendLine("    └── yourlib.bundle       (macOS bundle)");
            readme.AppendLine("```");
            readme.AppendLine();
            readme.AppendLine("## Adding Native Libraries");
            readme.AppendLine();
            readme.AppendLine("### 1. Copy library files to appropriate platform folder");
            readme.AppendLine();
            readme.AppendLine("### 2. Configure import settings in Unity");
            readme.AppendLine();
            readme.AppendLine("Select the native library in Unity and configure:");
            readme.AppendLine();
            readme.AppendLine("- **Android .so files**: Set CPU architecture (ARM64, ARMv7)");
            readme.AppendLine("- **iOS frameworks**: Enable \"Add to Embedded Binaries\"");
            readme.AppendLine("- **Windows .dll**: Set platform to \"Standalone\" and OS to \"Windows\"");
            readme.AppendLine();
            readme.AppendLine("### 3. Create C# bindings");
            readme.AppendLine();
            readme.AppendLine("```csharp");
            readme.AppendLine("public static class NativeBindings");
            readme.AppendLine("{");
            readme.AppendLine("#if UNITY_ANDROID && !UNITY_EDITOR");
            readme.AppendLine("    private const string LibraryName = \"yourlib\";");
            readme.AppendLine("#elif UNITY_IOS && !UNITY_EDITOR");
            readme.AppendLine("    private const string LibraryName = \"__Internal\";");
            readme.AppendLine("#else");
            readme.AppendLine("    private const string LibraryName = \"yourlib\";");
            readme.AppendLine("#endif");
            readme.AppendLine();
            readme.AppendLine("    [DllImport(LibraryName)]");
            readme.AppendLine("    public static extern bool Initialize();");
            readme.AppendLine("    ");
            readme.AppendLine("    [DllImport(LibraryName)]");
            readme.AppendLine("    public static extern void GetPose(out float x, out float y, out float z,");
            readme.AppendLine("                                      out float qx, out float qy, out float qz, out float qw);");
            readme.AppendLine("    ");
            readme.AppendLine("    [DllImport(LibraryName)]");
            readme.AppendLine("    public static extern void Shutdown();");
            readme.AppendLine("}");
            readme.AppendLine("```");
            readme.AppendLine();
            readme.AppendLine("## Common Native Library Functions");
            readme.AppendLine();
            readme.AppendLine("Typical XR device libraries expose:");
            readme.AppendLine();
            readme.AppendLine("| Function | Purpose |");
            readme.AppendLine("|----------|---------|");
            readme.AppendLine("| `Initialize()` | Start device connection |");
            readme.AppendLine("| `Shutdown()` | Clean disconnect |");
            readme.AppendLine("| `GetPose()` | Head/controller position & rotation |");
            readme.AppendLine("| `GetIMU()` | Raw accelerometer/gyroscope data |");
            readme.AppendLine("| `GetDisplayInfo()` | Resolution, refresh rate |");
            readme.AppendLine("| `SetDisplayMode()` | 2D/3D mode switching |");
            readme.AppendLine();
            readme.AppendLine("## Troubleshooting");
            readme.AppendLine();
            readme.AppendLine("- **DllNotFoundException**: Check library name matches DllImport");
            readme.AppendLine("- **EntryPointNotFoundException**: Function name mismatch or wrong library version");
            readme.AppendLine("- **Android crash on load**: Verify correct CPU architecture (arm64 vs armv7)");
            readme.AppendLine("- **iOS link errors**: Ensure framework is in \"Embedded Binaries\"");

            File.WriteAllText(Path.Combine(folder, "_README.md"), readme.ToString());
        }

        private void CreateGettingStarted(string packagePath, string packageId)
        {
            string className = ToPascalCase(packageName);
            var doc = new System.Text.StringBuilder();
            doc.AppendLine($"# Getting Started with {displayName}");
            doc.AppendLine();
            doc.AppendLine("This guide walks you through customizing this XR plugin template for your device.");
            doc.AppendLine();
            doc.AppendLine("## Quick Customization Checklist");
            doc.AppendLine();
            doc.AppendLine("### 1. Rename Everything");
            doc.AppendLine();
            doc.AppendLine("- [ ] Replace `" + className + "` with your device name in all C# files");
            doc.AppendLine("- [ ] Update `package.json` name, displayName, and description");
            doc.AppendLine("- [ ] Update assembly definition (.asmdef) names");
            doc.AppendLine("- [ ] Update namespace in all scripts");
            doc.AppendLine();
            doc.AppendLine("### 2. Implement Display Subsystem");
            doc.AppendLine();
            doc.AppendLine("Edit `Runtime/Subsystems/" + className + "DisplaySubsystemExample.cs`:");
            doc.AppendLine();
            doc.AppendLine("- [ ] Set correct display index (Display 0, 1, 2, 3, etc.)");
            doc.AppendLine("- [ ] Set your device's display resolution");
            doc.AppendLine("- [ ] Configure stereo vs mono rendering");
            doc.AppendLine("- [ ] Add any device-specific display initialization");
            doc.AppendLine();
            doc.AppendLine("```csharp");
            doc.AppendLine("// Example: Activate secondary display for AR glasses");
            doc.AppendLine("if (Display.displays.Length > 3)");
            doc.AppendLine("{");
            doc.AppendLine("    Display.displays[3].Activate(3840, 1200, 90);");
            doc.AppendLine("}");
            doc.AppendLine("```");
            doc.AppendLine();
            doc.AppendLine("### 3. Implement Input Subsystem");
            doc.AppendLine();
            doc.AppendLine("Edit `Runtime/Subsystems/" + className + "InputSubsystemStub.cs`:");
            doc.AppendLine();
            doc.AppendLine("- [ ] Add native library bindings for sensor access");
            doc.AppendLine("- [ ] Implement pose tracking (position/rotation)");
            doc.AppendLine("- [ ] Add head tracking logic from IMU/sensors");
            doc.AppendLine("- [ ] Register devices with Unity's Input System");
            doc.AppendLine();
            doc.AppendLine("### 4. Add Native Libraries");
            doc.AppendLine();
            doc.AppendLine("In `Runtime/Plugins/`:");
            doc.AppendLine();
            doc.AppendLine("- [ ] Copy .so files to `Android/libs/arm64-v8a/`");
            doc.AppendLine("- [ ] Copy .framework to `iOS/`");
            doc.AppendLine("- [ ] Copy .dll/.bundle to platform folders");
            doc.AppendLine("- [ ] Configure import settings in Unity");
            doc.AppendLine("- [ ] Create C# `[DllImport]` bindings");
            doc.AppendLine();
            doc.AppendLine("### 5. Configure Settings");
            doc.AppendLine();
            doc.AppendLine("Edit `Runtime/Scripts/" + className + "Settings.cs`:");
            doc.AppendLine();
            doc.AppendLine("- [ ] Add device-specific settings (e.g., IPD, brightness)");
            doc.AppendLine("- [ ] Remove unused default settings");
            doc.AppendLine("- [ ] Update tooltips and ranges");
            doc.AppendLine();
            doc.AppendLine("### 6. Update Loader");
            doc.AppendLine();
            doc.AppendLine("Edit `Runtime/Scripts/" + className + "Loader.cs`:");
            doc.AppendLine();
            doc.AppendLine("- [ ] Create your subsystems in `Initialize()`");
            doc.AppendLine("- [ ] Start subsystems in `Start()`");
            doc.AppendLine("- [ ] Stop and destroy in `Stop()` and `Deinitialize()`");
            doc.AppendLine();
            doc.AppendLine("### 7. Test");
            doc.AppendLine();
            doc.AppendLine("- [ ] Enable plugin in Project Settings → XR Plug-in Management");
            doc.AppendLine("- [ ] Build test application");
            doc.AppendLine("- [ ] Verify display activation");
            doc.AppendLine("- [ ] Verify head tracking");
            doc.AppendLine("- [ ] Check performance");
            doc.AppendLine();
            doc.AppendLine("## What's Included vs What You Must Implement");
            doc.AppendLine();
            doc.AppendLine("| Component | Status | Notes |");
            doc.AppendLine("|-----------|--------|-------|");
            doc.AppendLine("| Package structure | ✅ Complete | Ready to use |");
            doc.AppendLine("| XRLoader | ✅ Complete | Add subsystem creation |");
            doc.AppendLine("| XRSettings | ✅ Complete | Add device-specific settings |");
            doc.AppendLine("| Package Metadata | ✅ Complete | Update platforms if needed |");
            doc.AppendLine("| Display Subsystem | ⚠️ Example | Customize for your display |");
            doc.AppendLine("| Input Subsystem | ⚠️ Stub | Implement tracking logic |");
            doc.AppendLine("| Native Libraries | ❌ Empty | Add your device SDK |");
            doc.AppendLine();
            doc.AppendLine("## Common Pitfalls");
            doc.AppendLine();
            doc.AppendLine("1. **Display index wrong** - Check `Display.displays.Length` at runtime");
            doc.AppendLine("2. **Coordinate system mismatch** - Unity is left-handed Y-up");
            doc.AppendLine("3. **Missing native library** - Verify platform import settings");
            doc.AppendLine("4. **Subsystem not registered** - Ensure `RegisterDescriptor()` runs");
            doc.AppendLine("5. **Settings not loading** - Check the settings key matches");

            string docFolder = Path.Combine(packagePath, "Documentation~");
            File.WriteAllText(Path.Combine(docFolder, "GettingStarted.md"), doc.ToString());
        }

        private void CreateArchitectureDoc(string packagePath, string packageId)
        {
            string className = ToPascalCase(packageName);
            var doc = new System.Text.StringBuilder();
            doc.AppendLine("# XR Plugin Architecture");
            doc.AppendLine();
            doc.AppendLine("This document explains the Unity XR Plugin architecture used by this package.");
            doc.AppendLine();
            doc.AppendLine("## Overview");
            doc.AppendLine();
            doc.AppendLine("Unity's XR architecture is built on a **subsystem pattern**:");
            doc.AppendLine();
            doc.AppendLine("```");
            doc.AppendLine("┌─────────────────────────────────────────────────┐");
            doc.AppendLine("│             XR Plugin Management                │");
            doc.AppendLine("│         (Edit → Project Settings → XR)          │");
            doc.AppendLine("└─────────────────────┬───────────────────────────┘");
            doc.AppendLine("                      │");
            doc.AppendLine("                      ▼");
            doc.AppendLine("┌─────────────────────────────────────────────────┐");
            doc.AppendLine("│                  XRLoader                       │");
            doc.AppendLine("│     Manages lifecycle of all subsystems         │");
            doc.AppendLine("└──────┬──────────────┬───────────────────┬───────┘");
            doc.AppendLine("       │              │                   │");
            doc.AppendLine("       ▼              ▼                   ▼");
            doc.AppendLine("┌────────────┐ ┌────────────┐    ┌────────────┐");
            doc.AppendLine("│  Display   │ │   Input    │    │  Session   │");
            doc.AppendLine("│ Subsystem  │ │ Subsystem  │    │ Subsystem  │");
            doc.AppendLine("└────────────┘ └────────────┘    └────────────┘");
            doc.AppendLine("       │              │                   │");
            doc.AppendLine("       ▼              ▼                   ▼");
            doc.AppendLine("┌─────────────────────────────────────────────────┐");
            doc.AppendLine("│              Native Plugin / Device SDK         │");
            doc.AppendLine("└─────────────────────────────────────────────────┘");
            doc.AppendLine("```");
            doc.AppendLine();
            doc.AppendLine("## Component Responsibilities");
            doc.AppendLine();
            doc.AppendLine("### XRLoader (`" + className + "Loader.cs`)");
            doc.AppendLine();
            doc.AppendLine("The central coordinator that:");
            doc.AppendLine("- Creates subsystem instances during `Initialize()`");
            doc.AppendLine("- Starts subsystems when XR begins (`Start()`)");
            doc.AppendLine("- Stops subsystems when XR ends (`Stop()`)");
            doc.AppendLine("- Destroys subsystems during cleanup (`Deinitialize()`)");
            doc.AppendLine("- Provides access to settings");
            doc.AppendLine();
            doc.AppendLine("### XRSettings (`" + className + "Settings.cs`)");
            doc.AppendLine();
            doc.AppendLine("ScriptableObject containing configuration:");
            doc.AppendLine("- Serialized per-platform in EditorBuildSettings");
            doc.AppendLine("- Accessible from Project Settings UI");
            doc.AppendLine("- Loaded by XRLoader at runtime");
            doc.AppendLine();
            doc.AppendLine("### Display Subsystem");
            doc.AppendLine();
            doc.AppendLine("Handles rendering and display management:");
            doc.AppendLine("- Activates external displays");
            doc.AppendLine("- Configures resolution and refresh rate");
            doc.AppendLine("- Manages render passes (mono/stereo)");
            doc.AppendLine("- Provides render textures to Unity");
            doc.AppendLine();
            doc.AppendLine("### Input Subsystem");
            doc.AppendLine();
            doc.AppendLine("Handles tracking and input:");
            doc.AppendLine("- Head pose tracking (position/rotation)");
            doc.AppendLine("- Controller tracking");
            doc.AppendLine("- Button/axis input");
            doc.AppendLine("- Haptic feedback");
            doc.AppendLine();
            doc.AppendLine("### Package Metadata (`" + className + "PackageMetadata.cs`)");
            doc.AppendLine();
            doc.AppendLine("Registers the plugin with XR Plugin Management:");
            doc.AppendLine("- Defines package name and ID");
            doc.AppendLine("- Lists supported platforms");
            doc.AppendLine("- Links to loader and settings types");
            doc.AppendLine();
            doc.AppendLine("## Lifecycle Flow");
            doc.AppendLine();
            doc.AppendLine("```");
            doc.AppendLine("┌─────────────────┐");
            doc.AppendLine("│ Unity Starts    │");
            doc.AppendLine("└────────┬────────┘");
            doc.AppendLine("         │");
            doc.AppendLine("         ▼");
            doc.AppendLine("┌─────────────────┐     ┌─────────────────────────────┐");
            doc.AppendLine("│ XR Management   │────▶│ Finds enabled XRLoaders     │");
            doc.AppendLine("│ Initializes     │     │ from Project Settings       │");
            doc.AppendLine("└────────┬────────┘     └─────────────────────────────┘");
            doc.AppendLine("         │");
            doc.AppendLine("         ▼");
            doc.AppendLine("┌─────────────────┐     ┌─────────────────────────────┐");
            doc.AppendLine("│ Loader.         │────▶│ Load settings               │");
            doc.AppendLine("│ Initialize()    │     │ Create subsystem instances  │");
            doc.AppendLine("└────────┬────────┘     └─────────────────────────────┘");
            doc.AppendLine("         │");
            doc.AppendLine("         ▼");
            doc.AppendLine("┌─────────────────┐     ┌─────────────────────────────┐");
            doc.AppendLine("│ Loader.Start()  │────▶│ Start all subsystems        │");
            doc.AppendLine("│                 │     │ Begin XR session            │");
            doc.AppendLine("└────────┬────────┘     └─────────────────────────────┘");
            doc.AppendLine("         │");
            doc.AppendLine("         ▼");
            doc.AppendLine("┌─────────────────┐     ┌─────────────────────────────┐");
            doc.AppendLine("│ XR Running      │────▶│ Subsystems provide:         │");
            doc.AppendLine("│ (Game Loop)     │     │ - Render targets            │");
            doc.AppendLine("│                 │     │ - Pose data                 │");
            doc.AppendLine("│                 │     │ - Input events              │");
            doc.AppendLine("└────────┬────────┘     └─────────────────────────────┘");
            doc.AppendLine("         │");
            doc.AppendLine("         ▼");
            doc.AppendLine("┌─────────────────┐     ┌─────────────────────────────┐");
            doc.AppendLine("│ Loader.Stop()   │────▶│ Stop all subsystems         │");
            doc.AppendLine("│                 │     │ Pause XR session            │");
            doc.AppendLine("└────────┬────────┘     └─────────────────────────────┘");
            doc.AppendLine("         │");
            doc.AppendLine("         ▼");
            doc.AppendLine("┌─────────────────┐     ┌─────────────────────────────┐");
            doc.AppendLine("│ Loader.         │────▶│ Destroy subsystem instances │");
            doc.AppendLine("│ Deinitialize()  │     │ Release native resources    │");
            doc.AppendLine("└─────────────────┘     └─────────────────────────────┘");
            doc.AppendLine("```");
            doc.AppendLine();
            doc.AppendLine("## Subsystem Registration");
            doc.AppendLine();
            doc.AppendLine("Subsystems must register their descriptors at startup:");
            doc.AppendLine();
            doc.AppendLine("```csharp");
            doc.AppendLine("[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]");
            doc.AppendLine("public static void RegisterDescriptor()");
            doc.AppendLine("{");
            doc.AppendLine("    // This runs BEFORE any MonoBehaviour Awake/Start");
            doc.AppendLine("    XRDisplaySubsystemDescriptor.RegisterDescriptor(new XRDisplaySubsystemDescriptor.Cinfo");
            doc.AppendLine("    {");
            doc.AppendLine("        id = \"MyDisplay\",");
            doc.AppendLine("        providerType = typeof(MyDisplayProvider),");
            doc.AppendLine("        subsystemTypeOverride = typeof(MyDisplaySubsystem),");
            doc.AppendLine("    });");
            doc.AppendLine("}");
            doc.AppendLine("```");
            doc.AppendLine();
            doc.AppendLine("Then in your loader:");
            doc.AppendLine();
            doc.AppendLine("```csharp");
            doc.AppendLine("public override bool Initialize()");
            doc.AppendLine("{");
            doc.AppendLine("    // Find the registered descriptor and create an instance");
            doc.AppendLine("    CreateSubsystem<XRDisplaySubsystemDescriptor, XRDisplaySubsystem>(");
            doc.AppendLine("        displaySubsystemDescriptors, \"MyDisplay\");");
            doc.AppendLine("    return true;");
            doc.AppendLine("}");
            doc.AppendLine("```");
            doc.AppendLine();
            doc.AppendLine("## Resources");
            doc.AppendLine();
            doc.AppendLine("- [Unity XR Plugin Framework](https://docs.unity3d.com/Manual/xr-sdk.html)");
            doc.AppendLine("- [XR Subsystems Manual](https://docs.unity3d.com/Manual/xr-subsystems.html)");
            doc.AppendLine("- [XR Plugin Management](https://docs.unity3d.com/Packages/com.unity.xr.management@4.0/manual/index.html)");
            doc.AppendLine("- [Sample XR Plugin Implementation](https://github.com/Unity-Technologies/xr-sdk)");

            string docFolder = Path.Combine(packagePath, "Documentation~");
            File.WriteAllText(Path.Combine(docFolder, "Architecture.md"), doc.ToString());
        }

        private void CreateEditorWindowTemplate(string folder, string packageId)
        {
            string className = ToPascalCase(packageName) + "Window";
            var code = new System.Text.StringBuilder();
            code.AppendLine("using UnityEditor;");
            code.AppendLine("using UnityEngine;");
            code.AppendLine();
            code.AppendLine($"namespace {GetNamespace(packageId)}.Editor");
            code.AppendLine("{");
            code.AppendLine($"    public class {className} : EditorWindow");
            code.AppendLine("    {");
            code.AppendLine($"        [MenuItem(\"Tools/{displayName}\")]");
            code.AppendLine("        public static void ShowWindow()");
            code.AppendLine("        {");
            code.AppendLine($"            GetWindow<{className}>(\"{displayName}\");");
            code.AppendLine("        }");
            code.AppendLine();
            code.AppendLine("        private void OnGUI()");
            code.AppendLine("        {");
            code.AppendLine("            GUILayout.Label(\"" + displayName + "\", EditorStyles.boldLabel);");
            code.AppendLine("            ");
            code.AppendLine("            // TODO: Add your editor GUI here");
            code.AppendLine("            ");
            code.AppendLine("            if (GUILayout.Button(\"Click Me\"))");
            code.AppendLine("            {");
            code.AppendLine("                Debug.Log(\"Button clicked!\");");
            code.AppendLine("            }");
            code.AppendLine("        }");
            code.AppendLine("    }");
            code.AppendLine("}");
            
            File.WriteAllText(Path.Combine(folder, $"{className}.cs"), code.ToString());
        }

        private void CreatePlatformBuildProcessorTemplate(string folder, string packageId)
        {
            string className = ToPascalCase(packageName) + "BuildProcessor";
            var code = new System.Text.StringBuilder();
            code.AppendLine("using UnityEditor;");
            code.AppendLine("using UnityEditor.Build;");
            code.AppendLine("using UnityEditor.Build.Reporting;");
            code.AppendLine();
            code.AppendLine($"namespace {GetNamespace(packageId)}.Editor");
            code.AppendLine("{");
            code.AppendLine($"    public class {className} : IPreprocessBuildWithReport");
            code.AppendLine("    {");
            code.AppendLine("        public int callbackOrder => 0;");
            code.AppendLine();
            code.AppendLine("        public void OnPreprocessBuild(BuildReport report)");
            code.AppendLine("        {");
            code.AppendLine("            if (report.summary.platform == BuildTarget.Android)");
            code.AppendLine("            {");
            code.AppendLine("                // TODO: Configure Android-specific settings");
            code.AppendLine("            }");
            code.AppendLine("            else if (report.summary.platform == BuildTarget.iOS)");
            code.AppendLine("            {");
            code.AppendLine("                // TODO: Configure iOS-specific settings");
            code.AppendLine("            }");
            code.AppendLine("        }");
            code.AppendLine("    }");
            code.AppendLine("}");
            
            File.WriteAllText(Path.Combine(folder, $"{className}.cs"), code.ToString());
        }

        private void CreateAndroidManifestTemplate(string folder)
        {
            var manifest = new System.Text.StringBuilder();
            manifest.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            manifest.AppendLine("<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\"");
            manifest.AppendLine("    package=\"com.unity3d.player\"");
            manifest.AppendLine("    android:versionCode=\"1\"");
            manifest.AppendLine("    android:versionName=\"1.0\">");
            manifest.AppendLine();
            manifest.AppendLine("    <!-- TODO: Add your required permissions -->");
            manifest.AppendLine("    <!-- Example: <uses-permission android:name=\"android.permission.CAMERA\" /> -->");
            manifest.AppendLine();
            manifest.AppendLine("    <application>");
            manifest.AppendLine("        <!-- TODO: Add your application configuration -->");
            manifest.AppendLine("    </application>");
            manifest.AppendLine("</manifest>");
            
            File.WriteAllText(Path.Combine(folder, "AndroidManifest.xml"), manifest.ToString());
        }

        private void CreateReadme(string packagePath)
        {
            var readme = new System.Text.StringBuilder();
            readme.AppendLine($"# {displayName}");
            readme.AppendLine();
            readme.AppendLine(description);
            readme.AppendLine();
            
            // Template-specific installation notes
            if (selectedTemplate == PackageTemplate.XRPluginProvider)
            {
                readme.AppendLine("## Requirements");
                readme.AppendLine();
                readme.AppendLine("- Unity 6000.2 or later");
                readme.AppendLine("- XR Plugin Management (installed automatically)");
                readme.AppendLine();
            }
            
            readme.AppendLine("## Installation");
            readme.AppendLine();
            readme.AppendLine("Add this package via Package Manager:");
            readme.AppendLine();
            readme.AppendLine("1. Open Package Manager (Window → Package Manager)");
            readme.AppendLine("2. Click the + button");
            readme.AppendLine("3. Select 'Add package from git URL...'");
            readme.AppendLine("4. Enter the repository URL");
            readme.AppendLine();
            
            if (selectedTemplate == PackageTemplate.XRPluginProvider)
            {
                readme.AppendLine("## Setup");
                readme.AppendLine();
                readme.AppendLine("1. Go to Edit → Project Settings → XR Plug-in Management");
                readme.AppendLine($"2. Enable {displayName} for your target platform(s)");
                readme.AppendLine("3. Configure settings under XR Plug-in Management → " + displayName);
                readme.AppendLine();
            }
            
            readme.AppendLine("## Usage");
            readme.AppendLine();
            readme.AppendLine("TODO: Add usage instructions");
            readme.AppendLine();
            readme.AppendLine("## License");
            readme.AppendLine();
            readme.AppendLine("See LICENSE.md");
            
            File.WriteAllText(Path.Combine(packagePath, "README.md"), readme.ToString());
        }

        private void CreateChangelog(string packagePath)
        {
            var changelog = new System.Text.StringBuilder();
            changelog.AppendLine("# Changelog");
            changelog.AppendLine();
            changelog.AppendLine("All notable changes to this project will be documented in this file.");
            changelog.AppendLine();
            changelog.AppendLine("The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),");
            changelog.AppendLine("and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).");
            changelog.AppendLine();
            changelog.AppendLine("## [0.1.0] - " + DateTime.Now.ToString("yyyy-MM-dd"));
            changelog.AppendLine();
            changelog.AppendLine("### Added");
            changelog.AppendLine();
            changelog.AppendLine("- Initial release");
            changelog.AppendLine($"- Package template: {selectedTemplate}");
            
            File.WriteAllText(Path.Combine(packagePath, "CHANGELOG.md"), changelog.ToString());
        }

        private void CreateLicense(string packagePath)
        {
            var license = new System.Text.StringBuilder();
            license.AppendLine("MIT License");
            license.AppendLine();
            license.AppendLine($"Copyright (c) {DateTime.Now.Year} {(string.IsNullOrWhiteSpace(author) ? companyName : author)}");
            license.AppendLine();
            license.AppendLine("Permission is hereby granted, free of charge, to any person obtaining a copy");
            license.AppendLine("of this software and associated documentation files (the \"Software\"), to deal");
            license.AppendLine("in the Software without restriction, including without limitation the rights");
            license.AppendLine("to use, copy, modify, merge, publish, distribute, sublicense, and/or sell");
            license.AppendLine("copies of the Software, and to permit persons to whom the Software is");
            license.AppendLine("furnished to do so, subject to the following conditions:");
            license.AppendLine();
            license.AppendLine("The above copyright notice and this permission notice shall be included in all");
            license.AppendLine("copies or substantial portions of the Software.");
            license.AppendLine();
            license.AppendLine("THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR");
            license.AppendLine("IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,");
            license.AppendLine("FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE");
            license.AppendLine("AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER");
            license.AppendLine("LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,");
            license.AppendLine("OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE");
            license.AppendLine("SOFTWARE.");
            
            File.WriteAllText(Path.Combine(packagePath, "LICENSE.md"), license.ToString());
        }

        private void CreateDocumentation(string packagePath)
        {
            var doc = new System.Text.StringBuilder();
            doc.AppendLine($"# {displayName}");
            doc.AppendLine();
            doc.AppendLine("## Overview");
            doc.AppendLine();
            doc.AppendLine(description);
            doc.AppendLine();
            
            if (selectedTemplate == PackageTemplate.XRPluginProvider)
            {
                doc.AppendLine("## Architecture");
                doc.AppendLine();
                doc.AppendLine("This XR plugin provider implements the Unity XR Plugin Architecture.");
                doc.AppendLine();
                doc.AppendLine("### Components");
                doc.AppendLine();
                doc.AppendLine("- **XRLoader**: Manages XR subsystem lifecycle");
                doc.AppendLine("- **XRSettings**: Configuration for the XR provider");
                doc.AppendLine("- **Subsystems**: Display and input subsystem implementations");
                doc.AppendLine();
            }
            
            doc.AppendLine("## Features");
            doc.AppendLine();
            doc.AppendLine("TODO: List features");
            doc.AppendLine();
            doc.AppendLine("## API Reference");
            doc.AppendLine();
            doc.AppendLine("TODO: Add API documentation");
            
            string docFolder = Path.Combine(packagePath, "Documentation~");
            string packageId = $"com.{companyName}.{packageName}";
            File.WriteAllText(Path.Combine(docFolder, $"{packageId}.md"), doc.ToString());
        }

        // Helper methods
        
        private string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            var parts = input.Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new System.Text.StringBuilder();
            
            foreach (var part in parts)
            {
                if (part.Length > 0)
                {
                    result.Append(char.ToUpper(part[0]));
                    if (part.Length > 1)
                    {
                        result.Append(part.Substring(1).ToLower());
                    }
                }
            }
            
            return result.ToString();
        }

        private string GetNamespace(string packageId)
        {
            // Convert com.company.package-name to Company.PackageName
            var parts = packageId.Split('.');
            var result = new System.Text.StringBuilder();

            for (int i = 1; i < parts.Length; i++) // Skip 'com'
            {
                if (i > 1) result.Append(".");
                result.Append(ToPascalCase(parts[i]));
            }

            return result.ToString();
        }

        private string SanitizeForPackageId(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            // Convert to lowercase, replace spaces with hyphens, remove invalid characters
            var result = new System.Text.StringBuilder();
            foreach (char c in input.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c))
                    result.Append(c);
                else if (c == ' ' || c == '_' || c == '-')
                    result.Append('-');
            }

            // Remove consecutive hyphens and trim hyphens from ends
            string sanitized = result.ToString();
            while (sanitized.Contains("--"))
                sanitized = sanitized.Replace("--", "-");

            return sanitized.Trim('-');
        }

        private string GetDefaultUnityVersion()
        {
            // Parse Application.unityVersion (e.g., "6000.2.1f1") to major.minor (e.g., "6000.2")
            string version = Application.unityVersion;
            var parts = version.Split('.');

            if (parts.Length >= 2)
            {
                // Second part may have suffix like "2f1", extract just the number
                string minorPart = parts[1];
                var minorDigits = new System.Text.StringBuilder();
                foreach (char c in minorPart)
                {
                    if (char.IsDigit(c))
                        minorDigits.Append(c);
                    else
                        break;
                }

                return $"{parts[0]}.{minorDigits}";
            }

            return "6000.0"; // Fallback
        }
    }
}
