
namespace Voxel2Pixel.Uno.UITests;

public class TestBase
{
	private IApp? _app;

	static TestBase()
	{
		AppInitializer.TestEnvironment.AndroidAppName = Constants.AndroidAppName;
		AppInitializer.TestEnvironment.WebAssemblyDefaultUri = Constants.WebAssemblyDefaultUri;
		AppInitializer.TestEnvironment.iOSAppName = Constants.iOSAppName;
		AppInitializer.TestEnvironment.AndroidAppName = Constants.AndroidAppName;
		AppInitializer.TestEnvironment.iOSDeviceNameOrId = Constants.iOSDeviceNameOrId;
		AppInitializer.TestEnvironment.CurrentPlatform = Constants.CurrentPlatform;
		AppInitializer.TestEnvironment.WebAssemblyBrowser = Constants.WebAssemblyBrowser;

#if DEBUG
		AppInitializer.TestEnvironment.WebAssemblyHeadless = false;
#endif

		// Start the app only once, so the tests runs don't restart it
		// and gain some time for the tests.
		AppInitializer.ColdStartApp();
	}

	protected IApp App
	{
		get => _app!;
		private set
		{
			_app = value;
			Helpers.App = value;
		}
	}

	[SetUp]
	public void SetUpTest()
	{
		App = AppInitializer.AttachToApp();
	}

	[TearDown]
	public void TearDownTest()
	{
		TakeScreenshot("teardown");
	}

	public FileInfo TakeScreenshot(string stepName)
	{
		string title = $"{TestContext.CurrentContext.Test.Name}_{stepName}"
			.Replace(" ", "_")
			.Replace(".", "_");

		FileInfo fileInfo = App.Screenshot(title);

		string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileInfo.Name);
		if (fileNameWithoutExt != title && fileInfo.DirectoryName != null)
		{
			string destFileName = Path
				.Combine(fileInfo.DirectoryName, title + Path.GetExtension(fileInfo.Name));

			if (File.Exists(destFileName))
			{
				File.Delete(destFileName);
			}

			File.Move(fileInfo.FullName, destFileName);

			TestContext.AddTestAttachment(destFileName, stepName);

			fileInfo = new FileInfo(destFileName);
		}
		else
		{
			TestContext.AddTestAttachment(fileInfo.FullName, stepName);
		}

		return fileInfo;
	}

}
