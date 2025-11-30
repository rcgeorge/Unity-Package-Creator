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