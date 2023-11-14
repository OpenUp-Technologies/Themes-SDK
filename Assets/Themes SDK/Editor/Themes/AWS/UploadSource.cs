using System.IO;

namespace OpenUp.Networking.AWS 
{
    public class AWSDataSource
    {
        public byte[] data;
        public string name;
        public long size => data.LongLength;

        public static implicit operator AWSDataSource(FileInfo finfo)
        {
            return new AWSDataSource {
                data = File.ReadAllBytes(finfo.FullName),
                name = finfo.Name
            };
        }
    }
}