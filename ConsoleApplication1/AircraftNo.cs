using System.Collections.Generic;

namespace ConsoleApplication1
{
    public class FlightNum
    {
        public Dictionary<string, FlightInfo2> segs { get; set; }
    }

    public class FlightInfo2
    {
        public string co { get; set; }
        public string ca { get; set; }

        public string pt { get; set; }
    }
}