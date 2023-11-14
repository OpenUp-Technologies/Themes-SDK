using System;
using System.Collections.Concurrent;

namespace OpenUp.Utils
{
    /// <summary>
    /// Object representing the task of doing some Unity work on the main thread.
    /// </summary>
    public class UnityTask
    {
        private readonly ConcurrentQueue<Action> actionQueue;

        /// <summary>
        /// A awaitable object that resumes once the work has been completed.
        /// </summary>
        private readonly UnityTaskAwaiter unityTaskAwaiter;

        /// <summary>
        /// Constructs a task that can be run and awaited.
        /// </summary>
        /// <param name="task">The work that has to be done.</param>
        /// <param name="concurrentQueue"></param>
        public UnityTask(Action task, ConcurrentQueue<Action> concurrentQueue)
        {
            actionQueue = concurrentQueue;
            unityTaskAwaiter = new UnityTaskAwaiter(task);
        }

        /// <summary>
        /// Enqueues the work in the <see cref="AsyncHelperEditor.ActionQueue"/>.
        /// </summary>
        private void DoWork()
        {
            actionQueue.Enqueue(unityTaskAwaiter.GetWork);
        }
        
        /// <summary>
        /// Enqueues the work in the <see cref="AsyncHelperEditor.ActionQueue"/>
        /// and returns an awaitable object.
        /// </summary>
        /// <returns>A custom awaitable object that resumes once the work has been done.</returns>
        /// <remarks>Allows the <see cref="UnityTask"/> to be awaited</remarks>
        public UnityTaskAwaiter GetAwaiter()
        {
            DoWork();
            
            return unityTaskAwaiter;
        }
    }
}