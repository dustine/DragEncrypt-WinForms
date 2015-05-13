using System;
using System.CodeDom.Compiler;
using System.IO;

namespace DragEncrypt
{
    public class TempFileInfoGenerator : IDisposable
    {
        private int _count;
        private readonly TempFileCollection _collection;
        public string Extension { get; private set; }

        public TempFileInfoGenerator(string extension = "tmp")
        {
            _collection = new TempFileCollection();
            Extension = extension;
        }

        public TempFileInfoGenerator(string tempDir, string extension = "tmp")
        {
            _collection = new TempFileCollection(tempDir);
            Extension = extension;
        }

        public TempFileInfoGenerator(string tempDir, bool keepFiles, string extension = "tmp")
        {
            _collection = new TempFileCollection(tempDir, keepFiles);
            Extension = extension;
        }

        public void Dispose()
        {
            ((IDisposable)_collection).Dispose();
        }

        /// <summary>
        /// Creates an unique temporary file and returns their FileInfo.
        /// The file is named as follows, with N being an incremental numeric identifier:
        /// N-<paramref name="name"/>.<value>Extension</value>
        /// </summary>
        /// <param name="name">The name component of the newly created file</param>
        /// <param name="keepFile">If set to true, when the generator is disposed the file isn't disposed to as well</param>
        /// <returns>The fileinfo of</returns>
        public FileInfo CreateFile(string name="", bool keepFile=false)
        {
            if (String.IsNullOrWhiteSpace(name)) name = GetHashCode().ToString();
            var extension = String.Format("{0}.{1}.{2}", name, _count++, Extension);
            var filename = _collection.AddExtension(extension);
            var fileInfo = new FileInfo(filename);
            fileInfo.Create().Close();
            return fileInfo;
        }
    }
}
