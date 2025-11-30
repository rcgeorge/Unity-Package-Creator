# Unity Package Creator

A Unity Editor tool for quickly creating UPM (Unity Package Manager) packages with proper structure and configuration.

## Features

- ✅ Creates proper UPM package structure
- ✅ Generates package.json with all required fields
- ✅ Creates assembly definition files (.asmdef)
- ✅ Generates README, CHANGELOG, and LICENSE templates
- ✅ Sets up Runtime, Editor, and Tests folders
- ✅ User-friendly GUI in Unity Editor
- ✅ Accessible via **Tools → Create UPM Package** menu

## Installation

### Via Git URL (Recommended)

1. Open Unity Editor
2. Open Package Manager (Window → Package Manager)
3. Click the **+** button in the top-left
4. Select **"Add package from git URL..."**
5. Paste: `https://github.com/rcgeorge/Unity-Package-Creator.git`
6. Click **Add**

### Via manifest.json

Add this to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.instemic.package-creator": "https://github.com/rcgeorge/Unity-Package-Creator.git"
  }
}
```

## Usage

1. In Unity, go to **Tools → Create UPM Package**
2. Fill in the package details:
   - **Company Name**: Your company/username (e.g., "mycompany")
   - **Package Name**: Your package identifier (e.g., "awesome-tool")
   - **Display Name**: Human-readable name (e.g., "Awesome Tool")
   - **Description**: Brief description of your package
   - **Unity Version**: Minimum Unity version (e.g., "6000.2")
3. Choose the output location
4. Click **Create Package**

The tool will create a complete package structure ready to use!

## Generated Structure

```
com.yourcompany.yourpackage/
├── package.json
├── README.md
├── CHANGELOG.md
├── LICENSE.md
├── Editor/
│   ├── com.yourcompany.yourpackage.Editor.asmdef
│   └── Scripts/
├── Runtime/
│   ├── com.yourcompany.yourpackage.asmdef
│   └── Scripts/
├── Tests/
│   ├── Editor/
│   │   └── com.yourcompany.yourpackage.Editor.Tests.asmdef
│   └── Runtime/
│       └── com.yourcompany.yourpackage.Tests.asmdef
├── Samples~/
└── Documentation~/
```

## Requirements

- Unity 6000.2 or later

## License

MIT License - see [LICENSE](LICENSE) for details

## Contributing

Contributions are welcome! Feel free to open issues or submit pull requests.

## Support

For issues or questions, please open an issue on the [GitHub repository](https://github.com/rcgeorge/Unity-Package-Creator/issues).