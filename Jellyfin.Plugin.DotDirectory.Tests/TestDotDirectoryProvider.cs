namespace Jellyfin.Plugin.DotDirectory.Tests;

using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Jellyfin.Plugin.DotDirectory.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Controller.Library;

class TestBaseItem : BaseItem
{
    public TestBaseItem(string path) : base()
    {
        Path = path;
    }
}

[TestClass]
public class TestDotDirectoryProvider
{
    private string rootDir;
    private Mock<ILogger<DotDirectoryProvider>> logger;
    private Mock<IMediaSourceManager> mediaSourceManager;
    private DotDirectoryProvider self;

    [TestInitialize]
    public void Initialize()
    {
        rootDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(rootDir);
        logger = new Mock<ILogger<DotDirectoryProvider>>();
        self = new DotDirectoryProvider(logger.Object);

        mediaSourceManager = new Mock<IMediaSourceManager>();
        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>())).Returns(MediaProtocol.File);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Directory.Delete(rootDir, true);
        BaseItem.MediaSourceManager = null;
    }


    // ┌────────────────────────────────────────────────────┐
    // │                   Helper Methods                   │
    // └────────────────────────────────────────────────────┘

    private static void testLogRecieved(Mock<ILogger<DotDirectoryProvider>> mock, LogLevel level, string message)
    {
        testLogRecieved<Exception>(mock, level, message);
    }

    private static void testLogRecieved<T>(Mock<ILogger<DotDirectoryProvider>> mock, LogLevel level, string message)
    where T : Exception
    {
        mock.Verify(logger => logger.Log(
                        It.Is<LogLevel>(logLevel => logLevel == level),
                        It.Is<EventId>(eventId => eventId.Id == 0),
                        It.Is<It.IsAnyType>((@object, @type) => @object.ToString() == message && @type.Name == "FormattedLogValues"),
                        It.IsAny<T>(),
                        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                    Times.Once);
    }

    // ┌────────────────────────────────────────────────────┐
    // │                  Test Read INI File                │
    // └────────────────────────────────────────────────────┘

    [TestMethod]
    public async Task TestCanReadIconFieldFromFile()
    {
        // GIVEN
        var configFile = Path.Join(rootDir, "foo");
        File.WriteAllText(configFile, "[Desktop Entry]\nIcon=my-icon\n");

        // WHEN
        var result = await self.ReadIconFromConfigFile(configFile);

        // THEN
        Assert.AreEqual("my-icon", result);
    }

    [TestMethod]
    public async Task TestPicksFirstEntryMatchingTargetInFile()
    {
        // GIVEN
        var configFile = Path.Join(rootDir, "foo");
        File.WriteAllText(configFile, "[Desktop Entry]\nIcon=my-icon1\nIcon=my-icon2");

        // WHEN
        var result = await self.ReadIconFromConfigFile(configFile);

        // THEN
        Assert.AreEqual("my-icon1", result);
    }

    [TestMethod]
    public async Task TestPicksFirstEntryMatchingTargetInFileAcrossMultipleSections()
    {
        // GIVEN
        var configFile = Path.Join(rootDir, "foo");
        File.WriteAllText(configFile, "[Desktop Entry]\nIcon=my-icon1\n[Desktop Entry]\nIcon=my-icon2");

        // WHEN
        var result = await self.ReadIconFromConfigFile(configFile);

        // THEN
        Assert.AreEqual("my-icon1", result);
    }

    [TestMethod]
    public async Task TestSkipsCorrectFieldWhenInWrongSection()
    {
        // GIVEN
        var configFile = Path.Join(rootDir, "foo");
        File.WriteAllText(configFile, "[some-section]\nIcon=my-icon");

        // WHEN
        var result = await self.ReadIconFromConfigFile(configFile);

        // THEN
        Assert.IsNull(result);
        testLogRecieved(logger, LogLevel.Warning,
                        "Found icon definition to icon=my-icon from file=" + configFile + " but its in incorrect section=some-section");
    }

    [TestMethod]
    public async Task TestGracefullyHandlesMissingFile()
    {
        // GIVEN
        var configFile = Path.Join(rootDir, "foo");

        // WHEN
        var result = await self.ReadIconFromConfigFile(configFile);

        // THEN
        Assert.IsNull(result, "Should not have read an icon value from non-existant file");
        testLogRecieved<System.IO.IOException>(logger, LogLevel.Error, "Failed to read config file=" + configFile);
    }

    [TestMethod]
    public async Task TestCanProcessFileWithoutTrailingNewline()
    {
        // GIVEN
        var configFile = Path.Join(rootDir, "foo");
        File.WriteAllText(configFile, "[Desktop Entry]\nIcon=my-icon");

        // WHEN
        var result = await self.ReadIconFromConfigFile(configFile);

        // THEN
        Assert.AreEqual("my-icon", result);
    }

    [TestMethod]
    public async Task TestReturnsNullIfFieldIsUnassignedInFile()
    {
        // GIVEN
        var configFile = Path.Join(rootDir, "foo");
        File.WriteAllText(configFile, "[Desktop Entry]\n");

        // WHEN
        var result = await self.ReadIconFromConfigFile(configFile);

        // THEN
        Assert.IsNull(result);
        testLogRecieved(logger, LogLevel.Debug, "Failed to find Icon in file=" + configFile);
    }

    // ┌────────────────────────────────────────────────────┐
    // │                   Test Get Images                  │
    // └────────────────────────────────────────────────────┘

    [TestMethod]
    public async Task TestGetImage()
    {
        // GIVEN
        var configFile = Path.Join(rootDir, ".directory");
        File.WriteAllText(configFile, "[Desktop Entry]\nIcon=my-icon\n");
        var iconFile = Path.Join(rootDir, "my-icon");
        File.WriteAllText(iconFile, "icon.txt");

        var item = new TestBaseItem(rootDir);
        var type = ImageType.Primary;
        var cancellationToken = new CancellationToken();

        // WHEN
        var result = await self.GetImage(item, type, cancellationToken);

        // THEN
        Assert.IsTrue(result.HasImage);
        Assert.AreEqual(result.Path, iconFile);
    }

    [TestMethod]
    public async Task TestGetImageFailsWhenConfigFileIsMissing()
    {
        // GIVEN
        var configFile = Path.Join(rootDir, ".directory");

        var item = new TestBaseItem(rootDir);
        var type = ImageType.Primary;
        var cancellationToken = new CancellationToken();

        // WHEN
        var result = await self.GetImage(item, type, cancellationToken);

        // THEN
        Assert.IsFalse(result.HasImage);
        testLogRecieved(logger, LogLevel.Debug,
                        "Skipping cover extraction because config=" + configFile + " doesn't exist");
    }

    [TestMethod]
    public async Task TestGetImageFailsIfConfigFileDoesntContainIconValue()
    {
        // GIVEN
        var configFile = Path.Join(rootDir, ".directory");
        File.WriteAllText(configFile, "[Desktop Entry]\n");

        var item = new TestBaseItem(rootDir);
        var type = ImageType.Primary;
        var cancellationToken = new CancellationToken();

        // WHEN
        var result = await self.GetImage(item, type, cancellationToken);

        // THEN
        Assert.IsFalse(result.HasImage);
        testLogRecieved(logger, LogLevel.Debug,
                        "Skipping cover extraction because config=" + configFile + " doesn't contain an icon reference");
    }

    [TestMethod]
    public async Task TestGetImageUsesAbsolutePathIfAlreadyAbsoluteInConfigFile()
    {
        // GIVEN
        var configFile = Path.Join(rootDir, ".directory");
        var iconFile = Path.Join(rootDir, "temp", "my-icon");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(iconFile));
        File.WriteAllText(configFile, "[Desktop Entry]\nIcon=" + iconFile + "\n");
        File.WriteAllText(iconFile, "icon.txt");

        var item = new TestBaseItem(rootDir);
        var type = ImageType.Primary;
        var cancellationToken = new CancellationToken();

        // WHEN
        var result = await self.GetImage(item, type, cancellationToken);

        // THEN
        Assert.IsTrue(result.HasImage);
        Assert.AreEqual(result.Path, iconFile);
    }

    [TestMethod]
    public async Task TestGetImageFailsIfIconValueInConfigFileDoesntExist()
    {
        // GIVEN
        var configFile = Path.Join(rootDir, ".directory");
        var iconFile = Path.Join(rootDir, "my-icon");
        File.WriteAllText(configFile, "[Desktop Entry]\nIcon=my-icon\n");

        var item = new TestBaseItem(rootDir);
        var type = ImageType.Primary;
        var cancellationToken = new CancellationToken();

        // WHEN
        var result = await self.GetImage(item, type, cancellationToken);

        // THEN
        Assert.IsFalse(result.HasImage);
        testLogRecieved(logger, LogLevel.Warning,
                        "Icon file icon=" + iconFile + " from file=" + configFile + " does not exist");
    }
}
