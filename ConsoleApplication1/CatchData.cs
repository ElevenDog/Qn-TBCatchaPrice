using System;
using System.Collections.Generic;

namespace ConsoleApplication1
{
    public class CatchData
    {
        public FlightInfo Flight { get; set; }
        public List<FlightAgentInfo> AgentList { get; set; }
    }

    public class FlightAgentInfo
    {
        public int? Id { get; set; }
        public int? FlightId { get; set; }
        public string AgentName { get; set; }
        public int AgentPrice { get; set; }
        public int AgentTax { get; set; }
        public int AgentRank { get; set; }
    }

    public class FlightInfo
    {
        public int? Id { get; set; }
        public int PlatId { get; set; }
        public string AirLine { get; set; }
        public string CarrierCode { get; set; }
        public string DepDate { get; set; }
        public string FlightNum { get; set; }
        public int Rank { get; set; }
        public int Price { get; set; }
        public int Tax { get; set; }
        public string Status { get; set; }
        public DateTime CatchDate { get; set; }
        public int? AdultPrice { get; set; }
        public int? AdultTax { get; set; }
        public int? AdultPriceAdd { get; set; }
        public int? AdultTaxAdd { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string Memo { get; set; }
    }
}