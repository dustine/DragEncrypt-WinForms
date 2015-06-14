using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DragEncrypt.Tests
{
    [TestFixture]
    class CoreTest
    {
        private FileInfo _originalFile;
        private const string TestDirectory = "DragEncrypt-tests/";

        [SetUp]
        public void Init()
        {
            var dir = Directory.CreateDirectory(TestDirectory);

            _originalFile = Core.CreateRandomFile($"{TestDirectory}/originalFile");
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(TestDirectory, true);
        }

        [Test]
        public void SafeOverwriteFile_OverwriteTestFile_EqualOrBiggerLengthToOriginalFile()
        {
            //arrange
            //action
            Core.SafeOverwriteFile(_originalFile);
            //assert
            Assert.GreaterOrEqual(_originalFile.Length, _originalFile.Length);
        }

        [Test]
        public void SafeOverwriteFile_OverwriteTestFile_OverwritesAsEmptyFile()
        {
            //arrange
            //action
            Core.SafeOverwriteFile(_originalFile);
            //assert
            using (var fs = _originalFile.OpenRead())
            {
                while (fs.Position < fs.Length)
                {
                    Assert.AreEqual(fs.ReadByte(), 0);
                }
            }
        }
    }
}
