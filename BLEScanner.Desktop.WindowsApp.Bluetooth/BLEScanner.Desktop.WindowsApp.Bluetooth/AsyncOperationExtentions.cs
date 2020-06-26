using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace BLEScanner.Desktop.WindowsApp.Bluetooth
{
    //Provides helper methods for Async
    public static class AsyncOperationExtentions
    {
        //Converts Async operation into task
        public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> operation)
        {
            var tcs = new TaskCompletionSource<TResult>();

            operation.Completed += delegate
            {
                switch (operation.Status)
                {
                    //Operations Completed
                    case AsyncStatus.Completed: //Set result
                        tcs.TrySetResult(operation.GetResults());
                        break;
                    //Operation Error
                    case AsyncStatus.Error: //Set Exception
                        tcs.TrySetException(operation.ErrorCode);
                        break;
                    //Operation Canceled
                    case AsyncStatus.Canceled: //Set Canceled
                        tcs.SetCanceled();
                        break;
                }
            };
            //returns task
            return tcs.Task;
        }
    }
}
