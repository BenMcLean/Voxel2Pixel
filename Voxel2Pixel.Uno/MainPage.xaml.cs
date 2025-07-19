using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Voxel2Pixel.Uno.Services;

namespace Voxel2Pixel.Uno;

public sealed partial class MainPage : Microsoft.UI.Xaml.Controls.Page
{
	private static readonly string[] ValidJsonExtensions = { ".json" };

	public MainPage()
	{
		this.InitializeComponent();
	}

	private async void LoadFileButton_Click(object sender, RoutedEventArgs e)
	{
		await LoadJsonFileAsync();
	}

	private void ClearButton_Click(object sender, RoutedEventArgs e)
	{
		ClearContent();
	}

	private async Task LoadJsonFileAsync()
	{
		try
		{
			ShowLoading(true);
			HideError();

			FilePickerService.FileResult? fileResult = await FilePickerService.PickSingleFileAsync(
				ValidJsonExtensions,
				Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary);

			if (fileResult is null)
			{
				ShowLoading(false);
				return; // User cancelled
			}

			using (fileResult)
			{
				if (!FilePickerService.HasValidExtension(fileResult.FileName, ValidJsonExtensions))
				{
					ShowError($"Invalid file type. Please select a JSON file (.json). Selected: {fileResult.FileName}");
					ShowLoading(false);
					return;
				}

				await ProcessJsonFileAsync(fileResult);
			}
		}
		catch (Exception ex)
		{
			ShowError($"Failed to load file: {ex.Message}");
		}
		finally
		{
			ShowLoading(false);
		}
	}

	private async Task ProcessJsonFileAsync(FilePickerService.FileResult fileResult)
	{
		try
		{
			using StreamReader reader = new StreamReader(fileResult.Stream);
			string jsonContent = await reader.ReadToEndAsync();

			if (string.IsNullOrWhiteSpace(jsonContent))
			{
				ShowError("The selected file is empty or contains only whitespace.");
				return;
			}

			// Validate and format JSON
			string formattedJson = FormatJson(jsonContent);

			// Update UI
			JsonContentTextBlock.Text = formattedJson;

			FileNameTextBlock.Text = $"Loaded: {fileResult.FileName}";
			FileNameTextBlock.Visibility = Visibility.Visible;

			ClearButton.IsEnabled = true;
		}
		catch (JsonException jsonEx)
		{
			ShowError($"Invalid JSON format: {jsonEx.Message}");
		}
		catch (Exception ex)
		{
			ShowError($"Error processing file: {ex.Message}");
		}
	}

	[RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
	private static string FormatJson(string jsonContent)
	{
		try
		{
			JsonDocument jsonDocument = JsonDocument.Parse(jsonContent);
			JsonSerializerOptions options = new JsonSerializerOptions
			{
				WriteIndented = true
			};
			return JsonSerializer.Serialize(jsonDocument.RootElement, options);
		}
		catch (JsonException)
		{
			// If JSON is invalid, return original content
			return jsonContent;
		}
	}

	private void ClearContent()
	{
		JsonContentTextBlock.Text = "No file loaded. Click 'Load JSON File' to get started.";

		FileNameTextBlock.Text = string.Empty;
		FileNameTextBlock.Visibility = Visibility.Collapsed;

		ClearButton.IsEnabled = false;
		HideError();
	}

	private void ShowLoading(bool isLoading)
	{
		LoadingSection.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
		LoadFileButton.IsEnabled = !isLoading;
		ClearButton.IsEnabled = !isLoading && !string.IsNullOrEmpty(JsonContentTextBlock.Text) &&
								JsonContentTextBlock.Text != "No file loaded. Click 'Load JSON File' to get started.";
	}

	private void ShowError(string message)
	{
		ErrorTextBlock.Text = message;
		ErrorSection.Visibility = Visibility.Visible;
	}

	private void HideError()
	{
		ErrorSection.Visibility = Visibility.Collapsed;
	}
}
