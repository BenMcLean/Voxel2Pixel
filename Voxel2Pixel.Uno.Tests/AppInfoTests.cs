namespace Voxel2Pixel.Uno.Tests;

public class AppInfoTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void AppInfoCreation()
    {
		AppConfig appInfo = new AppConfig { Environment = "Test" };

        appInfo.Should().NotBeNull();
        appInfo.Environment.Should().Be("Test");
    }
}
