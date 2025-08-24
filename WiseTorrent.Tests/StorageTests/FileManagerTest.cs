using Moq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Pieces.Types;
using WiseTorrent.Storage.Classes;
using WiseTorrent.Storage.Interfaces;
using WiseTorrent.Storage.Types;

namespace WiseTorrent.Tests.StorageTests
{
    [TestFixture]
    public class FileManagerTests
    {
        private Mock<IDiskAllocator> _mockDiskAllocator;
        private Mock<IFileIO> _mockFileIO;
        private FileMap _fileMap;
        private FileManager _fileManager;
        private CancellationTokenSource _cancellationTokenSource;

        [SetUp]
        public void SetUp()
        {
            _mockDiskAllocator = new Mock<IDiskAllocator>();
            _mockFileIO = new Mock<IFileIO>();
            _cancellationTokenSource = new CancellationTokenSource();

            // Create a FileMap with dummy data
            _fileMap = new FileMap(1024, new List<(string, long)>
            {
                ("testfile.tmp", 2048)
            });

            // Create FileManager
            _fileManager = new FileManager(
                _mockDiskAllocator.Object,
                _mockFileIO.Object,
                _cancellationTokenSource.Token,
                maxPieceQueueSize: 5
            );

            // Mock DiskAllocator
            _mockDiskAllocator
                .Setup(d => d.VerifyAllocation(It.IsAny<string>()))
                .Returns(true);

            // Mock FileIO.WriteAsync to just complete
            _mockFileIO
                .Setup(f => f.WriteAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        [TearDown]
        public void TearDown()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        [Test]
        public void AttachFileMap_ShouldAttachFileMapSuccessfully()
        {
            // Act
            _fileManager.AttachFileMap(_fileMap);

            // Assert
            Assert.DoesNotThrow(() => _fileManager.AttachFileMap(_fileMap), "AttachFileMap should attach the FileMap without errors.");
        }

        [Test]
        public void StartProcessing_ShouldThrowExceptionIfFileMapNotAttached()
        {
            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(() => _fileManager.StartProcessing());
            Assert.AreEqual("FileMap must be attached before starting processing.", ex.Message);
        }

        [Test]
        public void StartProcessing_ShouldThrowExceptionIfAlreadyProcessing()
        {
            // Arrange
            _fileManager.AttachFileMap(_fileMap);
            _fileManager.StartProcessing();

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(() => _fileManager.StartProcessing());
            Assert.AreEqual("File manager is already processing.", ex.Message);
        }

        [Test]
        public async Task ProcessPiece_ShouldEnqueuePiece()
        {
            // Arrange
            var piece = new Piece(0, "dummyHash");

            // Act
            _fileManager.ProcessPiece(piece);

            // Assert
            Assert.AreEqual(1, GetPieceQueueCount(), "Piece should be added to the queue.");
        }

        [Test]
        public async Task WorkerLoop_WritesPiecesToDisk()
        {
            // Arrange
            _fileManager.AttachFileMap(_fileMap);

            // Enqueue two pieces
            var piece0 = new Piece(0, "dummyHash") { Data = new byte[1024] };
            var piece1 = new Piece(1, "dummyHash") { Data = new byte[1024] };
            _fileManager.ProcessPiece(piece0);
            _fileManager.ProcessPiece(piece1);

            // Act
            _fileManager.StartProcessing();

            // Wait to allow worker loop to drain the queue
            await Task.Delay(200);

            // Assert
            _mockFileIO.Verify(
                f => f.WriteAsync(
                    "testfile.tmp",
                    It.IsAny<byte[]>(),
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
                Times.AtLeast(2),
                "Each piece should result in at least one write to disk."
            );
        }

        [Test]
        public async Task FlushPiecesAsync_ShouldWriteAllQueuedPieces()
        {
            // Arrange
            var piece = new Piece(0, "dummyHash") { Data = new byte[1024] };
            _fileManager.AttachFileMap(_fileMap);
            _fileManager.ProcessPiece(piece);

            var segments = _fileMap.Resolve(piece.Index);
            _mockDiskAllocator.Setup(allocator => allocator.VerifyAllocation(It.IsAny<string>())).Returns(true);
            _mockFileIO
                .Setup(fileIO => fileIO.WriteAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _fileManager.FlushPiecesAsync();

            // Assert
            _mockFileIO.Verify(
                fileIO => fileIO.WriteAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce,
                "FlushPiecesAsync should write all queued pieces to disk."
            );
        }

        private int GetPieceQueueCount()
        {
            var pieceQueueField = typeof(FileManager).GetField("pieceQueue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pieceQueue = (ConcurrentQueue<Piece>)pieceQueueField!.GetValue(_fileManager)!;
            return pieceQueue.Count;
        }
    }
}
