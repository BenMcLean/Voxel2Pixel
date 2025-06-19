using Voxel2Pixel.Model;

namespace Voxel2Pixel.Uno.Controls;

public sealed partial class CuboidOrientationInput : UserControl
{
	public static readonly DependencyProperty ValueProperty =
		DependencyProperty.Register(
			nameof(Value),
			typeof(CuboidOrientation),
			typeof(CuboidOrientationInput),
			new PropertyMetadata(CuboidOrientation.SOUTH0, OnValueChanged));
	public CuboidOrientation Value
	{
		get => (CuboidOrientation)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}
	public event EventHandler<CuboidOrientation>? ValueChanged;
	public CuboidOrientationInput()
	{
		InitializeComponent();
		InitializeComboBox();
	}
	private void InitializeComboBox()
	{
		OrientationComboBox.ItemsSource = CuboidOrientation.Values;
		OrientationComboBox.SelectedItem = Value;
	}
	private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is CuboidOrientationInput control)
			control.OrientationComboBox.SelectedItem = e.NewValue;
	}
	private void OrientationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.AddedItems.FirstOrDefault() is CuboidOrientation newValue)
		{
			Value = newValue;
			ValueChanged?.Invoke(this, newValue);
		}
	}
	private void Turn(params Turn[] turns)
	{
		CuboidOrientation newValue = (CuboidOrientation)Value.Turn(turns);
		Value = newValue;
		ValueChanged?.Invoke(this, newValue);
	}
	private void ClockX_Click(object sender, RoutedEventArgs e) => Turn(Model.Turn.ClockX);
	private void ClockY_Click(object sender, RoutedEventArgs e) => Turn(Model.Turn.ClockY);
	private void ClockZ_Click(object sender, RoutedEventArgs e) => Turn(Model.Turn.ClockZ);
	private void CounterX_Click(object sender, RoutedEventArgs e) => Turn(Model.Turn.CounterX);
	private void CounterY_Click(object sender, RoutedEventArgs e) => Turn(Model.Turn.CounterY);
	private void CounterZ_Click(object sender, RoutedEventArgs e) => Turn(Model.Turn.CounterZ);
	private void Reset_Click(object sender, RoutedEventArgs e) => Turn(Model.Turn.Reset);
}
