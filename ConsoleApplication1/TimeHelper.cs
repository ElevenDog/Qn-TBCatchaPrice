using System;

namespace ConsoleApplication1
{
    public static class TimeHelper
    {
        // 将Unix时间戳（参数为long类型）转换为系统时间
        public static DateTime UnixTimestampToDateTime(long timestamp)
        {
            DateTime time;
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            time = startTime.AddMilliseconds(timestamp);
            return time;
        }
        // 将Unix时间戳(参数为string类型)转换为系统时间
        public static DateTime UnixTimestampToDateTime(string timestamp)
        {
            DateTime time;
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = long.Parse(timestamp + "0000000");
            time = startTime.AddMilliseconds(lTime);
            return time;
        }
        // 将系统时间转换为Unix时间戳
        public static int DateTimeToUnixTimestamp(DateTime time)
        {
            var startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            return (int)(time - startTime).TotalSeconds;
        }

    }
}
