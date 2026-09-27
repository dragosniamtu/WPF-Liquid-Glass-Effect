# Third-Party Notices

## CHARGE footage in the demo recording

The updated 35-second demo recording (`docs/XamlTemplates.Fx-Library.mp4` and
the README's embedded copy) includes an excerpt of
[CHARGE](https://www.youtube.com/watch?v=UXqq0ZvbOnk), copyright Blender Foundation,
licensed under [Creative Commons Attribution 4.0 International](https://creativecommons.org/licenses/by/4.0/).
The footage is shown inside a screen recording, partially refracted through
XamlTemplates.FX, trimmed and resized for this demonstration. The repository
video retains the recording's audio; the separately prepared X/Reddit edits
remove audio and add branding.

Keep this attribution, source link, license link and modification notice with
redistributed excerpts. The film is not covered by this repository's MIT license.
No endorsement by Blender Foundation or Blender Studio is implied.

## Original tutorial and external shader adaptation

The historical glass shader direction comes from AmirHossein Aghajari,
“Liquid Glass: iOS Effect Explanation,” Medium, November 24, 2025:

https://medium.com/@aghajari/liquid-glass-ios-effect-explanation-dabadd6414ae

`DemoApp/Shaders/LiquidGlassSample.glsl` preserves the tutorial-inspired
radial refraction, chromatic separation, and Gaussian sampling used by the
earlier prototype. The sample was adapted for XamlTemplates.FX.Wpf on
September 13, 2026:

- the captured window determines lens size and center instead of a fixed
  120 × 80 lens centered on the mouse;
- the SDK supplies `uv`, `u_resolution`, and `u_textures[0]`;
- configurable uniforms document recommended defaults through `@fx` comments;
  the preview.3 application supplies their values explicitly from XAML;
- the full shader remains a separate application file, outside the NuGet binary
  package and independent of the package's current built-in Liquid Glass effect.

The SDK handles capture and rendering; this is an adaptation of the earlier
sample to the package, not the original author's SDK integration. The article
text and its images are not copied into this repository by this update.

Attribution is not a license grant. No explicit code-redistribution grant was
identified in the referenced article during the earlier review; this is not
a definitive legal determination of the author's permissions. This repository's
MIT license covers rights its contributors can grant, not third-party rights they
do not own. Obtain appropriate permission or legal review before redistributing
the tutorial-derived shader. This notice is not a claim of legal clearance.

## XamlTemplates.FX NuGet package

The demo downloads `XamlTemplates.FX.Wpf` version `1.0.0-preview.5` from
https://www.nuget.org/packages/XamlTemplates.FX.Wpf/1.0.0-preview.5.

The package's compiled components are governed by the included XamlTemplates FX
Community Binary License, not this repository's MIT license. The package also
includes upstream license notices for its glslang and SPIRV-Cross shader-compiler
components. Keep those notices with distributions as required by their terms.

No private SDK source or pipeline implementation is included here.

## Historical XAMLTemplates.Net prototype

The original window style was adapted from the `GlassyWindowStyle` concept in
`XAMLTemplates.Net.WPF.Themes.Glass;component/Themes/Generic.xaml`, specifically
its `Themes/WindowStyle.xaml` resource dictionary. That standalone WPF capture
and window-style implementation remains in Git history; it is no longer the
active demo pipeline.

## Apple

Apple and Liquid Glass are names associated with Apple Inc. This sample is a
Windows approximation using public APIs, not Apple's implementation. This
repository is not affiliated with, sponsored by, or endorsed by Apple Inc.
