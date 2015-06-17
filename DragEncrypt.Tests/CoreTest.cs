using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DragEncrypt
{
    [TestFixture]
    class CoreTest
    {
        private const string TestDirectory = "DragEncrypt-tests/";

        [SetUp]
        public void Init()
        {
            Directory.CreateDirectory(TestDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(TestDirectory,true);
        }

        [Test]
        public void SafeOverwriteFile_OverwriteTestFile_EqualOrBiggerLengthToOriginalFile()
        {
            //arrange
            var file = Core.CreateRandomFilledFile(TestDirectory + "file");
            //action
            Core.SafeOverwriteFile(file);
            //assert
            Assert.GreaterOrEqual(file.Length, file.Length);
        }

        [Test]
        public void SafeOverwriteFile_OverwriteTestFile_OverwritesAsEmptyFile()
        {
            //arrange
            var file = Core.CreateRandomFilledFile(TestDirectory + "file");
            //action
            Core.SafeOverwriteFile(file);
            //assert
            using (var fs = file.OpenRead())
            {
                while (fs.Position < fs.Length)
                {
                    Assert.AreEqual(fs.ReadByte(), 0);
                }
            }
        }
    }
}
