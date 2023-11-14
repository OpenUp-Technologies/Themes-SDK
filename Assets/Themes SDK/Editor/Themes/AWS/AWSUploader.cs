using System;
using System.Net.Http;
using System.Threading.Tasks;

using UnityEngine;

namespace OpenUp.Networking.AWS
{
    // This stuff's a bit of a mess, apologies..
    public static class Uploader
    {
        private static readonly HttpClient HTTPClient = new HttpClient();

        /// <summary>
        /// Uploads the asset to AWS.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="caller"></param>
        /// <exception cref="HttpRequestException"></exception>
        public static async Task UploadAsset(UploadTask task, SimpleApiCaller caller)
        {
            try
            {
                // Get signing URLs
                task.status = UploadTask.Status.REQUESTING_PERMISSION;

                Task[] getUrlTasks = new Task[task.files.Length];

                async Task SetUrl(AWSDataSource file)
                {
                    string url = await caller.PostAsync<string>(
                        "assets/signedurl",
                        new { Bucket = task.bucket, Key = $"{task.prefix}/{file.name}" }
                    );

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
                    Uri           uri     = new Uri(task.uploadLinks[file]);
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
