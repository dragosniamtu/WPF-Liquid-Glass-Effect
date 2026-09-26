# Liquid Glass on WPF: loading an external GLSL shader with XamlTemplates.FX

## Update to the original article

The original Blog/02 prototype demonstrated a borderless WPF window, a GDI
desktop screenshot helper, and a compiled WPF pixel shader. This revision keeps
the tutorial-inspired shader as a separate GLSL asset but replaces the local
capture/rendering code with the public **XamlTemplates.FX.Wpf 1.0.0-preview.3**
NuGet package.

The earlier implementation is available in Git history at `ded012c`. Existing
screenshots and video show that standalone prototype; they are not new package
integration screenshots.

## Credit the original tutorial

The shader direction comes from AmirHossein Aghajari's
[Liquid Glass: iOS Effect Explanation](https://medium.com/@aghajari/liquid-glass-ios-effect-explanation-dabadd6414ae),
published November 24, 2025. Mention it prominently when describing the optics.

We adapted the sample to the FX package's shader-input contract and exposed
parameters. The original tutorial did not use this package. Attribution does
not establish redistribution rights; link to [the notices](../THIRD-PARTY-NOTICES.md).

## Install the public package

Requirements: .NET 10, Windows 10 build 19041 or later, x64.

```xml
<PackageReference Include="XamlTemplates.FX.Wpf" Version="1.0.0-preview.3" />
```

The repository selects NuGet.org through `NuGet.Config`; developers do not need
the private SDK repository or locally built packages.

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

`@fx` comments record suggested defaults for tuning tools; preview.3's runtime
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
