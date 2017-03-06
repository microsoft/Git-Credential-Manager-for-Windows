using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Authentication.Helpers
{
    public static class TaskExtensions
    {
        public static async Task<TResult> RunWithCancellation<TResult>(this Task<TResult> task, CancellationToken cancellationToken)
        {
            var completedTask = await Task.WhenAny(task, cancellationToken.AsTask());
            if (completedTask == task)
            {
                return await task;  // Very important in order to propagate exceptions
            }
            else
            {
                throw new TaskCanceledException("The operation has been canceled");
            }
        }

        /// <summary>
        /// https://github.com/StephenCleary/AsyncEx
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task AsTask(this CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                //return TaskConstants.Never;
                // TODO should be static?
                return new TaskCompletionSource<bool>().Task;
            }
            if (cancellationToken.IsCancellationRequested)
            {
                //return TaskConstants.Canceled;
                // TODO should be static ?
                TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
                completionSource.SetCanceled();
                return completionSource.Task;
            }

            var tcs = new TaskCompletionSource<object>();
            cancellationToken.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false);
            return tcs.Task;
        }
    }
}
