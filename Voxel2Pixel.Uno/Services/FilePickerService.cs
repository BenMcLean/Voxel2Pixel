using Windows.Storage.Pickers;

#if WINDOWS && !HAS_UNO
using WinRT.Interop;
#endif

namespace Voxel2Pixel.Uno.Services;

public class FilePickerService
{
	public record FileResult(Stream Stream, string FileName) : IDisposable, IAsyncDisposable
	{
		private bool _disposed = false;
		public void Dispose()
		{
			if (_disposed)
				return;
			Stream?.Dispose();
			_disposed = true;
			GC.SuppressFinalize(this);
		}
		public async ValueTask DisposeAsync()
		{
			if (_disposed)
				return;
			if (Stream is not null)
				await Stream.DisposeAsync();
			_disposed = true;
			GC.SuppressFinalize(this);
		}
	}
#if WINDOWS && !HAS_UNO
	/// <summary>
	/// Get the current window handle - you may need to adjust this based on your App setup
	/// </summary>
	private static void InitializePickerForWindows(FileOpenPicker picker)
	{
		Microsoft.UI.Xaml.Window? window = Microsoft.UI.Xaml.Application.Current.GetType().GetProperty("MainWindow")?.GetValue(Microsoft.UI.Xaml.Application.Current) as Microsoft.UI.Xaml.Window;
		if (window is not null)
			InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
	}
#endif
	public static async Task<FileResult?> PickSingleFileAsync(IEnumerable<string> fileTypeFilters, PickerLocationId suggestedStartLocation = PickerLocationId.DocumentsLibrary)
	{
		FileOpenPicker fileOpenPicker = new()
		{
			SuggestedStartLocation = suggestedStartLocation,
		};
		fileOpenPicker.FileTypeFilter.AddRange(fileTypeFilters);
#if WINDOWS && !HAS_UNO
		InitializePickerForWindows(fileOpenPicker);
#endif
		return await fileOpenPicker.PickSingleFileAsync() is StorageFile pickedFile ?
			new FileResult(await pickedFile.OpenStreamForReadAsync(), pickedFile.Name)
			: null;
	}
	public static async IAsyncEnumerable<FileResult> PickMultipleFilesAsync(IEnumerable<string> fileTypeFilters, PickerLocationId suggestedStartLocation = PickerLocationId.DocumentsLibrary)
	{
		FileOpenPicker fileOpenPicker = new()
		{
			SuggestedStartLocation = suggestedStartLocation,
		};
		fileOpenPicker.FileTypeFilter.AddRange(fileTypeFilters);
#if WINDOWS && !HAS_UNO
		InitializePickerForWindows(fileOpenPicker);
#endif
		foreach (StorageFile file in await fileOpenPicker.PickMultipleFilesAsync())
			yield return new FileResult(await file.OpenStreamForReadAsync(), file.Name);
	}
	public static bool HasValidExtension(string fileName, params string[] validExtensions) => HasValidExtension(fileName, validExtensions.AsEnumerable());
	public static bool HasValidExtension(string fileName, IEnumerable<string> validExtensions)
	{
		if (string.IsNullOrEmpty(fileName))
			return false;
		string extension = Path.GetExtension(fileName);
		return validExtensions.Any(validExtension => extension.Equals(validExtension, StringComparison.InvariantCultureIgnoreCase));
	}
}
