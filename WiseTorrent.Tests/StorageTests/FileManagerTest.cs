using WiseTorrent.Storage.Classes;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Tests.StorageTests
{
    [TestFixture]
    public class FileManagerIntegrationTests
    {
        private string _testFilePath = Path.Combine(Path.GetTempPath(), "filemanager_test.tmp");
        private FileManager _fileManager;
        private DiskAllocator _diskAllocator;
        private FileIO _fileIO;
        private FileMap _fileMap;

        [SetUp]
        public void SetUp()
        {
            if (File.Exists(_testFilePath))
                File.Delete(_testFilePath);

            _fileIO = new FileIO();
            _diskAllocator = new DiskAllocator(_fileIO);
            _fileManager = new FileManager(_diskAllocator, _fileIO);

            // Create a FileMap for a single file of 64 KB with 16 KB pieces
            _fileMap = new FileMap(
                pieceLength: 16 * 1024,
                files: new List<(string, long)> { (_testFilePath, 64 * 1024) }
            );
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_testFilePath))
                File.Delete(_testFilePath);
        }

        [Test]
        public async Task Writes_FirstPieceBlocks_Correctly()
        {
            // Arrange: 16 KB piece, 64 KB file, but we will write ONLY piece 0.
            var testFile = Path.Combine(Path.GetTempPath(), "fm_singlepiece.tmp");
            if (File.Exists(testFile)) File.Delete(testFile);

            var fileIO = new FileIO();
            var allocator = new DiskAllocator(fileIO);
            var fileManager = new FileManager(allocator, fileIO);

            const int pieceLength = 16 * 1024;
            const int fileSize = 64 * 1024;
            const int blockSize = 4 * 1024;

            var map = new FileMap(pieceLength, new[] { (testFile, (long)fileSize) });

            // Build expected data for piece 0 only
            var fullPattern = new byte[fileSize];
            for (int i = 0; i < fullPattern.Length; i++) fullPattern[i] = (byte)(i % 256);

            // Write only piece 0 in 4 KB blocks
            int pieceIndex = 0;
            for (int pieceOffset = 0; pieceOffset < pieceLength; pieceOffset += blockSize)
            {
                int len = Math.Min(blockSize, pieceLength - pieceOffset);
                var blockData = new byte[len];
                Buffer.BlockCopy(fullPattern, pieceOffset, blockData, 0, len);

                var block = new Block(pieceIndex, pieceOffset, len) { Data = blockData };
                await fileManager.WriteBlockAsync(block, map, CancellationToken.None);
            }

            // Read back only the first 16 KB and compare
            var readBack = new byte[pieceLength];
            using (var fs = new FileStream(testFile, FileMode.Open, FileAccess.Read))
                await fs.ReadAsync(readBack, 0, readBack.Length);

            var expectedFirstPiece = new byte[pieceLength];
            Buffer.BlockCopy(fullPattern, 0, expectedFirstPiece, 0, pieceLength);

            CollectionAssert.AreEqual(expectedFirstPiece, readBack,
                "First piece bytes should match exactly what was written.");
        }

        [Test]
        public async Task Writes_AllPieces_AllBlocks_EndToEnd()
        {
            // Arrange
            var testFile = Path.Combine(Path.GetTempPath(), "fm_full_integration.tmp");
            if (File.Exists(testFile)) File.Delete(testFile);

            var fileIO = new FileIO();
            var allocator = new DiskAllocator(fileIO);
            var fileManager = new FileManager(allocator, fileIO);

            const int pieceLength = 16 * 1024;   // 16 KB pieces
            const int fileSize = 64 * 1024;      // 64 KB file (4 pieces)
            const int blockSize = 4 * 1024;      // 4 KB blocks

            var map = new FileMap(pieceLength, new[] { (testFile, (long)fileSize) });

            // Create the expected byte pattern
            var expected = new byte[fileSize];
            for (int i = 0; i < expected.Length; i++) expected[i] = (byte)(i % 256);

            // Act: write ALL pieces, split into 4 KB blocks
            int pieceCount = (fileSize + pieceLength - 1) / pieceLength;

            for (int p = 0; p < pieceCount; p++)
            {
                int pieceStart = p * pieceLength;
                int actualPieceLen = Math.Min(pieceLength, fileSize - pieceStart);

                for (int pieceOffset = 0; pieceOffset < actualPieceLen; pieceOffset += blockSize)
                {
                    int len = Math.Min(blockSize, actualPieceLen - pieceOffset);

                    var blockData = new byte[len];
                    Buffer.BlockCopy(expected, pieceStart + pieceOffset, blockData, 0, len);

                    var block = new Block(p, pieceOffset, len) { Data = blockData };

                    await fileManager.WriteBlockAsync(block, map, CancellationToken.None);
                }
            }

            // Assert: read entire file back and compare
            var actual = new byte[fileSize];
            using (var fs = new FileStream(testFile, FileMode.Open, FileAccess.Read))
                await fs.ReadAsync(actual, 0, actual.Length);

            CollectionAssert.AreEqual(expected, actual, "File contents should match the full expected pattern.");
        }
    }
}
