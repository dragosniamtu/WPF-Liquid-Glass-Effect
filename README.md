# WPF Liquid Glass — external GLSL with XamlTemplates.FX

A small WPF demo of a tutorial-inspired glass surface over the live desktop.

## Demo video

The updated demo applies the external Liquid Glass shader over a playing video,
with live parameter controls on the right. It uses the selected-window recording
mode included in `XamlTemplates.FX.Wpf 1.0.0-preview.5`.

https://github.com/user-attachments/assets/f661b055-abdd-49b5-91ae-9f1d5c20b685

[Open or download the demo video](docs/XamlTemplates.Fx-Library.mp4)
(18.5 seconds, 1080p).

## Updated for the NuGet package

The demo consumes [XamlTemplates.FX.Wpf 1.0.0-preview.5](https://www.nuget.org/packages/XamlTemplates.FX.Wpf/1.0.0-preview.5)
directly from **NuGet.org** through a `PackageReference`. No private SDK checkout,
project reference, local package feed, or native prebuild is needed. The package
provides the native renderer, shader compiler, and compiled pipeline shaders;
the demo supplies its own external GLSL file. This version adds selected-window
capture so the complete glass window can appear in screen recordings.

`NuGet.Config` selects NuGet.org only. The project no longer imports the earlier
ignored `DemoApp/LocalFxPackage.props` override, so an old local candidate cannot
replace the public package.

It loads [LiquidGlassSample.glsl](DemoApp/Shaders/LiquidGlassSample.glsl) as an
external application asset, **not** the package's built-in Liquid Glass preset.
The built-in preset has different optics; this sample preserves the historical
tutorial-inspired radial distortion and Gaussian sampling.

The original shader direction comes from AmirHossein Aghajari's
[Liquid Glass: iOS Effect Explanation](https://medium.com/@aghajari/liquid-glass-ios-effect-explanation-dabadd6414ae),
published November 24, 2025. We adapted the sample for the NuGet API: a full-window
lens instead of the original fixed 120 × 80 mouse-centered lens, host-provided
captured pixels, configurable uniforms, and `@fx` default annotations.
This is an adaptation, not a claim that the original tutorial used this SDK.
See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for provenance and rights limitations.

## Run

Requirements: .NET 10 SDK, Windows 10 version 2004 (build 19041) or later,
and an x64 process. The project targets x64 automatically.

```powershell
dotnet restore ".\02 WPF XAML Liquid Glass.slnx" --configfile ".\NuGet.Config"
dotnet build ".\02 WPF XAML Liquid Glass.slnx" -c Release --no-restore
dotnet run --project ".\DemoApp\DemoApp.csproj" -c Release --no-build
```

In Visual Studio, open the solution, select `DemoApp` as the startup project,
restore packages, and run. Builds restore the public package and copy its matching
runtime assets; no local override, manual clean, or native prebuild is needed.

## Record the glass window

Normal desktop capture deliberately excludes the whole glass window from
compatible screen recordings, including Windows 11 Snipping Tool. The new
**Screen recording** section switches to capturing a specific background window
so the glass host itself can be recorded without recursively capturing itself.

1. Open Chrome/YouTube (or another source), keep it restored, and preferably
   maximize it behind the demo.
2. Click **Refresh windows**, select that window, and check **Enable recording
   mode**. No source is selected automatically or saved between runs.
3. Check the footer for `Active`, `Capture fallback: False`, and no error.
   Then use Snipping Tool's video mode to select a region including the demo.
4. Uncheck recording mode to return to the normal desktop backdrop.

Only the selected window supplies pixels. Overlap follows physical screen
coordinates; areas outside it use neutral padding, not the rest of the desktop.
Minimizing/closing the source shows a fallback/error, never a silent desktop
capture. Restore it or select a new source; toggle off/on to retry a failed
capture session. Windows may show its capture indicator. Protected content can
remain blank. Source selection and recording mode are opt-in for each session.

The demo binds `fx:Glass.CaptureSourceWindow` to an `IntPtr` in
[RecordingCaptureModel.cs](DemoApp/RecordingCaptureModel.cs). Zero means desktop
mode; a foreign top-level HWND enables selected-window mode. Capture and shader
rendering still live in the package; the helper only lists windows and tracks
the user's choice. Shader uniforms, their defaults, and Reset are unchanged.

## XAML-only integration

The application uses an ordinary `Window`. The package manages capture,
rendering, resize/move handling, and fallback through attached properties:

```xml
<Window
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:fx="clr-namespace:XamlTemplates.FX.Wpf;assembly=XamlTemplates.FX.Wpf"
    WindowStyle="None"
    AllowsTransparency="True"
    Background="Transparent"
    fx:Glass.IsEnabled="True">
    <fx:Glass.Effect>
        <fx:GlslShaderEffect Source="Shaders/LiquidGlassSample.glsl"
                             ReloadOnChange="True">
            <fx:GlslParameter Name="refractionStrength" Value="1" />
            <fx:GlslParameter Name="lensDisplacement" Value="60" />
            <fx:GlslParameter Name="cornerRadius" Value="16" />
            <fx:GlslParameter Name="refractionWidth" Value="0.30" />
            <fx:GlslParameter Name="blurRadius" Value="0.2" />
            <fx:GlslParameter Name="chromaticAberration" Value="1" />
            <fx:GlslParameter Name="brightness" Value="0.90" />
            <fx:GlslParameter Name="edgeHighlight" Value="0.15" />
            <fx:GlslParameter Name="saturation" Value="1" />
            <fx:GlslParameter Name="tintColor">
                <fx:GlslParameter.Value>
                    <Color>#FFFFFFFF</Color>
                </fx:GlslParameter.Value>
            </fx:GlslParameter>
            <fx:GlslParameter Name="tintStrength" Value="0" />
        </fx:GlslShaderEffect>
    </fx:Glass.Effect>
    <Grid Background="Transparent" />
</Window>
```

The actual [MainWindow.xaml](DemoApp/MainWindow.xaml) binds all eleven custom
uniforms explicitly to the demo's [tuning model](DemoApp/ShaderTuningModel.cs).
The runtime does **not** parse `@fx` comments into parameter values;
the comments document recommended settings, while the model supplies defaults.
The fixed-value example above shows the minimal integration without a controls panel.

## Live parameter controls

The left side stays clear to show the glass. The scrollable right panel groups
the ten numeric sliders under Geometry, Optics and Color, with a tint-color input
and preview. Changes update the existing shader parameters immediately; they do
not recompile the shader. Use **Reset defaults** to restore every original value.

Tint accepts `#RRGGBB`. Invalid or incomplete input shows an inline message and
keeps the last valid color. Raise tint strength above zero to see the tint.
Sliders support keyboard adjustment; scroll the panel to reach every control in
a small window. These demo slider limits are not SDK custom-uniform restrictions.

## Window behavior

The demo uses `WindowStyle=None`, `AllowsTransparency=True` and an initial
`Background=Transparent`, matching the SDK demo's transparent host. WPF
`WindowChrome` handles title-bar dragging and edge resizing. FX replaces the
initial background with the captured/shaded image; transparency alone is not
evidence that capture or the shader is working. Its three button handlers only minimize, maximize/restore and
close the window. Capture and rendering still require no application code-behind.
Make the content transparent or translucent so the SDK background remains visible.

Declare the package and shader in the project:

```xml
<ItemGroup>
    <PackageReference Include="XamlTemplates.FX.Wpf" Version="1.0.0-preview.5" />
    <XamlTemplatesFxShader Include="Shaders\LiquidGlassSample.glsl" />
    <None Update="Shaders\LiquidGlassSample.glsl"
          CopyToPublishDirectory="PreserveNewest" />
</ItemGroup>
```

## What the shader receives

- `u_textures[0]`: the SDK's captured input for this window (desktop normally,
  selected source with neutral padding in recording mode).
- `u_resolution`: the output dimensions in pixels.
- `uv`: the SDK's WebGL-style coordinates; use them directly, with no extra
  vertical flip or monitor/virtual-desktop offset.
- Custom uniforms: values explicitly supplied by `GlslParameter` in XAML.

The lens derives its size and center from `u_resolution`; it does not depend on
`u_mouse`. The shader refracts and blurs the captured background, while WPF
content is drawn over it.

`ReloadOnChange` watches the file resolved next to the running executable.
To live-edit it, edit the output `Shaders/LiquidGlassSample.glsl`; to keep a
change in the repository, edit the source file and rebuild. An invalid edit
keeps the last working shader; the footer displays runtime state, FPS, capture
fallback status and errors. An `Active` presenter does not by itself prove that
desktop capture is live: also check `Capture fallback` and the shader error text.
Load only shader files you trust.

## Original prototype and article

The original version used a borderless window, GDI capture,
`GlassyWindowBehavior`, and a compiled WPF `ShaderEffect`. That implementation
remains available in Git history at `ded012c`. The current application replaces
those components with the NuGet runtime; it does not run two capture pipelines.

The article outline has been updated in [docs/article-outline.md](docs/article-outline.md).

After the original prototype was shared on X, Microsoft support suggested a
Feedback Hub submission: “Bring back a modern Aero Glass / Liquid Glass material
for Windows 11.” The goal was a modern refractive Windows surface, not an exact
recreation of Aero.

## Limitations and licenses

Capture/GPU availability depends on the Windows session. When unavailable, the
SDK uses a readable fallback surface; check the footer's state/error text.
Visual behavior, dragging, and multi-monitor movement should be checked manually.

The repository's original sample code is covered by [LICENSE](LICENSE) (MIT).
The **NuGet binaries are not MIT**; they use the proprietary XamlTemplates FX
Community Binary License shipped in the package. Third-party shader/source rights
are not established by an attribution link or by this repository's MIT notice;
review [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) before redistribution.

No Apple private APIs are used. This sample and XamlTemplates.FX are not
affiliated with or endorsed by Apple or the tutorial author.
