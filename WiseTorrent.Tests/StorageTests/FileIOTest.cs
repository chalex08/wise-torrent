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
	public class FileIOTest
	{
		private FileIO _fileIO;
		private string _testFilePath;

		[SetUp]
		public void SetUp()
		{
			_fileIO = new FileIO();
            _testFilePath = Path.Combine(Path.GetTempPath(), "testfile.tmp");
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
		public async Task WriteAsync_ShouldWriteDataToFile()
		{
			// Arrange
			byte[] data = Encoding.UTF8.GetBytes("Hello, World!");
			long offset = 0;

			// Act
			await _fileIO.WriteAsync(_testFilePath, data, offset, data.Length);

			// Assert
			string fileContent = File.ReadAllText(_testFilePath);
            Assert.AreEqual("Hello, World!", fileContent, "File content should match the written data.");
        }

        [Test]
        public async Task ReadAsync_ShouldReadDataFromFile()
        {
            // Arrange
            File.WriteAllText(_testFilePath, "Hello, World!");
            byte[] buffer = new byte[20];
            long offset = 0;

            // Act
            int bytesRead = await _fileIO.ReadAsync(_testFilePath, buffer, offset, buffer.Length);

            // Assert
            string readContent = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Assert.AreEqual("Hello, World!", readContent, "Read content should match the file content.");
        }

        [Test]
        public async Task DeleteAsync_ShouldDeleteFile()
        {
			// Arrange
			File.WriteAllText(_testFilePath, "This file will be deleted.");
			Assert.IsTrue(File.Exists(_testFilePath));

            // Act
            await _fileIO.DeleteAsync(_testFilePath);

            // Assert
            Assert.IsFalse(File.Exists(_testFilePath), "File should not exist after deletion.");
        }
    }
}
