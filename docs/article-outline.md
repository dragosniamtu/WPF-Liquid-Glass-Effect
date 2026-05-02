# Bringing Apple's Liquid Glass Feeling to WPF on Windows

## Hook

Apple's Liquid Glass effect is a useful design prompt for desktop UI: it is not just opacity and blur, it is a surface that bends what sits behind it.

This article shows a WPF approximation using public Windows APIs:

- a borderless transparent window;
- a screenshot of the desktop behind the window;
- a PixelShader that distorts the captured backdrop;
- a lightweight WPF surface that leaves room to see the distortion.

## Attribution

The shader direction was inspired by AmirHossein Aghajari's Medium article, "Liquid Glass: iOS Effect Explanation", published on November 24, 2025:

https://medium.com/@aghajari/liquid-glass-ios-effect-explanation-dabadd6414ae

The WPF window style started from the `GlassyWindowStyle` experiment in `XAMLTemplates.Net.WPF.Themes.Glass`.

## The WPF Shape

Start with `GlassyWindowStyle` in `DemoApp/Themes/LiquidGlassWindow.xaml`.

The template has:

- `WindowFrame` for the captured backdrop;
- `GlassyLayer` for the shader effect;
- a translucent content layer;
- caption buttons;
- a transparent resize border.

## Capturing the Backdrop

`ScreenCaptureHelper` uses GDI to capture the virtual screen. `GlassyWindowBehavior` crops the capture to the current window rectangle and assigns it to an `ImageBrush`.

The trick is to hide the window briefly while capturing so the window does not photograph itself.

## Applying the Shader

`GlassyEffect` is a WPF `ShaderEffect` wrapper around the compiled `GlassyEffect.ps` file.

The behavior updates the shader's texture size, glass center, glass size, and blur intensity whenever the window changes.

## Keeping The Surface Focused

The sample only styles the outer window. The content stays lightweight so the article can clearly show that the glass effect is a host surface and not a full control theme.

## Caveats

- This is a Windows approximation, not Apple's implementation.
- Screen capture has a cost, so real apps should update it deliberately.
- Remote desktop, disabled transparency effects, or restricted DWM sessions may fall back to a tinted surface.
