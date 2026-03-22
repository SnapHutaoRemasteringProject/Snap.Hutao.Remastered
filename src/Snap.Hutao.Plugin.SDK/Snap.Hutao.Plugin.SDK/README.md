# Snap Hutao Plugin SDK

This SDK provides tools and templates for creating plugins for Snap Hutao.

## Features

- Automatic .hutao file generation during build
- Template project structure
- Easy integration with existing Snap Hutao plugin system

## Getting Started

### 1. Create a new plugin project

Create a new .NET project and add the SDK package reference:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-windows10.0.26100.0</TargetFramework>
    <Platforms>x64</Platforms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <GenerateHutaoPlugin>true</GenerateHutaoPlugin>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Snap.Hutao.Plugin.SDK" Version="1.0.0" />
  </ItemGroup>

</Project>
```

### 2. Create manifest.json

Create a `manifest.json` file in your project root:

```json
{
  "name": "My Plugin",
  "id": "MyPlugin",
  "version": {
    "major": 1,
    "minor": 0,
    "patch": 0
  },
  "author": ["Your Name"],
  "description": "A sample plugin for Snap Hutao"
}
```

### 3. Create your plugin class

Create a plugin class that inherits from `HutaoPlugin`:

```csharp
using Snap.Hutao.Remastered.Model.Plugin;
using Snap.Hutao.Remastered.Model.Plugin.Annotation;

namespace MyPlugin;

[PluginMain]
public class Plugin : HutaoPlugin
{
    public override void OnInstall()
    {
        // Called when plugin is installed
    }

    public override void OnLoad(PluginContext context)
    {
        // Called when plugin is loaded
    }

    public override void OnEnable()
    {
        // Called when plugin is enabled
    }

    public override void OnDisable()
    {
        // Called when plugin is disabled
    }

    public override void OnUninstall()
    {
        // Called when plugin is uninstalled
    }
}
```

### 4. Optional files

- `icon.png` - Plugin icon (64x64 recommended)
- `inject/` directory - Files to inject into the game

### 5. Build your plugin

When you build your project, a `.hutao` file will be automatically generated in the output directory.

## Advanced Configuration

### Disable automatic .hutao generation

Set `GenerateHutaoPlugin` to `false` in your project file:

```xml
<PropertyGroup>
  <GenerateHutaoPlugin>false</GenerateHutaoPlugin>
</PropertyGroup>
```

### Custom output path

Override the default output path:

```xml
<PropertyGroup>
  <HutaoPluginOutputPath>$(OutputPath)CustomName.hutao</HutaoPluginOutputPath>
</PropertyGroup>
```

## Plugin Structure

A `.hutao` file is a ZIP archive containing:

- `manifest.json` - Plugin metadata (required)
- `{PluginId}.dll` - Main plugin assembly (required)
- `icon.png` - Plugin icon (optional)
- `inject/` - Files to inject into the game (optional)
- Other referenced DLLs (automatically included)

## Best Practices

1. Use a unique plugin ID (reverse domain notation recommended, e.g., `com.example.myplugin`)
2. Test your plugin thoroughly before distribution
3. Follow semantic versioning for plugin versions
4. Include clear documentation for your plugin's functionality

## Troubleshooting

### "Plugin manifest.json not found"
Ensure you have a `manifest.json` file in your project root.

### ".hutao file not generated"
Check that `GenerateHutaoPlugin` is set to `true` (default).

### "Plugin fails to load in Snap Hutao"
Verify that:
- Your plugin class has the `[PluginMain]` attribute
- Your plugin inherits from `HutaoPlugin`
- All required dependencies are included
