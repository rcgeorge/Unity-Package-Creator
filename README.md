# Unity Package Creator

A Unity Editor tool for quickly creating UPM (Unity Package Manager) packages with proper structure and configuration.

## Features

- ✅ Creates proper UPM package structure
- ✅ Multiple package templates (Universal, XR Plugin Provider, Editor-Only, Platform-Specific)
- ✅ Generates package.json with all required fields
- ✅ Creates assembly definition files (.asmdef)
- ✅ Generates README, CHANGELOG, and LICENSE templates
- ✅ Sets up Runtime, Editor, and Tests folders
- ✅ **XR Plugin Provider** template with production-ready components
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
2. Select a package template:
   - **Universal**: Standard package with Runtime, Editor, and Tests
   - **XR Plugin Provider**: Complete XR device integration template
   - **Editor-Only**: Tools and extensions (no Runtime code)
   - **Platform-Specific**: Android/iOS specific packages
3. Fill in the package details:
   - **Company Name**: Your company/username (e.g., "mycompany")
   - **Package Name**: Your package identifier (e.g., "awesome-tool")
   - **Display Name**: Human-readable name (e.g., "Awesome Tool")
   - **Description**: Brief description of your package
   - **Unity Version**: Minimum Unity version (e.g., "6000.2")
4. Choose the output location
5. Click **Create Package**

The tool will create a complete package structure ready to use!

## XR Plugin Provider Template

The **XR Plugin Provider** template includes production-ready components based on universal XR patterns:

### Core Components
- **XRPermissionManager**: Cross-platform permission handling (Camera, Sensors, XRService)
- **XREventSystem**: Centralized event management with singleton pattern
- **XRServiceConnection**: Device system service binding (Android AIDL, iOS XPC)
- **XRMemoryBridge**: Shared memory management for high-performance sensor data
- **XRLifecycleManager**: Proper init/pause/resume/shutdown handling

### Utilities
- **XRCoordinateConverter**: Universal coordinate system conversion (Unity ↔ OpenGL ↔ DirectX)
- **XRFeatureDetector**: Device capability detection
- **XRTrackingModeManager**: 3DOF/6DOF mode switching
- **XRCalibrationManager**: IPD, height, tracking reset
- **XRPerformanceMonitor**: FPS and frame drop monitoring

### Data Structures
- **XRIMUData**: Raw IMU sensor data
- **XRHandPose**: Hand tracking data structure

All components include clear TODOs for device-specific customization.

## Generated Structure

### Universal Package
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

### XR Plugin Provider Package
```
com.yourcompany.xrdevice/
├── package.json (with XR dependencies)
├── README.md
├── Runtime/
│   ├── Core/
│   │   ├── XRPermissionManager.cs
│   │   ├── XREventSystem.cs
│   │   ├── XRServiceConnection.cs
│   │   ├── XRMemoryBridge.cs
│   │   └── XRLifecycleManager.cs
│   ├── Utilities/
│   │   ├── XRCoordinateConverter.cs
│   │   ├── XRFeatureDetector.cs
│   │   ├── XRTrackingModeManager.cs
│   │   ├── XRCalibrationManager.cs
│   │   └── XRPerformanceMonitor.cs
│   ├── Data/
│   │   └── XRDataStructures.cs
│   ├── Subsystems/
│   │   ├── XRDeviceDisplay.cs
│   │   └── XRDeviceTracking.cs
│   ├── Scripts/
│   │   ├── XRDeviceLoader.cs
│   │   └── XRDeviceSettings.cs
│   └── Plugins/
│       ├── Android/
│       └── iOS/
├── Editor/
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