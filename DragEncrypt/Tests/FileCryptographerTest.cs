using System;
using System.IO;
using System.Security.Cryptography;
using DragEncrypt.Properties;
using Newtonsoft.Json;
using NUnit.Framework;

namespace DragEncrypt.Tests
{
    [TestFixture]
    internal class FileCryptographerTest
    {
        private const string TestDirectory = "DragEncrypt-tests/";
        private FileInfo _originalFile;
        private FileInfo _testFile;
        private FileInfo _resultingFile;

        [SetUp]
        public void Init()
        {
            Directory.CreateDirectory(TestDirectory);

            _originalFile = new FileInfo(string.Format("{0}/{1}", TestDirectory, "originalFile"));
            using (var originalFs = _originalFile.Open(FileMode.Create))
            {
                var random = new Random();
                for (var i = 0; i < 1024; i++)
                {
                    var buffer = new byte[1024];
                    random.NextBytes(buffer);
                    originalFs.Write(buffer, 0, buffer.Length);
                }
            }

            File.Copy(string.Format("{0}/{1}", TestDirectory, "originalFile"),
                string.Format("{0}/{1}", TestDirectory, "testFile"));

            _testFile = new FileInfo(string.Format("{0}/{1}", TestDirectory, "testFile"));

            _resultingFile = new FileInfo(_testFile.FullName + Settings.Default.Extension);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var file in Directory.EnumerateFiles(TestDirectory))
                File.Delete(file);
            Directory.Delete(TestDirectory);
        }

        [Test]
        public void Encrypt_AnyKey_CreatesFile()
        {
            // arrange
            var encryptedLoc = _testFile.FullName + Settings.Default.Extension;

            // action
            FileCryptographer.EncryptFile(_testFile, "");

            // assertion
            var encryptedFile = new FileInfo(encryptedLoc);
            Assert.IsTrue(encryptedFile.Exists, "createdFile.Exists");
        }

        [Test]
        public void Encrypt_AnyKey_HasFilledInJsonHeader()
        {
            // arrange
            var encryptedLoc = _testFile.FullName + Settings.Default.Extension;

            // action
            FileCryptographer.EncryptFile(_testFile, "");
            var encryptedFile = new FileInfo(encryptedLoc);

            EncryptionInfo encryptInfo;
            using (var stream = encryptedFile.OpenText())
            {
                var jsonSerializer = new JsonSerializer
                {
                    CheckAdditionalContent = false,
                    MissingMemberHandling = MissingMemberHandling.Error
                };
                encryptInfo = (EncryptionInfo) jsonSerializer.Deserialize(stream, typeof (EncryptionInfo));
            }

            // assertion
            Assert.IsNotNull(encryptInfo.Version);

            Assert.IsNotNull(encryptInfo.SaltSize);
            Assert.IsNotNull(encryptInfo.Salt);
            Assert.IsTrue(encryptInfo.Salt.Length == encryptInfo.SaltSize/8);

            Assert.IsNotNull(encryptInfo.HashAlgorithm);
            var hA = Activator.CreateInstance(encryptInfo.HashAlgorithm) as HashAlgorithm;
            Assert.IsInstanceOf<HashAlgorithm>(hA);
            Assert.IsNotNull(encryptInfo.OriginalHash);
            // Only *4 as the hashes are saved under a verboxe hexadecimal format
            //  so one character is half a byte
            Assert.AreEqual(hA.HashSize, encryptInfo.OriginalHash.ToCharArray().Length*4);

            Assert.IsNotNull(encryptInfo.EncryptionAlgorithm);
            var sA = Activator.CreateInstance(encryptInfo.EncryptionAlgorithm) as SymmetricAlgorithm;
            Assert.IsInstanceOf<SymmetricAlgorithm>(sA);
            Assert.IsNotNull(encryptInfo.KeySize);
            Assert.IsTrue(sA.ValidKeySize(encryptInfo.KeySize));
            Assert.IsNotNull(encryptInfo.BlockSize);
            Assert.IsNotNull(encryptInfo.Iv);
            Assert.AreEqual(encryptInfo.BlockSize, encryptInfo.Iv.Length*8);
        }

        [Test]
        public void Encrypt_AnyKey_HasValidJsonHeader()
        {
            // arrange
            var encryptedLoc = _testFile.FullName + Settings.Default.Extension;

            // action
            FileCryptographer.EncryptFile(_testFile, "");

            // assertion
            var encryptedFile = new FileInfo(encryptedLoc);
            Assert.IsTrue(encryptedFile.Exists);

            object json;
            using (var stream = encryptedFile.OpenText())
            {
                var jsonSerializer = new JsonSerializer
                {
                    CheckAdditionalContent = false,
                    MissingMemberHandling = MissingMemberHandling.Error
                };
                json = jsonSerializer.Deserialize(stream, typeof (EncryptionInfo));
            }

            Assert.IsInstanceOf<EncryptionInfo>(json);
        }

        [Test]
        [ExpectedException(typeof (CryptographicException))]
        public void Decrypt_DifferentKeys_CryptographicException([Values("", "A")] string encryptKey,
            [Values(" ", "B")] string decryptKey)
        {
            // arrange
            FileCryptographer.EncryptFile(_testFile, encryptKey);
            // Assert.IsFalse(_key == _fc.HashedKey);

            // action
            FileCryptographer.DecryptFile(_resultingFile, decryptKey);

            // assertion
        }

        [Test]
        public void Decrypt_ValidKey_GetSameFile(
            [Values("", "password", "a really long password that has the intent of beating any key size")] string key)
        {
            // arrange
            FileCryptographer.EncryptFile(_testFile, key);
            _testFile.Delete();

            // action
            FileCryptographer.DecryptFile(_resultingFile, key);

            // assertion
            FileAssert.AreEqual(_originalFile, _testFile);
        }

        [Test]
        public void SafeOverwriteFile_OverwriteTestFile_EqualOrBiggerLength()
        {
            //arrange
            //action
            FileCryptographer.SafeOverwriteFile(_testFile);
            //assert
            Assert.GreaterOrEqual(_testFile.Length,_originalFile.Length);
        }

        [Test]
        public void SafeOverwriteFile_OverwriteTestFile_EmptyFile()
        {
            //arrange
            //action
            FileCryptographer.SafeOverwriteFile(_testFile);
            //assert
            using (var fs = _testFile.OpenRead())
            {
                while (fs.Position < fs.Length)
                {
                    Assert.AreEqual(fs.ReadByte(), 0);
                }
            }
        }
    }
}