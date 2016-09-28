using System;
using System.Web.Script.Serialization;

namespace ConsoleApplication1
{
    public static class JsonHelper
    {
        // 从一个对象信息生成Json串
        public static string ObjectToJson(object obj)
        {
            var jss = new JavaScriptSerializer();
            jss.MaxJsonLength = int.MaxValue;
            jss.RecursionLimit = int.MaxValue;
            return jss.Serialize(obj);
        }

        // 从一个Json串生成对象信息
        public static T JsonToObject<T>(string json) where T : new()
        {
            try
            {
                var jss = new JavaScriptSerializer();
                jss.MaxJsonLength = int.MaxValue;
                jss.RecursionLimit = int.MaxValue;
                return jss.Deserialize<T>(json);
            }
            catch (Exception)
            {
                return new T();
            }
        }
    }
}