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
        private string companyName = "mycompany";
        private string packageName = "my-package";
        private string displayName = "My Package";
        private string description = "Description of my package";
        private string unityVersion = "6000.2";
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
            var code = new System.Text.StringBuilder();
            code.AppendLine("using System.Collections.Generic;");
            code.AppendLine("using UnityEngine;");
            code.AppendLine("using UnityEngine.XR.Management;");
            code.AppendLine();
            code.AppendLine($"namespace {GetNamespace(packageId)}");
            code.AppendLine("{");
            code.AppendLine($"    public class {className} : XRLoaderHelper");
            code.AppendLine("    {");
            code.AppendLine("        private static List<XRDisplaySubsystemDescriptor> displaySubsystemDescriptors = new List<XRDisplaySubsystemDescriptor>();");
            code.AppendLine("        private static List<XRInputSubsystemDescriptor> inputSubsystemDescriptors = new List<XRInputSubsystemDescriptor>();");
            code.AppendLine();
            code.AppendLine("        public override bool Initialize()");
            code.AppendLine("        {");
            code.AppendLine("            // TODO: Initialize your XR subsystems");
            code.AppendLine("            Debug.Log(\"" + className + ": Initializing XR subsystems\");");
            code.AppendLine("            ");
            code.AppendLine("            // Example: Create and start display subsystem");
            code.AppendLine("            // CreateSubsystem<XRDisplaySubsystemDescriptor, XRDisplaySubsystem>(displaySubsystemDescriptors, \"YourDisplaySubsystemId\");");
            code.AppendLine("            ");
            code.AppendLine("            return true;");
            code.AppendLine("        }");
            code.AppendLine();
            code.AppendLine("        public override bool Start()");
            code.AppendLine("        {");
            code.AppendLine("            // TODO: Start your XR subsystems");
            code.AppendLine("            Debug.Log(\"" + className + ": Starting XR subsystems\");");
            code.AppendLine("            ");
            code.AppendLine("            // Example: Start display subsystem");
            code.AppendLine("            // StartSubsystem<XRDisplaySubsystem>();");
            code.AppendLine("            ");
            code.AppendLine("            return true;");
            code.AppendLine("        }");
            code.AppendLine();
            code.AppendLine("        public override bool Stop()");
            code.AppendLine("        {");
            code.AppendLine("            // TODO: Stop your XR subsystems");
            code.AppendLine("            Debug.Log(\"" + className + ": Stopping XR subsystems\");");
            code.AppendLine("            ");
            code.AppendLine("            // Example: Stop display subsystem");
            code.AppendLine("            // StopSubsystem<XRDisplaySubsystem>();");
            code.AppendLine("            ");
            code.AppendLine("            return true;");
            code.AppendLine("        }");
            code.AppendLine();
            code.AppendLine("        public override bool Deinitialize()");
            code.AppendLine("        {");
            code.AppendLine("            // TODO: Cleanup and destroy your XR subsystems");
            code.AppendLine("            Debug.Log(\"" + className + ": Deinitializing XR subsystems\");");
            code.AppendLine("            ");
            code.AppendLine("            // Example: Destroy display subsystem");
            code.AppendLine("            // DestroySubsystem<XRDisplaySubsystem>();");
            code.AppendLine("            ");
            code.AppendLine("            return true;");
            code.AppendLine("        }");
            code.AppendLine("    }");
            code.AppendLine("}");
            
            File.WriteAllText(Path.Combine(folder, $"{className}.cs"), code.ToString());
        }

        private void CreateXRSettingsTemplate(string folder, string packageId)
        {
            string className = ToPascalCase(packageName) + "Settings";
            var code = new System.Text.StringBuilder();
            code.AppendLine("using UnityEngine;");
            code.AppendLine();
            code.AppendLine($"namespace {GetNamespace(packageId)}");
            code.AppendLine("{");
            code.AppendLine($"    [CreateAssetMenu(fileName = \"{className}\", menuName = \"XR/{displayName}/Settings\")]");
            code.AppendLine($"    public class {className} : ScriptableObject");
            code.AppendLine("    {");
            code.AppendLine("        [SerializeField]");
            code.AppendLine("        [Tooltip(\"Enable verbose logging\")]");
            code.AppendLine("        private bool enableLogging = false;");
            code.AppendLine();
            code.AppendLine("        public bool EnableLogging => enableLogging;");
            code.AppendLine();
            code.AppendLine("        // TODO: Add your XR provider settings here");
            code.AppendLine("        ");
            code.AppendLine("        // Example:");
            code.AppendLine("        // [SerializeField]");
            code.AppendLine("        // private int refreshRate = 90;");
            code.AppendLine("        // ");
            code.AppendLine("        // public int RefreshRate => refreshRate;");
            code.AppendLine("    }");
            code.AppendLine("}");
            
            File.WriteAllText(Path.Combine(folder, $"{className}.cs"), code.ToString());
        }

        private void CreateXRPackageMetadataTemplate(string folder, string packageId)
        {
            string className = ToPascalCase(packageName) + "PackageMetadata";
            string loaderClassName = ToPascalCase(packageName) + "Loader";
            var code = new System.Text.StringBuilder();
            code.AppendLine("using System.Collections.Generic;");
            code.AppendLine("using UnityEditor;");
            code.AppendLine("using UnityEditor.XR.Management.Metadata;");
            code.AppendLine();
            code.AppendLine($"namespace {GetNamespace(packageId)}.Editor");
            code.AppendLine("{");
            code.AppendLine($"    public class {className} : IXRPackage");
            code.AppendLine("    {");
            code.AppendLine("        private class LoaderMetadata : IXRLoaderMetadata");
            code.AppendLine("        {");
            code.AppendLine($"            public string loaderName {{ get; set; }}");
            code.AppendLine($"            public string loaderType {{ get; set; }}");
            code.AppendLine($"            public List<BuildTargetGroup> supportedBuildTargets {{ get; set; }}");
            code.AppendLine("        }");
            code.AppendLine();
            code.AppendLine("        private class PackageMetadata : IXRPackageMetadata");
            code.AppendLine("        {");
            code.AppendLine($"            public string packageName {{ get; set; }}");
            code.AppendLine($"            public string packageId {{ get; set; }}");
            code.AppendLine($"            public string settingsType {{ get; set; }}");
            code.AppendLine($"            public List<IXRLoaderMetadata> loaderMetadata {{ get; set; }}");
            code.AppendLine("        }");
            code.AppendLine();
            code.AppendLine("        private static IXRPackageMetadata s_Metadata = new PackageMetadata()");
            code.AppendLine("        {");
            code.AppendLine($"            packageName = \"{displayName}\",");
            code.AppendLine($"            packageId = \"{packageId}\",");
            code.AppendLine($"            settingsType = typeof({ToPascalCase(packageName)}Settings).FullName,");
            code.AppendLine($"            loaderMetadata = new List<IXRLoaderMetadata>()");
            code.AppendLine("            {");
            code.AppendLine("                new LoaderMetadata()");
            code.AppendLine("                {");
            code.AppendLine($"                    loaderName = \"{displayName}\",");
            code.AppendLine($"                    loaderType = typeof({loaderClassName}).FullName,");
            code.AppendLine("                    supportedBuildTargets = new List<BuildTargetGroup>()");
            code.AppendLine("                    {");
            code.AppendLine("                        BuildTargetGroup.Standalone,");
            code.AppendLine("                        BuildTargetGroup.Android");
            code.AppendLine("                        // TODO: Add your supported platforms");
            code.AppendLine("                    }");
            code.AppendLine("                }");
            code.AppendLine("            }");
            code.AppendLine("        };");
            code.AppendLine();
            code.AppendLine("        public IXRPackageMetadata metadata => s_Metadata;");
            code.AppendLine();
            code.AppendLine("        public bool PopulateNewSettingsInstance(ScriptableObject obj)");
            code.AppendLine("        {");
            code.AppendLine("            // TODO: Initialize default settings if needed");
            code.AppendLine("            return true;");
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
    }
}
