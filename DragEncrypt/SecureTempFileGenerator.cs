using System.Linq;

namespace DragEncrypt
{
    public class SecureTempFileGenerator : TempFileGenerator
    {
        public override void Dispose()
        {
            foreach (var tuple in Collection.Where(tuple => !tuple.Item2))
            {
                Core.SafeOverwriteFile(tuple.Item1);
            }
            base.Dispose();
        }
    }
}