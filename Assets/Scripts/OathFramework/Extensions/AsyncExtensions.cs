using System;
using System.Threading.Tasks;

namespace OathFramework.Extensions 
{

    public static class AsyncExtensions 
    { 

        public static async Task AwaitWithTimeout(this Task task, int timeout, Action success, Action error)
        {
            if(await Task.WhenAny(task, Task.Delay(timeout)) == task) {
                success();
            } else {
                error();
            }
        }

    }

}
