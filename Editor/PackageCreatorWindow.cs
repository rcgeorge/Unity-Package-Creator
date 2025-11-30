using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Instemic.PackageCreator.Editor
{
    public class PackageCreatorWindow : EditorWindow
    {
        private string companyName = "mycompany";
        private string packageName = "my-package";
        private string displayName = "My Package";
        private string description = "Description of my package";
        private string unityVersion = "6000.2";
        private string author = "";
        private string outputPath = "";
        
        private Vector2 scrollPosition;

        [MenuItem("Tools/Create UPM Package")]
        public static void ShowWindow()
        {
            var window = GetWindow<PackageCreatorWindow>("Package Creator");
            window.minSize = new Vector2(450, 500);
            window.Show();
        }

        private void OnEnable()
        {
            // Set default output path to parent of Assets folder
            outputPath = Path.GetDirectoryName(Application.dataPath);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Unity Package Creator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Create a new UPM package with proper structure and configuration.", MessageType.Info);
            
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
                EditorUtility.DisplayDialog("Success", 
                    $"Package created successfully at:\n{packagePath}\n\nYou can now move this to your Unity project's Packages folder or publish it to GitHub.", 
                    "OK");
                
                // Open the folder
                EditorUtility.RevealInFinder(packagePath);
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
            
            // Create directories
            Directory.CreateDirectory(packagePath);
            Directory.CreateDirectory(Path.Combine(packagePath, "Editor", "Scripts"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Runtime", "Scripts"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Tests", "Editor"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Tests", "Runtime"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Samples~"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Documentation~"));
            
            // Create package.json
            CreatePackageJson(packagePath, packageId);
            
            // Create assembly definition files
            CreateAsmdef(Path.Combine(packagePath, "Runtime"), $"{packageId}", packageId, false);
            CreateAsmdef(Path.Combine(packagePath, "Editor"), $"{packageId}.Editor", $"{packageId}.Editor", true);
            CreateTestAsmdef(Path.Combine(packagePath, "Tests", "Runtime"), $"{packageId}.Tests", packageId, false);
            CreateTestAsmdef(Path.Combine(packagePath, "Tests", "Editor"), $"{packageId}.Editor.Tests", $"{packageId}.Editor", true);
            
            // Create documentation files
            CreateReadme(packagePath);
            CreateChangelog(packagePath);
            CreateLicense(packagePath);
            CreateDocumentation(packagePath);
            
            Debug.Log($"Package created successfully: {packageId}");
        }

        private void CreatePackageJson(string packagePath, string packageId)
        {
            var json = new System.Text.StringBuilder();
            json.AppendLine("{");
            json.AppendLine($"  \"name\": \"{packageId}\",");
            json.AppendLine($"  \"version\": \"0.1.0\",");
            json.AppendLine($"  \"displayName\": \"{displayName}\",");
            json.AppendLine($"  \"description\": \"{description}\",");
            json.AppendLine($"  \"unity\": \"{unityVersion}\",");
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

        private void CreateAsmdef(string folder, string asmdefName, string rootNamespace, bool editorOnly)
        {
            var asmdef = new System.Text.StringBuilder();
            asmdef.AppendLine("{");
            asmdef.AppendLine($"    \"name\": \"{asmdefName}\",");
            asmdef.AppendLine($"    \"rootNamespace\": \"{rootNamespace}\",");
            asmdef.AppendLine($"    \"references\": [],");
            
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

        private void CreateReadme(string packagePath)
        {
            var readme = new System.Text.StringBuilder();
            readme.AppendLine($"# {displayName}");
            readme.AppendLine();
            readme.AppendLine(description);
            readme.AppendLine();
            readme.AppendLine("## Installation");
            readme.AppendLine();
            readme.AppendLine("Add this package via Package Manager:");
            readme.AppendLine();
            readme.AppendLine("1. Open Package Manager (Window → Package Manager)");
            readme.AppendLine("2. Click the + button");
            readme.AppendLine("3. Select 'Add package from git URL...'");
            readme.AppendLine("4. Enter the repository URL");
            readme.AppendLine();
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
    }
}