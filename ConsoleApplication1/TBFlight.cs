
namespace ConsoleApplication1
{
    public class Rootobject
    {
        public Datum[] data { get; set; }
        public Productitem[] productItems { get; set; }
    }

    public class Datum
    {
        public string uniqKey { get; set; }
        public long depTimeStamp { get; set; }
    }

    public class Productitem
    {
        public int totalAdultPrice { get; set; }
        public Agentinfo agentInfo { get; set; }
    }

    public class Agentinfo
    {
        public string showName { get; set; }
    }

}