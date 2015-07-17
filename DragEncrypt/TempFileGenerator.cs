using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DragEncrypt
{
    public class TempFileGenerator : IDisposable
    {
        protected readonly List<Tuple<FileInfo, bool>> Collection;

        public TempFileGenerator(bool keepFile = false)
        {
            Collection = new List<Tuple<FileInfo,bool>>();
            DefaultKeepFile = keepFile;
        }

        public bool DefaultKeepFile { get; }

        public virtual void Dispose()
        {
            foreach (var tuple in Collection.Where(tuple => !tuple.Item2))
            {
                tuple.Item1.Delete();
            }
        }

        public FileInfo CreateFile()
        {
            return CreateFile(DefaultKeepFile);
        }

        public FileInfo CreateFile(bool keepFile)
        {
            var fileLocation = Path.GetTempFileName();
            var file = new FileInfo(fileLocation);
            Collection.Add(new Tuple<FileInfo, bool>(file,keepFile));
            return file;
        }
    }
}