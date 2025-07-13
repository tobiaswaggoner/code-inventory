using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using CodeInventory.Backend.Services;

namespace CodeInventory.Backend.Tests.Services;

[TestFixture]
public class RepomixServiceTests
{
    private ILogger<RepomixService> _logger;
    private RepomixService _service;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<RepomixService>>();
        _service = new RepomixService(_logger);
    }

    [Test]
    public void GenerateRepomixOutputAsync_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _service.GenerateRepomixOutputAsync(null!));
        Assert.That(ex.ParamName, Is.EqualTo("repositoryPath"));
    }

    [Test]
    public void GenerateRepomixOutputAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _service.GenerateRepomixOutputAsync(""));
        Assert.That(ex.ParamName, Is.EqualTo("repositoryPath"));
    }

    [Test]
    public async Task GenerateRepomixOutputAsync_WithNonExistentPath_ReturnsFailure()
    {
        // Arrange
        var nonExistentPath = "/path/that/does/not/exist";

        // Act
        var result = await _service.GenerateRepomixOutputAsync(nonExistentPath);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Contains.Substring("does not exist"));
    }

    [Test]
    public async Task IsRepomixAvailableAsync_ReturnsBoolean()
    {
        // Act
        var result = await _service.IsRepomixAvailableAsync();

        // Assert
        Assert.That(result, Is.TypeOf<bool>());
        // Note: The actual result depends on whether npx/repomix is installed
    }

    [Test]
    public async Task CreateRepomixIgnoreFileAsync_WithNullPath_HandlesGracefully()
    {
        // Act & Assert - Should not throw
        await _service.CreateRepomixIgnoreFileAsync(null!);
        await _service.CreateRepomixIgnoreFileAsync("");
    }

    [Test]
    public async Task CreateRepomixIgnoreFileAsync_WithValidPath_CompletesSuccessfully()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var testDir = Path.Combine(tempDir, "test-repomix-ignore");
        
        try
        {
            Directory.CreateDirectory(testDir);

            // Act
            await _service.CreateRepomixIgnoreFileAsync(testDir);

            // Assert
            var repomixIgnorePath = Path.Combine(testDir, ".repomixignore");
            Assert.That(File.Exists(repomixIgnorePath), Is.True);
            
            var content = await File.ReadAllTextAsync(repomixIgnorePath);
            Assert.That(content, Contains.Substring("**/*.css"));
            Assert.That(content, Contains.Substring("**/*.css.map"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDir))
                Directory.Delete(testDir, true);
        }
    }
}