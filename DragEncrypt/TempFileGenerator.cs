using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DragEncrypt
{
    public class TempFileGenerator : IDisposable
    {
        private readonly List<Tuple<FileInfo, bool>> _collection;
        private readonly bool _globalKeepFiles;

        public TempFileGenerator(bool keepFiles = false)
        {
            _collection = new List<Tuple<FileInfo,bool>>();
            _globalKeepFiles = keepFiles;
        }

        public void Dispose()
        {
            if (_globalKeepFiles) return;
            foreach (var tuple in _collection.Where(tuple => !tuple.Item2))
            {
                tuple.Item1.Delete();
            }
        }

        public FileInfo CreateFile(bool keepFile = false)
        {
            var fileLocation = Path.GetTempFileName();
            var file = new FileInfo(fileLocation);
            _collection.Add(new Tuple<FileInfo, bool>(file,keepFile));
            return file;
        }
    }
}