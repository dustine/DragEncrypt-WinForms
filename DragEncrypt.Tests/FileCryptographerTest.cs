using System;
using System.IO;
using System.Security.Cryptography;
using DragEncrypt.Properties;
using Newtonsoft.Json;
using NUnit.Framework;

namespace DragEncrypt.Tests
{
    [TestFixture]
    class FileCryptographerTest
    {
        private readonly Randomizer _randomizer = new Randomizer();
        private const string TestDirectory = "DragEncrypt-tests/";
        private FileInfo _originalFile;
        private FileCryptographer _fc;
        private byte[] _key;
        private FileInfo _testFile;

        private byte[] GenerateKey(int bitSize)
        {
            var result = new byte[bitSize/8];
            _randomizer.NextBytes(result);
            return result;
        }

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

            _key = GenerateKey(256);
            _fc = new FileCryptographer();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var file in Directory.EnumerateFiles(TestDirectory))
            {
                File.Delete(file);
            }
            Directory.Delete(TestDirectory);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Encrypt_NoKey_ArgumentErrorException()
        {
            // arrange

            // action
            _fc.EncryptFile(_testFile);

            //// assertion
        }

        [Test]
        public void Encrypt_AnyKey_CreatesFile()
        {
            // arrange
            _fc.HashedKey = _key;
            var encryptedLoc = _testFile.FullName + Settings.Default.Extension;

            // action
            _fc.EncryptFile(_testFile);

            // assertion
            var encryptedFile = new FileInfo(encryptedLoc);
            Assert.IsTrue(encryptedFile.Exists,"createdFile.Exists");
        }

        [Test]
        public void Encrypt_AnyKey_HasValidJsonHeader()
        {
            // arrange
            _fc.HashedKey = _key;
            var encryptedLoc = _testFile.FullName + Settings.Default.Extension;

            // action
            _fc.EncryptFile(_testFile);

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
                json = jsonSerializer.Deserialize(stream,typeof(EncryptionInfo));
            }

            Assert.IsInstanceOf<EncryptionInfo>(json);
            var encryptInfo = (EncryptionInfo) json;

            Assert.IsNotNull(encryptInfo.Version);

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
            Assert.AreEqual(encryptInfo.BlockSize,encryptInfo.Iv.Length*8);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Decrypt_NoKey_ArgumentErrorException()
        {
            // arrange
            _fc.HashedKey = _key;
            _fc.EncryptFile(_testFile);
            _fc.HashedKey = null;

            // action
            _fc.DecryptFile(new FileInfo(_testFile.FullName + Settings.Default.Extension));

            //// assertion
        }

        [Test]
        [ExpectedException(typeof(CryptographicException))]
        public void Decrypt_WrongSizedKey_CryptographicException()
        {
            // arrange
            _fc.HashedKey = _key;
            _fc.EncryptFile(_testFile);
            _fc.HashedKey = GenerateKey(128);

            // action
            _fc.DecryptFile(new FileInfo(_testFile.FullName + Settings.Default.Extension));

            //// assertion
        }

        [Test]
        [ExpectedException(typeof(CryptographicException))]
        public void Decrypt_DifferentKeys_CryptographicException()
        {
            // arrange
            _fc.HashedKey = _key;
            _fc.EncryptFile(_testFile);
            _fc.HashedKey = GenerateKey(256); ;
            // Assert.IsFalse(_key == _fc.HashedKey);

            // action
            _fc.DecryptFile(new FileInfo(_testFile.FullName + Settings.Default.Extension));

            // assertion
        }

        [Test]
        public void Decrypt_ValidKey_GetSameFile()
        {
            // arrange
            _fc = new FileCryptographer { HashedKey = _key };
            _fc.EncryptFile(_testFile);
            _testFile.Delete();

            // action
            _fc.DecryptFile(new FileInfo(_testFile.FullName + Settings.Default.Extension));

            // assertion
            FileAssert.AreEqual(_originalFile, _testFile);
        }
    }
}
