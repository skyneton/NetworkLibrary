using System;

namespace NetworkLibrary.Utils
{
    public class TimeManager
    {
        public static long CurrentTimeInMillis => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
