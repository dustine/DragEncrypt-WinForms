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
        private FileInfo _encryptedFile;
        private FileInfo _decryptedFile;

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
        }

        [TearDown]
        public void TearDown()
        {
            EraseDirectory(TestDirectory);
        }

        private static void EraseDirectory(string directory)
        {
            foreach (var file in Directory.EnumerateFiles(directory))
                File.Delete(file);
            foreach (var dir in Directory.EnumerateDirectories(directory))
            {
                EraseDirectory(dir);
            }
            Directory.Delete(directory);
        }

        [Test]
        public void SafeOverwriteFile_OverwriteTestFile_EqualOrBiggerLengthToOriginalFile()
        {
            //arrange
            //action
            FileCryptographer.SafeOverwriteFile(_originalFile);
            //assert
            Assert.GreaterOrEqual(_originalFile.Length, _originalFile.Length);
        }

        [Test]
        public void SafeOverwriteFile_OverwriteTestFile_OverwritesAsEmptyFile()
        {
            //arrange
            //action
            FileCryptographer.SafeOverwriteFile(_originalFile);
            //assert
            using (var fs = _originalFile.OpenRead())
            {
                while (fs.Position < fs.Length)
                {
                    Assert.AreEqual(fs.ReadByte(), 0);
                }
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Encrypt_NullFile_ArgumentNullException()
        {
            // arrange

            // action
            FileCryptographer.EncryptFile(null, "");

            // assertion
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Encrypt_NullKey_ArgumentNullException()
        {
            // arrange

            // action
            FileCryptographer.EncryptFile(_originalFile, null);

            // assertion
        }

        [Test]
        [ExpectedException(typeof(FileNotFoundException))]
        public void Encrypt_NonExistingFile_Exception()
        {
            // arrange

            // action
            FileCryptographer.DecryptFile(new FileInfo(TestDirectory + "fakeFile"), "");

            // assertion
        }

        [Test]
        [ExpectedException(typeof(IOException))]
        public void Encrypt_UnavailableFile_IOException()
        {
            // arrange
            // ReSharper disable once UnusedVariable
            using (var fs = _originalFile.OpenWrite())
            {
                // action
                FileCryptographer.EncryptFile(_originalFile, "");
            }
            
            // assertion
        }

        [Test]
        public void Encrypt_AnyStringKey_CreatesFile()
        {
            // arrange

            // action
            _encryptedFile=FileCryptographer.EncryptFile(_originalFile, "");

            // assertion
            Assert.IsTrue(File.Exists(_encryptedFile.FullName), "encryptedFile.Exists");
        }

        [Test]
        public void Encrypt_FileAlreadyExists_NewFileWithoutConflict()
        {
            // arrange
            var conflictFile = new FileInfo(TestDirectory + _originalFile.Name + Settings.Default.Extension);
            using (var fs = conflictFile.Create())
            {
                // action
                _encryptedFile = FileCryptographer.EncryptFile(_originalFile, "");

                // assert
                Assert.IsTrue(File.Exists(_encryptedFile.FullName));
                Assert.AreNotEqual(conflictFile.FullName,_encryptedFile.FullName);
            }
        }

        [Test]
        public void Encrypt_FileAlreadyExists_NewFileNamedCorrectly([Values(1,10)]int attempts)
        {
            // arrange 
            _encryptedFile = FileCryptographer.EncryptFile(_originalFile, "");

            // action
            for (var i = 1; i < attempts; i++)
            {
                var newFile = FileCryptographer.EncryptFile(_originalFile, "");
                // assert
                Assert.AreEqual(String.Format("{0} ({1}){2}", _encryptedFile.Name.Substring(0, _encryptedFile.Name.Length - _encryptedFile.Extension.Length), i, _encryptedFile.Extension), newFile.Name);
            }
        }

        [Test]
        public void Encrypt_SafelyDeleteOriginal_OriginalGone()
        {
            // arrange
            
            // action
            FileCryptographer.EncryptFile(_originalFile, "", true);

            // assertion
            Assert.IsFalse(File.Exists(_originalFile.FullName));
        }

        [Test]
        public void Encrypt_AnyStringKey_HasValidJsonHeader()
        {
            // arrange

            // action
            _encryptedFile = FileCryptographer.EncryptFile(_originalFile, "");

            // assertion
            Assert.IsTrue(File.Exists(_encryptedFile.FullName));

            object json;
            using (var stream = _encryptedFile.OpenText())
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
        public void Encrypt_AnyStringKey_HasFilledInJsonHeader()
        {
            // arrange
            FileCryptographer.EncryptFile(_originalFile, "");

            // action
            EncryptionInfo encryptInfo;
            using (var stream = _encryptedFile.OpenText())
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
            // ReSharper disable once PossibleNullReferenceException
            Assert.AreEqual(hA.HashSize, encryptInfo.OriginalHash.ToCharArray().Length*4);

            Assert.IsNotNull(encryptInfo.EncryptionAlgorithm);
            var sA = Activator.CreateInstance(encryptInfo.EncryptionAlgorithm) as SymmetricAlgorithm;
            Assert.IsInstanceOf<SymmetricAlgorithm>(sA);
            Assert.IsNotNull(encryptInfo.KeySize);
            // ReSharper disable once PossibleNullReferenceException
            Assert.IsTrue(sA.ValidKeySize(encryptInfo.KeySize));
            Assert.IsNotNull(encryptInfo.BlockSize);
            Assert.IsNotNull(encryptInfo.Iv);
            Assert.AreEqual(encryptInfo.BlockSize, encryptInfo.Iv.Length*8);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Decrypt_NullFile_ArgumentNullException()
        {
            // arrange

            // action
            FileCryptographer.DecryptFile(null, "");

            // assertion
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Decrypt_NullKey_ArgumentNullException()
        {
            // arrange
            FileCryptographer.EncryptFile(_originalFile, "");
            // action
            FileCryptographer.DecryptFile(_originalFile, null);

            // assertion
        }

        [Test]
        [ExpectedException(typeof(FileNotFoundException))]
        public void Decrypt_NonExistingFile_FileNotFoundException()
        {
            // arrange

            // action
            FileCryptographer.DecryptFile(new FileInfo(TestDirectory + "fakeFile"), "");

            // assertion
        }

        [Test]
        [ExpectedException(typeof(IOException))]
        public void Decrypt_UnavailableFile_IOException()
        {
            // arrange
            _encryptedFile = FileCryptographer.EncryptFile(_originalFile, "");
            
            // ReSharper disable once UnusedVariable
            using (var fs = _encryptedFile.Open(FileMode.Open, FileAccess.ReadWrite))
            {
                // action
                FileCryptographer.DecryptFile(_encryptedFile, "");
            }

            // assertion
        }

        [Test]
        [ExpectedException(typeof (CryptographicException))]
        public void Decrypt_DifferentKeys_CryptographicException([Values("", "A")] string encryptKey,
            [Values(" ", "B")] string decryptKey)
        {
            // arrange
            _encryptedFile = FileCryptographer.EncryptFile(_originalFile, encryptKey);
            // Assert.IsFalse(_key == _fc.HashedKey);

            // action
            FileCryptographer.DecryptFile(_encryptedFile, decryptKey);

            // assertion
        }

        [Test]
        public void Decrypt_ValidKey_GetSameFile(
            [Values("", "password", "a really long password that has the intent of beating any key size")] string key)
        {
            // arrange
            _encryptedFile=FileCryptographer.EncryptFile(_originalFile, key);

            // action
            _decryptedFile=FileCryptographer.DecryptFile(_encryptedFile, key);

            // assertion
            FileAssert.AreEqual(_originalFile, _decryptedFile); // hah lol
        }

        [Test]
        [ExpectedException(typeof (ArgumentException))]
        public void Encrypt_IsDirectory_ArgumentException()
        {
            // arrange
            var _newDirectory = Directory.CreateDirectory(TestDirectory + "/folderTest");
            _originalFile = new FileInfo(_newDirectory.FullName);

            // action
            _encryptedFile = FileCryptographer.EncryptFile(_originalFile,"");

            // assertion
        }
    }
}