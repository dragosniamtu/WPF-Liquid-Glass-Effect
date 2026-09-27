# Liquid Glass on WPF: loading an external GLSL shader with XamlTemplates.FX

## Update to the original article

The original Blog/02 prototype demonstrated a borderless WPF window, a GDI
desktop screenshot helper, and a compiled WPF pixel shader. This revision keeps
an updated version of the original tutorial-inspired shader in
[LiquidGlassSample.glsl](../DemoApp/Shaders/LiquidGlassSample.glsl), adapted for
the [XamlTemplates.FX.Wpf NuGet library](https://www.nuget.org/packages/XamlTemplates.FX.Wpf/).
The library replaces the prototype's local capture/rendering code; the shader
remains a separate, editable file supplied by this demo, not a built-in preset.

The earlier implementation is available in Git history at `ded012c`. The
[current demo recording](../README.md#demo-video) replaces the old prototype
clips in the README and shows the FX-backed demo with its external shader and
live controls. Its selected-window recording mode is included in the public
`XamlTemplates.FX.Wpf 1.0.0-preview.5` package; earlier previews did not include it.

## Credit the original tutorial

The shader direction comes from AmirHossein Aghajari's
[Liquid Glass: iOS Effect Explanation](https://medium.com/@aghajari/liquid-glass-ios-effect-explanation-dabadd6414ae),
published November 24, 2025. Mention it prominently when describing the optics.

We adapted the sample to the FX package's shader-input contract and exposed
parameters. The original tutorial did not use this package. Attribution does
not establish redistribution rights; link to [the notices](../THIRD-PARTY-NOTICES.md).

## What changed in the original shader file

The updated [LiquidGlassSample.glsl](../DemoApp/Shaders/LiquidGlassSample.glsl)
retains the tutorial-inspired radial distortion, Gaussian blur and color-channel
separation while adapting the effect to the library:

- The glass covers the window using `u_resolution`, replacing the fixed
  120 × 80 lens around `u_mouse`.
- The library provides the captured background through `u_textures[0]` and the
  matching `uv` coordinates; the shader does not capture the screen itself.
- Eleven custom uniforms expose corner radius, refraction strength and width,
  lens displacement, blur, chromatic aberration, brightness, edge highlight,
  saturation, tint color and tint strength.
- `@fx` comments document suggested defaults. The demo supplies the actual
  values through bindable `GlslParameter` entries, so its controls can adjust
  the effect without editing or recompiling the shader.

This is an adaptation of the original example for XamlTemplates.FX, not an
unchanged copy or a claim that the original tutorial used the library.

## Install the public package

Package page: [XamlTemplates.FX.Wpf on NuGet.org](https://www.nuget.org/packages/XamlTemplates.FX.Wpf/).

Requirements: .NET 10, Windows 10 build 19041 or later, x64.

Install the package version used by this demo:

```xml
<PackageReference Include="XamlTemplates.FX.Wpf" Version="1.0.0-preview.5" />
```

The repository selects NuGet.org through `NuGet.Config`. Both custom shader
loading and selected-window recording support come from the public package;
developers do not need the private SDK repository or locally built packages.
See [the README](../README.md#record-the-glass-window) for the recording setup.

## Attach the effect to an ordinary Window

The important application code is in `DemoApp/MainWindow.xaml`. This excerpt
shows the loading API; the complete Window supplies **all** custom uniforms:

```xml
<fx:Glass.Effect>
    <fx:GlslShaderEffect Source="Shaders/LiquidGlassSample.glsl"
                         ReloadOnChange="True">
        <fx:GlslParameter Name="lensDisplacement" Value="60" />
        <fx:GlslParameter Name="blurRadius" Value="0.2" />
    </fx:GlslShaderEffect>
</fx:Glass.Effect>
```

Add `fx:Glass.IsEnabled="True"` to the Window and import
`clr-namespace:XamlTemplates.FX.Wpf;assembly=XamlTemplates.FX.Wpf`.
The demo removes the native title bar with `WindowStyle=None` and uses WPF
`WindowChrome` for dragging and resizing. It sets `AllowsTransparency=True`
and an initial transparent background, matching the SDK demo's transparent host.
FX paints the captured/shaded image behind the content; merely seeing through
the window does not confirm the effect is running. Button handlers
only operate the window. No capture/rendering code-behind or custom Window base
class is required.

## Ship an actual external shader

```xml
<XamlTemplatesFxShader Include="Shaders\LiquidGlassSample.glsl" />
<None Update="Shaders\LiquidGlassSample.glsl"
      CopyToPublishDirectory="PreserveNewest" />
```

These project items copy the file to the build/publish output. The NuGet runtime
compiles and loads it; it is not a built-in preset or a bundled reference image.

## Map the glass to the window

The original small lens used a fixed 120 × 80 area around `u_mouse`.
The adapted file derives the lens size from `u_resolution` and uses half that
size as its center. It does not read `u_mouse`.

The SDK supplies the captured desktop texture in `u_textures[0]`. Use the
provided `uv` directly, without adding virtual-screen offsets or a second flip.
The shader applies radial distortion, channel separation, blur, tint and edge
highlights to that captured image. WPF draws the content on top.

## Explain configurable parameters

The file declares custom uniforms such as:

```glsl
uniform float lensDisplacement; // @fx default=60
uniform float blurRadius;       // @fx default=0.2
uniform float brightness;       // @fx default=0.90
```

`@fx` comments record suggested defaults for tuning tools; preview.5's runtime
does not automatically read them. A matching `GlslParameter` in XAML supplies
the actual value, and its `Value` can use a normal WPF binding. Keep all custom
uniforms populated, as shown in the complete demo, rather than removing a
parameter and expecting its comment default to be applied.

Changing the output shader file demonstrates live reload. Changes to source
files must be rebuilt/copied to that output location. Failed edits keep the last
working shader and expose an error through `Glass.LastError`.

## Distinguish the built-in effect

This example deliberately loads `LiquidGlassSample.glsl`. The package's current
built-in Liquid Glass uses a different beveled-sheet model. Do not describe the
external sample as identical to that built-in effect.

## Caveats

- Use only trusted shader files.
- Capture and GPU availability vary by Windows session; readable fallback and
  runtime/error status are part of the demo.
- Manually check moving/resizing, monitor transitions, source alignment, and
  shader edits on the target machine.
- The sample-owned code is MIT; the NuGet binaries use their own proprietary
  binary license, and upstream shader permissions require separate consideration.
- This is not Apple's implementation and no Apple private APIs are used.
