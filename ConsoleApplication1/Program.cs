using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace ConsoleApplication1
{
    internal class Program
    {
        public static void Main()
        {
            while (true)
            {
                // 取任务
                string linejson;
                var platid = TaskLineInfo(out linejson);
                string time;        //出发时间
                string defCity;     //出发城市
                string arrCity;     //目的城市
                string hs;          //航司码
                // 取搜索参数
                SearchParams(linejson, out time, out defCity, out arrCity, out hs);
                if (platid != 1)
                    // 去哪抓价格
                    RequestPrice(time, defCity, arrCity, platid, hs);
                else
                    // 淘宝抓价格
                    GetPrice(time, defCity, arrCity, hs, platid);
            }
        }

        #region TB

        /// <summary>
        /// 淘宝取价
        /// </summary>
        /// <param name="time">出发时间</param>
        /// <param name="defCity">出发城市码</param>
        /// <param name="arrCity">目的城市码</param>
        /// <param name="hs">航司</param>
        /// <param name="platid">平台</param>
        private static void GetPrice(string time, string defCity, string arrCity, string hs, int platid)
        {
            var data = new CatchData();
            data.Flight = new FlightInfo();
            data.Flight.DepDate = time;
            data.Flight.CatchDate = DateTime.Now;
            data.Flight.AirLine = defCity + arrCity;
            data.Flight.PlatId = 1;

            var str = String.Empty;

            var url = String.Format(
                    "https://sijipiao.alitrip.com/ie/flight_search_result_poller.do?_ksTS=1474957255322_582&callback=jsonp583&supportMultiTrip=true&searchBy=1280&searchJourney=%5B%7B\"arrCityCode\"%3A\"{0}\"%2C\"arrCityName\"%3A\"%25E6%25BE%25B3%25E9%2597%25A8\"%2C\"depCityCode\"%3A\"{1}\"%2C\"depCityName\"%3A\"%25E5%25B9%25BF%25E5%25B7%259E\"%2C\"depDate\"%3A\"{2}\"%2C\"selectedFlights\"%3A%5B%5D%7D%5D&tripType=0&searchCabinType=0&agentId=-1&searchMode=0&b2g=0&formNo=-1&cardId=&needMemberPrice=true",
                    arrCity, defCity, time);
            // 取航班号相关数据
            str = TryTimeSpan(str, url);
            var index1 = str.IndexOf("flightItems", StringComparison.Ordinal);
            var index2 = str.IndexOf("acceptFilters", StringComparison.Ordinal);
            var start = str.Substring(index1 + 14);
            var end = str.Substring(index2);
            str = start.Replace(end, "");
            str = "{\"data\":[" + str.Substring(0, str.Length - 3) + "]}";
            var obj = JsonHelper.JsonToObject<Rootobject>(str);
            // 取航班下的代理信息及价格信息
            foreach (var item in obj.data)
            {
                var uniqKey = item.uniqKey;
                if (uniqKey.Contains(defCity + arrCity) && hs.Contains(uniqKey.Substring(0, 2)))
                {
                    var flightNo = uniqKey.Replace(uniqKey.Substring(uniqKey.IndexOf(defCity + arrCity, StringComparison.Ordinal)), "");
                    Console.WriteLine("-----------------------------{0}------------------------", flightNo);
                    data.Flight.FlightNum = flightNo;
                    data.Flight.CarrierCode = uniqKey.Substring(0, 2);
                    data.AgentList = new List<FlightAgentInfo>();
                    var startTime = TimeHelper.UnixTimestampToDateTime(item.depTimeStamp).ToShortTimeString().Replace(":", "%3A");
                    startTime = time + "%20" + startTime + "%3A00";

                    url = String.Format(
                    "https://sijipiao.alitrip.com/ie/flight_search_result_poller.do?plk=%2522eyJfcWlkIjoiMGE2N2JjYWUxNDc0OTY1NTAyMzk4NDg1MGUiLCJfc2tleSI6IjE5MmVkMGNlNzhlODQwMjJhZDFlOWI2NDE0ZjM5ZTVjIn0%253D%2522&_ksTS=1474965493066_2274&callback=jsonp2275&supportMultiTrip=true&searchBy=1280&searchJourney=%5B%7B%22arrCityCode%22%3A%22{0}%22%2C%22arrCityName%22%3A%22%25E5%2590%2589%25E9%259A%2586%25E5%259D%25A1%22%2C%22depCityCode%22%3A%22{1}%22%2C%22depCityName%22%3A%22%25E5%25B9%25BF%25E5%25B7%259E%22%2C%22depDate%22%3A%22{2}%22%2C%22selectedFlights%22%3A%5B%7B%22marketFlightNo%22%3A%22{3}%22%2C%22flightTime%22%3A%22{4}%22%7D%5D%7D%5D&tripType=0&searchCabinType=0&agentId=-1&searchMode=2&b2g=0&formNo=-1&cardId=&needMemberPrice=true", arrCity, defCity, time, flightNo, startTime);
                    var json = String.Empty;
                    // 提取与价格相关数据
                    json = TryTimeSpan(json, url);
                    index1 = json.IndexOf("productItems", StringComparison.Ordinal);
                    index2 = json.IndexOf("flightInfos", StringComparison.Ordinal);
                    start = json.Substring(index1 - 1);
                    end = json.Substring(index2 - 2);
                    json = start.Replace(end, "");
                    json = "{" + json + "}";
                    // 将json字符串转为对象obj
                    obj = JsonHelper.JsonToObject<Rootobject>(json);
                    int i = 0;
                    // 循环取代理价格
                    foreach (var pro in obj.productItems)
                    {
                        i++;
                        data.AgentList.Add(new FlightAgentInfo()
                        {
                            AgentPrice = pro.totalAdultPrice,
                            AgentName = pro.agentInfo.showName,
                            AgentRank = i
                        });
                        // 提取"一路无忧"的价格和排名
                        if (pro.agentInfo.showName.Contains("一路无忧"))
                        {
                            data.Flight.Rank = i;
                            data.Flight.Price = pro.totalAdultPrice / 100;
                        }
                        Console.WriteLine(pro.agentInfo.showName + "----" + pro.totalAdultPrice / 100 + "----Rank----" + i);
                    }

                    if (data.AgentList.Count != 0)
                    {
                        //序列化对象data
                        str = JsonHelper.ObjectToJson(data);
                        //传数据
                        SendDataToAdjustPrice(str, platid);
                    }
                }
            }
        }

        /// <summary>
        /// 尝试多次请求
        /// </summary>
        /// <param name="str">请求结果</param>
        /// <param name="url">请求地址</param>
        /// <returns></returns>
        private static string TryTimeSpan(string str, string url)
        {
            int tryTime = 3;
            while (tryTime > 0)
            {
                str = GetString(url);
                if (str != null) break;
                tryTime--;
            }
            return str;
        }

        #endregion

        #region QN

        /// <summary>
        ///     请求取价
        /// </summary>
        /// <param name="time">出发时间</param>
        /// <param name="defCity">出发城市码</param>
        /// <param name="arrCity">目的城市码</param>
        /// <param name="platid">平台</param>
        /// <param name="hs">航司</param>
        private static void RequestPrice(string time, string defCity, string arrCity, int platid, string hs)
        {
            // 对象
            Dictionary<string, object> obj;
            // 请求地址
            string url;
            // 响应内容
            string str;
            // cookie内容
            var cc = new CookieContainer();
            var tryTime = 3;
            Dictionary<string, FlightNum> flightInfo = null;
            var data = new CatchData();
            data.Flight = new FlightInfo();
            data.Flight.DepDate = time;
            data.Flight.CatchDate = DateTime.Now;
            data.Flight.AirLine = defCity + arrCity;
            data.Flight.PlatId = platid;
            // 取航班信息
            while (tryTime > 0)
            {
                obj = GetFlightNo(defCity, arrCity, time, hs);
                var aircraft = JsonHelper.ObjectToJson(obj);
                flightInfo = JsonHelper.JsonToObject<Dictionary<string, FlightNum>>(aircraft);
                if (flightInfo.Count != 0) break;
                tryTime--;
            }

            #region 循环请求航班取价

            for (var i = 0; i < flightInfo.Count; i++)
            {
                // 航班号
                var flightNo = flightInfo.Values.ToList()[i].segs.Values.ToList()[0].co;
                Console.WriteLine("-----------------------取到航班号：{0}--------------------", flightNo);
                // 请求退改签URL参数：pt
                //var pt = flightInfo.Values.ToList()[i].segs.Values.ToList()[0].pt;

                data.AgentList = new List<FlightAgentInfo>();
                data.Flight.FlightNum = flightNo;
                data.Flight.CarrierCode = flightInfo.Values.ToList()[i].segs.Values.ToList()[0].ca;
                url = "http://www.qunar.com";
                GetString(url, cc);
                url = "http://www.qunar.com/twell/cookie/allocateCookie.jsp";
                GetString(url, cc);
                // 代理、价格信息字符串
                object pdata = null;
                while (tryTime > 0)
                {
                    url =
                        "http://flight.qunar.com/twelli/flight/tags/onewayflight_groupinfo_inter.jsp?&departureCity={0}&arrivalCity={1}&departureDate={2}&returnDate={2}&nextNDays=0&searchType=OneWayFlight&searchLangs=zh&prePay=true&lowReservation=true&locale=zh&from=zdzl&lowestPrice=null&mergeFlag=0&ftime=&fcarrier={3}&fdirect=true&fcity=&fplaneType=&farrAirport=&fdepAirport=&queryID=10.88.170.0%3A-7a4dcf57%3A157416fb45e%3A-793b&serverIP=4hMfchclOOg3ioyJGdUxOxYCG4QhuEQLrX93DLV9rQgVA3TSvGLpcQ%3D%3D&status=1474272625966&_token=57124&deduce=true&flightCode={4}%7C{2}&tabKey=ap&actCode=";
                    url = string.Format(url, defCity, arrCity, time, hs, flightNo);
                    str = GetString(url);
                    str = str.Substring(str.IndexOf("(", StringComparison.Ordinal) + 1).Replace(")", "");
                    obj = JsonHelper.JsonToObject<Dictionary<string, object>>(str);
                    pdata = str.Contains("priceData") ? obj["priceData"] : null;
                    if (pdata != null)
                        break;
                    tryTime--;
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                }
                if (pdata == null)
                {
                    Console.WriteLine("No Price---------" + flightNo);
                    continue;
                }
                // TKP-小骆驼，LPP-低价特惠套餐，LPN-低价特惠
                var js = JsonHelper.ObjectToJson(pdata);
                // priceData 位置
                var p = js.Substring(js.IndexOf("A\":", StringComparison.Ordinal) + 3);
                var res = p.Substring(0, p.Length - 2);
                var priceData = JsonHelper.JsonToObject<Dictionary<string, FlightBagPrice>>(res);
                if (priceData.Count == 0)
                {
                    Console.WriteLine("-------------- No Price Data !!!!!!!!!!!!!!!!!!!!!!!!! " + flightNo);
                    continue;
                }
                // 需要排除的字符
                var excluedList = new[] { "TPK", "LPP", "LPN" };
                foreach (var flightBagPrice in priceData)
                {
                    var flagstr = flightBagPrice.Key;
                    var bagPrice = flightBagPrice.Value;
                    //var warrpid = flagstr.Split('_')[0];
                    //var cabin = bagPrice.cabin;
                    // 筛选出符合条件的代理的价格
                    if (flagstr.StartsWith("tt"))
                    {
                        if (excluedList.Any(flagstr.Contains) || (bagPrice.pinfo == null))
                            continue;
                        // 代理名
                        var pro = bagPrice.pack == 0 ? "代理" : "打包:" + bagPrice.packWrapperName;
                        //var tag = platid == 2 ? "。T" : "。Z";
                        //// 退改签
                        //var tgq = string.Empty;
                        //while (tryTime > 0)
                        //{
                        //    tgq = Tgq(warrpid, bagPrice.pinfo, defCity, arrCity, time, flightNo, cc, pt, cabin);
                        //    if (!tgq.Contains("isLimit"))
                        //    {
                        //        Console.WriteLine("Get TGQ Rule....");
                        //        break;
                        //    }
                        //    Console.WriteLine("Not Get TGQ Rule!!!!!!");
                        //    Thread.Sleep(TimeSpan.FromSeconds(5));
                        //    tryTime--;
                        //}

                        ////
                        //var rank = tgq.Contains(tag) ? -1 : 0;
                        // 总价
                        var price = bagPrice.bpr == 0 ? bagPrice.npr + bagPrice.tax : bagPrice.bpr + bagPrice.tax;
                        Console.WriteLine("{0}：价格{1}", pro, price);
                        data.AgentList.Add(new FlightAgentInfo
                        {
                            AgentName = pro,
                            AgentPrice = price,
                            AgentRank = 0
                        });

                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                }

            #endregion 循环请求航班取价

                if (data.AgentList.Count != 0)
                {
                    //序列化对象data
                    str = JsonConvert.SerializeObject(data);
                    //传数据
                    SendDataToAdjustPrice(str, platid);
                }
            }
        }

        /// <summary>
        ///     退改签规则获取
        /// </summary>
        /// <param name="warrpid">URL参数</param>
        /// <param name="pinfo">URL参数</param>
        /// <param name="defCity">出发城市码</param>
        /// <param name="arrCity">目的城市码</param>
        /// <param name="time">出发日期</param>
        /// <param name="flightNo">航班号</param>
        /// <param name="cc">cookie</param>
        /// <param name="pt">URL参数</param>
        /// <param name="cabin">URL仓位参数</param>
        /// <returns></returns>
        private static string Tgq(string warrpid, string pinfo, string defCity, string arrCity,
            string time, string flightNo, CookieContainer cc, string pt, string cabin)
        {
            var url = string.Format(
                "http://flight.qunar.com/twelli/api/getInterTGQ.jsp?&depCity={0}&arrCity={1}&depDate={2}&flightNo={3}&wrapperId={4}&pInfo={5}&cabin={8}&passengerType=adult&transferAP=&pt={6}&_token={7}",
                defCity, arrCity, time, flightNo, warrpid, pinfo.Replace("#", "%23"), pt,
                new Random().Next(10000, 99999), cabin);
            // 退改签内容字符串
            var tgq = GetString(url, cc);
            return tgq;
        }

        /// <summary>
        ///     获取航班信息
        /// </summary>
        /// <param name="defCity">出发城市码</param>
        /// <param name="arrCity">目的城市码</param>
        /// <param name="time">出发日期</param>
        /// <param name="hs">航司</param>
        /// <returns></returns>
        private static Dictionary<string, object> GetFlightNo(string defCity, string arrCity, string time, string hs)
        {
            // 请求地址
            string url;
            // 响应信息
            string str;
            // 对象
            Dictionary<string, object> obj;

            #region 请求航班信息

            while (true)
            {
                url = string.Format("http://www.qua.com/flights/{0}-{1}/{2}", defCity, arrCity, time);
                CookieContainer cookie;
                var request = GetCookie(url, out cookie);
                using (request.GetResponse())
                {
                    //Thread.Sleep(1500);
                    url =
                        string.Format(
                            "http://www.qua.com/twell/en/search?stop=0&fcarrier={0}&depCity={1}&arrCity={2}&depDate={3}&searchType=OneWayFlight&from=home&_=",
                            hs, defCity, arrCity, time) + TimeHelper.DateTimeToUnixTimestamp(DateTime.Now);
                    str = GetString(url, cookie);
                    var timeSpatm = str.Split('"')[3];
                    url =
                        "http://www.qua.com/twell/en/groupdata?queryID={0}&from=home&_={1}";
                    url = string.Format(url, timeSpatm.Replace(":-", "%3A"), TimeHelper.DateTimeToUnixTimestamp(DateTime.Now));
                    //Thread.Sleep(800);
                    str = GetString(url, cookie);
                }
                if (str.Length > 89) break;
            }

            var start = str.IndexOf("flights", StringComparison.Ordinal) + 9;
            var end = str.IndexOf("prices", StringComparison.Ordinal) - 2;
            var mid = str.Substring(end);
            var nstr = str.Substring(start);
            str = nstr.Replace(mid, "");
            obj = JsonHelper.JsonToObject<object>(str) as Dictionary<string, object>;
            return obj;

            #endregion 请求航班信息
        }

        /// <summary>
        ///     请求cookie
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        private static HttpWebRequest GetCookie(string url, out CookieContainer cookie)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            cookie = new CookieContainer();
            if (cookie.Count == 0)
            {
                request.CookieContainer = new CookieContainer();
                cookie = request.CookieContainer;
            }
            else
            {
                request.CookieContainer = cookie;
            }
            return request;
        }

        /// <summary>
        ///     带cookie的get请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        private static string GetString(string url, CookieContainer cookie)
        {
            var str = string.Empty;
            var http = new HttpClient(new HttpClientHandler { CookieContainer = cookie });
            var result =
                http.GetAsync(url).Result;
            if (result.IsSuccessStatusCode)
            {
                str = result.Content.ReadAsStringAsync().Result;
            }
            else
            {
                Console.WriteLine(result.StatusCode);
                Console.WriteLine(result.Content.ReadAsStringAsync().Result);
            }
            return str.Trim();
        }

        #endregion

        #region TB、QN 共用代码

        /// <summary>
        ///     传数据
        /// </summary>
        /// <param name="str"></param>
        /// <param name="platid"></param>
        private static void SendDataToAdjustPrice(string str, int platid)
        {
            var clients = new HttpClient();
            HttpContent contents = new StringContent(str, Encoding.UTF8, "application/json");
            var url = string.Format("http://139.129.132.222:8095/api/Data/Price/UpdateQn?platform={0}", platid);
            var res = clients.PostAsync(url, contents).Result;
            //var res = clients.PostAsync("http://192.168.2.162:9494/api/Flight", contents).Result;
            if (res.IsSuccessStatusCode)
                str = res.Content.ReadAsStringAsync().Result;
            else
                str = res.Content.ReadAsStringAsync().Result;
        }

        /// <summary>
        ///     搜索参数
        /// </summary>
        /// <param name="linejson"></param>
        /// <param name="time"></param>
        /// <param name="defCity"></param>
        /// <param name="arrCity"></param>
        /// <param name="hs"></param>
        private static void SearchParams(string linejson, out string time, out string defCity, out string arrCity,
            out string hs)
        {
            // 反序列化航线信息的json串
            var lineinfo = JsonConvert.DeserializeObject<MissionDetials>(linejson);
            // 出发日期
            time = lineinfo.FromDate.ToString("yyyy-MM-dd");
            // 出发城市码
            defCity = lineinfo.AirLine.Substring(0, 3);
            // 到达城市码
            arrCity = lineinfo.AirLine.Substring(3);
            // 航司ma
            var hslist = lineinfo.Carrier;
            switch (hslist)
            {
                case "FD":
                    hs = "AK-FD-D7-QZ-XJ-XT-Z2";
                    break;

                case "JQ":
                    hs = "3K-BL-GK-JQ";
                    break;

                case "5J":
                    hs = "5J-DG";
                    break;

                case "TR":
                    hs = "TR-IT";
                    break;

                default:
                    hs = hslist;
                    break;
            }
        }

        /// <summary>
        ///     get请求
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string GetString(string url)
        {
            var str = string.Empty;
            var http = new HttpClient();

            var result =
                http.GetAsync(url).Result;
            if (result.IsSuccessStatusCode)
            {
                str = result.Content.ReadAsStringAsync().Result;
            }
            else
            {
                Console.WriteLine(result.StatusCode);
                Console.WriteLine(result.Content.ReadAsStringAsync().Result);
            }
            return str.Trim();
        }

        /// <summary>
        ///     任务航线信息
        /// </summary>
        /// <param name="linejson"></param>
        /// <returns></returns>
        private static int TaskLineInfo(out string linejson)
        {
            // 平台
            int platid;
            // 任务信息
            linejson = string.Empty;
            while (true)
            {
                for (platid = 1; platid <= 3; platid++)
                {
                    linejson = RequestTask(platid, linejson);
                    if (!string.IsNullOrEmpty(linejson))
                        break;
                }
                if (!string.IsNullOrEmpty(linejson))
                    break;
                Thread.Sleep(1000);
            }
            Console.WriteLine("取到任务{0}", linejson);
            return platid;
        }

        /// <summary>
        ///     请求任务
        /// </summary>
        /// <param name="platid">航班号</param>
        /// <param name="linejson"></param>
        /// <returns></returns>
        private static string RequestTask(int platid, string linejson)
        {
            // get请求航线信息
            while (true)
            {
                var client = new HttpClient();
                var url = string.Format("http://139.129.132.222:8096/api/QnMission/Get?platform={0}", platid);
                var response = client.DeleteAsync(url).Result;
                if (response.IsSuccessStatusCode)
                {
                    linejson = response.Content.ReadAsStringAsync().Result;
                    break;
                }
                Thread.Sleep(5000);
            }

            return linejson;
        }

        private class MissionDetials
        {
            public string Message { get; set; }

            public string Carrier { get; set; }

            public string AirLine { get; set; }
            public DateTime FromDate { get; set; }

            public int Count { get; set; }
            public string Data { get; set; }
            //public DateTime RecieveTime { get; set; }
        }
        #endregion
    }
}
