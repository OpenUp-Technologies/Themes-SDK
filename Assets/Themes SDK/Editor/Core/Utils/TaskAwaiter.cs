using System;
using System.Collections;
using System.Runtime.CompilerServices;

namespace OpenUp.Utils
{
    // TODO: This could be expanded upon to allow return values.
    /// <summary>
    /// Object that informs a <see cref="UnityTask"/> that its work has been completed.
    /// </summary>
    public class UnityTaskAwaiter : INotifyCompletion
    {
        /// <summary>
        /// <c>true</c> if the task is complete, otherwise <c>false</c>.
        /// </summary>
        /// <remarks>Required to be used in <c>await</c> statements.</remarks>
        public bool IsCompleted { get; set; } = false;
        
        /// <summary>
        /// The task that has to be done.
        /// </summary>
        private readonly Action task;
        
        /// <summary>
        /// The coroutine that has to be done and awaited from outside the main thread.
        /// </summary>
        private readonly IEnumerator routine;
        
        /// <summary>
        /// <see cref="Action"/> that is called when the task has been completed.
        /// </summary>
        private Action done;

        /// <summary>
        /// Constructs a <see cref="UnityTaskAwaiter"/> for the given task.
        /// </summary>
        /// <param name="task">The task that has to be completed.</param>
        public UnityTaskAwaiter(Action task)
        {
            this.task = task;
        }

        public UnityTaskAwaiter(IEnumerator routine)
        {
            this.routine = routine;
        }
        
        /// <summary>
        /// This method is used to pass the awaiter what to do after is has finished.
        /// </summary>
        /// <param name="continuation">What to de next.</param>
        public void OnCompleted(Action continuation)
        { 
            done = continuation;
        }
        
        /// <summary>
        /// Gets the result of the work.
        /// </summary>
        /// <remarks>Required to be used in <c>await</c> statements.</remarks>
        public void GetResult() {}

        /// <summary>
        /// This should be passed to the <see cref="CoroutineHelper.ActionQueue"/>, it contains
        /// the instructions to do the work, and continue onto the next task.
        /// </summary>
        public void GetWork()
        {
            task();
            IsCompleted = true;
            done?.Invoke();
        }

        public IEnumerator GetRoutine()
        {
            yield return routine;
            
            IsCompleted = true;
            done?.Invoke();
        }
    }
}