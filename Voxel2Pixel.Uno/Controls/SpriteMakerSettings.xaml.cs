using System;
using System.Globalization;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;

namespace Voxel2Pixel.Uno.Controls;

public sealed partial class SpriteMakerSettings : UserControl
{
	public static readonly DependencyProperty SpriteMakerProperty =
		DependencyProperty.Register(
			nameof(SpriteMaker),
			typeof(SpriteMaker),
			typeof(SpriteMakerSettings),
			new PropertyMetadata(new SpriteMaker(), OnSpriteMakerChanged));
	public SpriteMaker SpriteMaker
	{
		get => (SpriteMaker)GetValue(SpriteMakerProperty);
		set => SetValue(SpriteMakerProperty, value);
	}
	public event EventHandler<SpriteMaker>? SpriteMakerChanged;
	public event EventHandler<SpriteMaker>? PreviewRequested;
	private bool _isUpdatingControls = false;
	public SpriteMakerSettings()
	{
		InitializeComponent();
		InitializeControls();
		UpdateControlsFromSpriteMaker();
	}
	private void InitializeControls()
	{
		PerspectiveComboBox.ItemsSource = Enum.GetValues<Perspective>();
	}
	private static void OnSpriteMakerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is SpriteMakerSettings control)
			control.UpdateControlsFromSpriteMaker();
	}
	private void UpdateControlsFromSpriteMaker()
	{
		if (SpriteMaker is null) return;
		_isUpdatingControls = true;
		try
		{
			// Perspective settings
			PerspectiveComboBox.SelectedItem = SpriteMaker.Perspective;
			ScaleXNumberBox.Value = SpriteMaker.ScaleX;
			ScaleYNumberBox.Value = SpriteMaker.ScaleY;
			ScaleZNumberBox.Value = SpriteMaker.ScaleZ;
			RadiansSlider.Value = SpriteMaker.Radians;

			// Orientation settings
			FlipXCheckBox.IsChecked = SpriteMaker.FlipX;
			FlipYCheckBox.IsChecked = SpriteMaker.FlipY;
			FlipZCheckBox.IsChecked = SpriteMaker.FlipZ;
			CuboidOrientationInput.Value = SpriteMaker.CuboidOrientation;

			// Rendering options
			ShadowCheckBox.IsChecked = SpriteMaker.Shadow;
			OutlineCheckBox.IsChecked = SpriteMaker.Outline;
			PeakCheckBox.IsChecked = SpriteMaker.Peak;
			CropCheckBox.IsChecked = SpriteMaker.Crop;
			OutlineColorTextBox.Text = SpriteMaker.OutlineColor.ToString("X8");
			ThresholdNumberBox.Value = SpriteMaker.Threshold;

			// Final scale settings
			FinalScaleXNumberBox.Value = SpriteMaker.FinalScaleX;
			FinalScaleYNumberBox.Value = SpriteMaker.FinalScaleY;

			// Sprite generation
			NumberOfSpritesNumberBox.Value = SpriteMaker.NumberOfSprites;
		}
		finally
		{
			_isUpdatingControls = false;
		}
	}
	private void NotifySpriteMakerChanged()
	{
		if (!_isUpdatingControls)
			SpriteMakerChanged?.Invoke(this, SpriteMaker);
	}
	#region Event handlers
	private void PerspectiveComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!_isUpdatingControls && e.AddedItems.Count > 0 && e.AddedItems[0] is Perspective perspective)
		{
			SpriteMaker = SpriteMaker.Set(perspective);
			NotifySpriteMakerChanged();
		}
	}
	private void NumberBox_ValueChanged(object sender, NumberBoxValueChangedEventArgs args)
	{
		if (_isUpdatingControls || double.IsNaN(args.NewValue)) return;
		NumberBox numberBox = (NumberBox)sender;
		switch (numberBox.Name)
		{
			case nameof(ScaleXNumberBox):
				SpriteMaker = SpriteMaker.SetScaleX((byte)Math.Clamp(args.NewValue, 1, 255));
				break;
			case nameof(ScaleYNumberBox):
				SpriteMaker = SpriteMaker.SetScaleY((byte)Math.Clamp(args.NewValue, 1, 255));
				break;
			case nameof(ScaleZNumberBox):
				SpriteMaker = SpriteMaker.SetScaleZ((byte)Math.Clamp(args.NewValue, 1, 255));
				break;
			case nameof(FinalScaleXNumberBox):
				SpriteMaker = SpriteMaker.SetFinalScaleX((byte)Math.Clamp(args.NewValue, 1, 255));
				break;
			case nameof(FinalScaleYNumberBox):
				SpriteMaker = SpriteMaker.SetFinalScaleY((byte)Math.Clamp(args.NewValue, 1, 255));
				break;
			case nameof(ThresholdNumberBox):
				SpriteMaker = SpriteMaker.SetThreshold((byte)Math.Clamp(args.NewValue, 0, 255));
				break;
			case nameof(NumberOfSpritesNumberBox):
				SpriteMaker = SpriteMaker.SetNumberOfSprites((ushort)Math.Clamp(args.NewValue, 1, 65535));
				break;
		}
		NotifySpriteMakerChanged();
	}
	private void CheckBox_Changed(object sender, RoutedEventArgs e)
	{
		if (_isUpdatingControls) return;
		CheckBox checkBox = (CheckBox)sender;
		bool isChecked = checkBox?.IsChecked ?? false;
		switch (checkBox?.Name)
		{
			case nameof(FlipXCheckBox):
				SpriteMaker = SpriteMaker.SetFlipX(isChecked);
				break;
			case nameof(FlipYCheckBox):
				SpriteMaker = SpriteMaker.SetFlipY(isChecked);
				break;
			case nameof(FlipZCheckBox):
				SpriteMaker = SpriteMaker.SetFlipZ(isChecked);
				break;
			case nameof(ShadowCheckBox):
				SpriteMaker = SpriteMaker.SetShadow(isChecked);
				break;
			case nameof(OutlineCheckBox):
				SpriteMaker = SpriteMaker.SetOutline(isChecked);
				break;
			case nameof(PeakCheckBox):
				SpriteMaker = SpriteMaker.SetPeak(isChecked);
				break;
			case nameof(CropCheckBox):
				SpriteMaker = SpriteMaker.SetCrop(isChecked);
				break;
		}
		NotifySpriteMakerChanged();
	}
	private void RadiansSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
	{
		if (!_isUpdatingControls)
		{
			SpriteMaker = SpriteMaker.SetRadians(e.NewValue);
			NotifySpriteMakerChanged();
		}
	}
	private void CuboidOrientationInput_ValueChanged(object sender, CuboidOrientation e)
	{
		if (!_isUpdatingControls)
		{
			SpriteMaker = SpriteMaker.Set(e);
			NotifySpriteMakerChanged();
		}
	}
	private void OutlineColorTextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (!_isUpdatingControls && sender is TextBox textBox)
		{
			if (uint.TryParse(textBox.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint color))
			{
				SpriteMaker = SpriteMaker.SetOutlineColor(color);
				NotifySpriteMakerChanged();
			}
		}
	}
	private void ResetButton_Click(object sender, RoutedEventArgs e)
	{
		SpriteMaker = new SpriteMaker();
		UpdateControlsFromSpriteMaker();
		NotifySpriteMakerChanged();
	}
	private void PreviewButton_Click(object sender, RoutedEventArgs e)
	{
		PreviewRequested?.Invoke(this, SpriteMaker);
	}
	#endregion Event handlers
}
