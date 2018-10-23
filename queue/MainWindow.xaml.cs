using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace queue
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static String LogFileName;
        private static readonly HttpClient client = new HttpClient();
        //API KEY QVKwDnKQKNx8xRUb0OqLxVxaIBaATuRjtUc8c9QgckmeEenRH1JDLKmRIZnDrgMp
        private String apiKey = "QVKwDnKQKNx8xRUb0OqLxVxaIBaATuRjtUc8c9QgckmeEenRH1JDLKmRIZnDrgMp";
        public MainWindow()
        {
            InitializeComponent();
            LogFileName = "log" + /*DateTime.Now.ToString("yyyyMMdd-HHmmss") + */".txt";
            LogWorker("\n\n\n I am alive? \n");
            //Request();

            // move 'em to a button
            /*List<Thread> threadler = new List<Thread>();
            threadler.Add(new Thread(() => Request()));
            threadler[threadler.Count - 1].Start();*/
        }
        
        private class TradeInfo
        {
            public string id { get; set; }
            public string price { get; set; }
            public string qty { get; set; }
            public string time { get; set; }
            public string isBuyerMaker { get; set; }
            public string isBestMatch { get; set; }
        }

        private class Depth
        {
            public string lastUpdateId { get; set; }
            public JArray bids { get; set; }
            public JArray asks { get; set; }
        }
        

        // bekletme(ms)
        public async Task<string> WaitAsynchronouslyAsync(int i)
        {
            await Task.Delay(i);
            return "Finished";
        }

        private async void Request()
        {
            try
            {
                bool cont = true;
                int count = 0;
                int lastTrade = 0;
                string tradeHistoryString = "";
                while (cont)
                {
                    //trade history
                    var tradeHistory = System.Net.WebRequest.Create("https://www.binance.com/api/v1/historicalTrades?symbol=HOTBTC&limit=50");
                    tradeHistory.Method = "GET";
                    tradeHistory.ContentType = "text/html";
                    tradeHistory.Headers["X-MBX-APIKEY"] = apiKey;
                    using (Stream s = tradeHistory.GetResponse().GetResponseStream())
                    {
                        using (StreamReader sr = new StreamReader(s))
                        {
                            tradeHistoryString = sr.ReadToEnd();
                            LogWorker(String.Format("Response: {0}", tradeHistoryString));
                        }
                    }
                    List<TradeInfo> tradeHistoryList = JsonConvert.DeserializeObject<List<TradeInfo>>(tradeHistoryString);
                    tradeHistoryList.Reverse();
                    LogWorker("Last trade from history: " + tradeHistoryList[0].id + "with amount of " + tradeHistoryList[0].qty);


                    //last trades
                    var tradeString = await client.GetStringAsync("https://www.binance.com/api/v1/trades?symbol=HOTBTC&limit=50");
                    List<TradeInfo> lastTrades = JsonConvert.DeserializeObject<List<TradeInfo>>(tradeString);
                    lastTrades.Reverse();
                    if (lastTrade != 0)
                    {
                        foreach (TradeInfo item in lastTrades)
                        {
                            if (Int32.Parse(item.id) != lastTrade)
                            {
                                LogWorker("Found new trade with ID: " + item.id + " with qty: " + item.qty);
                                cont = false;
                            }
                            else { break; }
                        }
                    }
                    lastTrade = Int32.Parse(lastTrades[0].id);
                    LogWorker("Last trade id: " + lastTrade.ToString() + " with qty: " + lastTrades[0].qty);
                    count++;
                    LogWorker("waitin 1 ssecs...");
                    await WaitAsynchronouslyAsync(1000);
                    LogWorker("waited, checking again");

                    //depth
                    var depthString = await client.GetStringAsync("https://www.binance.com/api/v1/depth?symbol=HOTBTC&limit=10");
                    LogWorker(depthString);
                    Depth depths = JsonConvert.DeserializeObject<Depth>(depthString);
                    int comp = 0;
                    double comp1 = 0;
                    double comp2 = 0;
                    foreach(var item in depths.bids.Children())
                    {
                        //LogWorker(item.First.ToString());
                        if (comp == 0) { Double.TryParse(item.First.ToString(), out comp1); }
                        if (comp == 1) { Double.TryParse(depths.asks.First.First.ToString(), out comp2); }
                        LogWorker(depths.asks.First.First.ToString());
                        comp++;
                        if (comp == 2)
                        {
                            LogWorker(comp1.ToString());
                            LogWorker(comp2.ToString());
                            if(comp1 < comp2)
                            {
                                LogWorker("yesxd");
                            }
                            break;
                        }
                    }
                    //LogWorker(depths.lastUpdateId);
                    /*LogWorker("Lowest bid price: " + depths[0].bids[0][0]);
                    LogWorker("Highest ask price: " + depths[0].asks[0][0]);*/
                    /*JObject trades = JObject.Parse(depthString);
                    Depth J = trades.First.Next.ToObject<Depth>();
                    LogWorker(trades.First.Next[0].ToString());*/
                    

                }

                

                //JObject trades = JObject.Parse(responseString);
                // v1
                //string name = stuff.lastUpdateId;
                //LogWorker(name);

                // v2
                /*IList<JToken> results = trades["bids"].ToList();
                foreach (JToken result in results)
                {
                    LogWorker(result[1].ToString());
                }*/
            }
            catch (Exception ex)
            {
                LogWorker(ex.Message);
            }
        }

        private void LogWorker(String log)
        {
            try
            {
                using (StreamWriter w = File.AppendText(LogFileName))
                {
                    w.WriteLine("{0} : {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), log);
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        private async void Instrument_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Instrument.IsEnabled = false;
                StartQueue.IsEnabled = false;
                //get depth info
                var depthString = await client.GetStringAsync("https://www.binance.com/api/v1/depth?symbol=" + Instrument.SelectedItem.ToString() + "&limit=10");
                LogWorker("Got the depth info: " + depthString);
                Depth depths = JsonConvert.DeserializeObject<Depth>(depthString);
                // enter closest bid&ask prices to UI
                BidTextBlock.Text = depths.bids.First.First.ToString();
                AskTextBlock.Text = depths.asks.First.First.ToString();
                QueuePrice.Text = depths.asks.First.First.ToString();
            }
            catch(Exception ex)
            {
                LogWorker("Error while parsing initial bid & ask prices: \n" + ex.Message);
            }
            finally{
                Instrument.IsEnabled = true;
                StartQueue.IsEnabled = true;
            }
        }

        private async void StartQueue_Click(object sender, RoutedEventArgs e)
        {
            decimal queuePrice = 0;
            decimal queueAmount = 0;
            try
            {
                LogWorker("Started Queue!");
                queuePrice = decimal.Parse(QueuePrice.Text, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo);
                //get depth info
                var depthString = await client.GetStringAsync("https://www.binance.com/api/v1/depth?symbol=" + Instrument.SelectedItem.ToString() + "&limit=10");
                LogWorker("Got the depth info: " + depthString);
                Depth depths = JsonConvert.DeserializeObject<Depth>(depthString);
                // find the queue price&amount in depths
                if(queuePrice <= MyDC(depths.bids.First.First.ToString()))
                {
                    foreach(var item in depths.bids.Children())
                    {
                        if(queuePrice == MyDC(item.First.ToString()))
                        {
                            LogWorker("Found the queue price!");
                            LogWorker("Queue price is: " + item.First.ToString() + " with total amount of: " + item.First.Next.ToString());
                            queueAmount = MyDC(item.First.Next.ToString());
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var item in depths.asks.Children())
                    {
                        if (queuePrice == MyDC(item.First.ToString()))
                        {
                            LogWorker("Found the queue price!");
                            LogWorker("Queue price is: " + item.First.ToString() + " with total amount of: " + item.First.Next.ToString());
                            queueAmount = MyDC(item.First.Next.ToString());
                            break;
                        }
                    }
                }
                // Start tracking on another thread
                if(queuePrice == 0 || queueAmount == 0)
                {
                    LogWorker("Price or Amount could not be populated. I think something is wrong...");
                }
                else
                {
                    //Start tracking if everything is fine

                }
            }
            catch (Exception ex)
            {
                LogWorker("Error while parsing initial bid & ask prices: \n" + ex.Message);
            }
        }

        private decimal MyDC(string stringtodecimal)
        {
            return decimal.Parse(stringtodecimal, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        private async void StartTracking()
        {

        }
    }
}

