using System;
using System.CodeDom.Compiler;
using System.IO;

namespace DragEncrypt
{
    public class SecureTempFileGenerator : TempFileGenerator
    {
        public SecureTempFileGenerator(string extension = "tmp") : base(extension)
        {
        }

        public SecureTempFileGenerator(string tempDir, string extension = "tmp") : base(tempDir, extension)
        {
        }

        public SecureTempFileGenerator(string tempDir, bool keepFiles, string extension = "tmp") : base(tempDir, keepFiles, extension)
        {
        }

        public new void Dispose()
        {
            foreach (var file in _collection)
            {
                Core.SafeOverwriteFile(new FileInfo((string)file));
            }
            ((IDisposable) _collection).Dispose();
        }
    }
}