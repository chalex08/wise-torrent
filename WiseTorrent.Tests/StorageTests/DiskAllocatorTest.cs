using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Storage.Classes;
using WiseTorrent.Storage.Interfaces;

namespace WiseTorrent.Tests.StorageTests
{
    [TestFixture]
    public class DiskAllocatorTest
    {
        private Mock<IFileIO> _mockFileIO;
        private DiskAllocator _diskAllocator;
        private string _testFilePath;

        [SetUp]
        public void SetUp()
        {
            _mockFileIO = new Mock<IFileIO>();
            _diskAllocator = new DiskAllocator(_mockFileIO.Object);
            _testFilePath = Path.Combine(Path.GetTempPath(), "testfile.tmp");

            // Mock FileIO.WriteAsync to just complete
            _mockFileIO
                .Setup(f => f.WriteAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Mock FileIO.DeleteAsync to just complete
            _mockFileIO
                .Setup(fileIO => fileIO.DeleteAsync(_testFilePath, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }

        [Test]
        public async Task Allocate_ShouldWriteToFileAtCorrectOffset()
        {
            // Arrange
            long fileSize = 1024; // 1 KB
            _mockFileIO
                .Setup(fileIO => fileIO.WriteAsync(_testFilePath, It.IsAny<Byte[]>(), fileSize - 1, 0, It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Act
            await _diskAllocator.Allocate(_testFilePath, fileSize);

            // Assert
            _mockFileIO.Verify(
                fileIO => fileIO.WriteAsync(_testFilePath, It.IsAny<Byte[]>(), fileSize - 1, 1, It.IsAny<CancellationToken>(), It.IsAny<int>()),
                Times.Once,
                "Allocate should write a single byte at the correct offset to allocate disk space."
            );
        }

        [Test]
        public async Task Allocate_ShouldNotWriteIfFileSizeIsLessThanOne()
        {
            // Arrange
            long fileSize = 0;

            // Act
            await _diskAllocator.Allocate(_testFilePath, fileSize);

            // Assert
            _mockFileIO.Verify(
                fileIO => fileIO.WriteAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<int>()),
                Times.Never,
                "Allocate should not perform any write operation if file size is less than one."
            );
        }

        [Test]
        public async Task Deallocate_ShouldDeleteFile()
        {
            // Act
            await _diskAllocator.Deallocate(_testFilePath);

            // Assert
            _mockFileIO.Verify(
                fileIO => fileIO.DeleteAsync(_testFilePath, It.IsAny<CancellationToken>()),
                Times.Once,
                "Deallocate should call DeleteAsync to remove the file."
            );
        }

        [Test]
        public void VerifyAllocation_ShouldReturnTrueIfFileExists()
        {
            // Arrange
            var data = "Temporary file content";
            var dataLength = data.Length;
            File.WriteAllText(_testFilePath, data);

            // Act
            bool result = _diskAllocator.VerifyAllocation(_testFilePath, dataLength);

            // Assert
            Assert.IsTrue(result, "VerifyAllocation should return true if the file exists with correct allocation size.");
        }

        [Test]
        public void VerifyAllocation_ShouldReturnFalseIfFileDoesNotExist()
        {
            // Act
            bool result = _diskAllocator.VerifyAllocation(_testFilePath, 30);

            // Assert
            Assert.IsFalse(result, "VerifyAllocation should return false if the file does not exist.");
        }

        [Test]
        public void VerifyAllocation_ShouldReturnFalseIfFileIsNotRequiredSize()
        {
            File.WriteAllText(_testFilePath, "Temporary file content");

            // Act
            bool result = _diskAllocator.VerifyAllocation(_testFilePath, 50);

            // Assert
            Assert.IsFalse(result, "VerifyAllocation should return false if the file is not required size");
        }
    }
}
