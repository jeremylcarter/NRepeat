using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NRepeat
{
    public static class AwaitExtensionMethods
    {
        public static async Task<T> WithWaitCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            // The tasck completion source. 
            var tcs = new TaskCompletionSource<bool>();

            // Register with the cancellation token.
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
                // If the task waited on is the cancellation token...
                if (task != await Task.WhenAny(task, tcs.Task))
                    throw new OperationCanceledException(cancellationToken);

            // Wait for one or the other to complete.
            return await task;
        }
    }
}
