using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace OpenUp.Utils
{
    public static class AsyncHelperEditor
    {
        private static bool hasAddedEditorLoop = false;
        private static readonly ConcurrentQueue<Action> ActionQueue = new ConcurrentQueue<Action>();

        /// <summary>
        /// Awaitable method that allows Unity Main-thread functions to be used outside of the main thread.
        /// Also allows non-crucial work to be batched for later.
        /// </summary>
        /// <param name="action">Action that has the work to do in pieces.</param>
        /// <param name="delay">Optional param that delays the work by the given amount of seconds.</param>
        public static async Task DoUnityWork(Action action, float delay = 0)
        {
            StartEditorBackground();
            
            if (delay > 0) await Task.Delay(Mathf.FloorToInt(delay * 1000));
            
            await new UnityTask(action, ActionQueue);
        }

        public static async Task<T> DoUnityWork<T>(Func<T> func, float delay = 0)
        {
            T result = default(T);

            Action action = () => result = func();

            await DoUnityWork(action, delay);

            return result;
        }


        private static void StartEditorBackground()
        {
            if (hasAddedEditorLoop) return;

            hasAddedEditorLoop = true;
            
            UnityEditor.EditorApplication.playModeStateChanged += change => 
            {
                if (change == UnityEditor.PlayModeStateChange.ExitingEditMode)
                {
                    UnityEditor.EditorApplication.update -= DoUnityWorkInEditor;
                }
                
                hasAddedEditorLoop = false;
            };
            
            UnityEditor.EditorApplication.update += DoUnityWorkInEditor;
        }

        private static void DoUnityWorkInEditor()
        {
            if (Application.isPlaying) return;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            
            while (ActionQueue.TryDequeue(out Action work) && sw.ElapsedMilliseconds < 30)
            {
                work();
            }
            sw.Stop();
        }
    }
}