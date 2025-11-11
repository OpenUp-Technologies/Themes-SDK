using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace OpenUp.Networking.AWS
{
    /// <summary>
    /// Represents the task of uploading an asset to the the OpenUp S3 bucket on AWS.
    /// </summary>
    public class UploadTask
    {
        public enum Status
        {
            STARTING,
            REQUESTING_PERMISSION,
            UPLOADING,
            CONFIRMING,
            FINISHED,
            ERRORED
        }
        
        public readonly ConcurrentDictionary<AWSDataSource, UploadUrlResponse> uploadLinks = 
            new ConcurrentDictionary<AWSDataSource, UploadUrlResponse>();

        public readonly string bucket;
        public readonly long totalBytes;
        public readonly AWSDataSource[] files;

        private long _uploadedBytes;
        
        public Action<Exception> OnComplete;

        public long uploadedBytes => _uploadedBytes;
        public string prefix { get; }

        public Status status { get; internal set; } = Status.STARTING;

        public UploadTask(string bucket, string prefix, params AWSDataSource[] files)
        {
            this.bucket = bucket;
            this.prefix = prefix;
            this.files = files;

            totalBytes = files.Sum(f => f.size);
        }

        public void AddLink(AWSDataSource file, UploadUrlResponse link)
        {
            if (!files.Contains(file))
            {
                throw new ArgumentException("Can't add link for file not in upload task");
            }

            uploadLinks.TryAdd(file, link);
        }

        public void ChunkUploaded(int bytesUploaded)
        {
            Interlocked.Add(ref _uploadedBytes, bytesUploaded);
        }
    }
}
