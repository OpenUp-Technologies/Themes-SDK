using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace OpenUp.Networking.AWS
{
    public class UploadContent : ByteArrayContent
    {
        private const    int CHUNK_SIZE = 4096;
        
        private readonly int totalSize;
        private readonly byte[] data;

        public Action<int> onUploadChunk;
        
        public int uploaded;

        /// <inheritdoc />
        public UploadContent(byte[] data) : base(data)
        {
            totalSize = data.Length;
            this.data = data;
        }

        /// <inheritdoc />
        protected override async Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext context)
        {
            for (int i = 0; i < totalSize; i += CHUNK_SIZE)
            {
                int byteCount = Math.Min(CHUNK_SIZE, totalSize - i);
                
                await stream.WriteAsync(data, i, byteCount);

                uploaded += byteCount;
                
                onUploadChunk?.Invoke(byteCount);
            }
        }
    }
}