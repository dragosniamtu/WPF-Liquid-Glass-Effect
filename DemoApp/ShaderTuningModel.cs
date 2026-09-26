using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;

namespace DemoApp;

public sealed class NumericSetting : INotifyPropertyChanged
{
    private double value;

    public NumericSetting(string label, string uniformName, double initialValue,
        double minimum, double maximum, double step, string unit = "")
    {
        Label = label;
        UniformName = uniformName;
        DefaultValue = initialValue;
        Minimum = minimum;
        Maximum = maximum;
        Step = step;
        Unit = unit;
        value = initialValue;
    }

    public string Label { get; }
    public string UniformName { get; }
    public double DefaultValue { get; }
    public double Minimum { get; }
    public double Maximum { get; }
    public double Step { get; }
    public string Unit { get; }
    public string DisplayValue => Value.ToString("0.##", CultureInfo.InvariantCulture) + Unit;

    public double Value
    {
        get => value;
        set
        {
            if (!double.IsFinite(value)) return;
            double bounded = Math.Clamp(value, Minimum, Maximum);
            if (this.value.Equals(bounded)) return;
            this.value = bounded;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayValue)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class ShaderTuningModel : INotifyPropertyChanged
{
    public NumericSetting CornerRadius { get; } = new("Corner radius", "cornerRadius", 16, 0, 120, 1, " px");
    public NumericSetting RefractionStrength { get; } = new("Refraction strength", "refractionStrength", 1, 0, 2, 0.01);
    public NumericSetting LensDisplacement { get; } = new("Lens displacement", "lensDisplacement", 60, 0, 120, 1, " px");
    public NumericSetting RefractionWidth { get; } = new("Refraction width", "refractionWidth", 0.30, 0.05, 0.60, 0.01);
    public NumericSetting BlurRadius { get; } = new("Blur radius", "blurRadius", 0.2, 0, 8, 0.01, " px");
    public NumericSetting ChromaticAberration { get; } = new("Chromatic aberration", "chromaticAberration", 1, 0, 3, 0.01);
    public NumericSetting EdgeHighlight { get; } = new("Edge highlight", "edgeHighlight", 0.15, 0, 1, 0.01);
    public NumericSetting Brightness { get; } = new("Brightness", "brightness", 0.90, 0.5, 1.25, 0.01);
    public NumericSetting Saturation { get; } = new("Saturation", "saturation", 1, 0, 2, 0.01);
    public NumericSetting TintStrength { get; } = new("Tint strength", "tintStrength", 0, 0, 1, 0.01);

    public IReadOnlyList<NumericSetting> GeometryControls { get; }
    public IReadOnlyList<NumericSetting> OpticsControls { get; }
    public IReadOnlyList<NumericSetting> ColorControls { get; }
    public ICommand ResetCommand { get; }

    private string tintHex = "#FFFFFF";
    private Color tintColor = Colors.White;
    private string tintError = string.Empty;

    public ShaderTuningModel()
    {
        GeometryControls = [CornerRadius, RefractionStrength, LensDisplacement, RefractionWidth];
        OpticsControls = [BlurRadius, ChromaticAberration, EdgeHighlight];
        ColorControls = [Brightness, Saturation, TintStrength];
        ResetCommand = new ResetSettingsCommand(Reset);
    }

    public string TintHex
    {
        get => tintHex;
        set
        {
            value ??= string.Empty;
            if (tintHex == value) return;
            tintHex = value;
            Notify();
            string candidate = value.Trim();
            if (candidate.Length == 7 && candidate[0] == '#'
                && uint.TryParse(candidate.AsSpan(1), NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture, out uint rgb))
            {
                TintColor = Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
                TintError = string.Empty;
            }
            else
            {
                // Keep the last valid shader value while the user is typing.
                TintError = "Use #RRGGBB, for example #80C8FF.";
            }
        }
    }

    public Color TintColor
    {
        get => tintColor;
        private set
        {
            if (tintColor == value) return;
            tintColor = value;
            Notify();
        }
    }

    public string TintError
    {
        get => tintError;
        private set
        {
            if (tintError == value) return;
            tintError = value;
            Notify();
            Notify(nameof(HasTintError));
        }
    }

    public bool HasTintError => TintError.Length != 0;

    public void Reset()
    {
        foreach (NumericSetting setting in GeometryControls.Concat(OpticsControls).Concat(ColorControls))
            setting.Value = setting.DefaultValue;
        TintHex = "#FFFFFF";
        TintColor = Colors.White;
        TintError = string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private sealed class ResetSettingsCommand(Action reset) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => reset();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
