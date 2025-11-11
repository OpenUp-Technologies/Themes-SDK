using System;
using System.Net.Http;
using System.Threading.Tasks;

using UnityEngine;

namespace OpenUp.Networking.AWS
{
    /// <summary>
    /// Model that the server provides for upload requests
    /// </summary>
    /// <param name="UploadUrl"></param>
    /// <param name="ResourceUrl"></param>
    public record UploadUrlResponse(string UploadUrl, string ResourceUrl);
    
    public interface IUrlSource
    {
        Task<UploadUrlResponse> GetUrl(string fileName);
    }

    
    // This stuff's a bit of a mess, apologies...
    public static class Uploader
    {
        private static readonly HttpClient HTTPClient = new HttpClient();

        /// <summary>
        /// Uploads the asset to AWS.
        /// </summary>
        /// <param name="task">The stuff to upload.</param>
        /// <param name="source">A UrlProvider that can generate a URL for each file.</param>
        /// <exception cref="HttpRequestException"></exception>
        public static async Task UploadAsset(UploadTask task, IUrlSource source)
        {
            HTTPClient.Timeout = TimeSpan.FromHours(1);
            
            try
            {
                // Get signing URLs
                task.status = UploadTask.Status.REQUESTING_PERMISSION;

                Task[] getUrlTasks = new Task[task.files.Length];

                async Task SetUrl(AWSDataSource file)
                {
                    var url = await source.GetUrl(file.name);

                    task.AddLink(file, url);
                }

                for (int i = 0; i < task.files.Length; i++)
                {
                    getUrlTasks[i] = SetUrl(task.files[i]);
                }

                await Task.WhenAll(getUrlTasks);
                // end Get signing URLs

                // Upload stuff
                task.status = UploadTask.Status.UPLOADING;

                async Task Upload(AWSDataSource file)
                {
                    Uri           uri     = new Uri(task.uploadLinks[file].UploadUrl);
                    byte[]        data    = file.data;
                    UploadContent content = new UploadContent(data);

                    content.onUploadChunk += task.ChunkUploaded;
                    content.Headers.Add("x-amz-acl", "public-read");

                    HttpResponseMessage response = await HTTPClient.PutAsync(uri, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"Failed to upload '{file.name}'. Response:\n{response}");
                    }
                }

                foreach (AWSDataSource file in task.files)
                {
                    await Upload(file);
                }

                // Send Asset data back to server.
                task.status = UploadTask.Status.CONFIRMING;
                task.status = UploadTask.Status.FINISHED;

            }
            catch (Exception exception)
            {
                task.status = UploadTask.Status.ERRORED;

                Debug.LogException(exception);
            }
            finally
            {
                task.OnComplete?.Invoke(null);
            }
        }
    }
}
