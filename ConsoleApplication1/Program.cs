using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace ConsoleApplication1
{
    public class serverResponse
    {
        [JsonProperty("gameID")]
        public string gameID { get; set; }
        [JsonProperty("playerID")]
        public string playerID { get; set; }
        [JsonProperty("state")]
        public string state { get; set; }
    }

    class Program
    {
        static string[] m_actions = { "mu", "ml", "md", "mr", "tu", "tl", "td", "tr", "b", "op", "bp" };
        static string[] m_buyActions = { "buy_count", "buy_pierce", "buy_range", "buy_block" };
        static Random m_random;

        static void Main(string[] args)
        {
            m_random = new Random();
            PlayGame();
        }

        static void PlayGame()
        {
            while (true)
            {
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create("http://aicomp.io/api/games/practice");

                    var postData = "{\"devkey\": \"5820df82d7d4995d08393b9f\", \"username\": \"keyboardkommander\" }";
                    var data = Encoding.ASCII.GetBytes(postData);

                    request.Method = "POST";
                    request.ContentType = "application/json";
                    request.ContentLength = data.Length;

                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }

                    var response = (HttpWebResponse)request.GetResponse();
                    bool notComplete = true;
                    do
                    {
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            var responseString = reader.ReadToEnd();
                            Console.Write(responseString);
                            serverResponse parsed = JsonConvert.DeserializeObject<serverResponse>(responseString);
                            Console.Write(parsed.playerID);
                            Console.Write(parsed.gameID);
                            request = (HttpWebRequest)WebRequest.Create("http://aicomp.io/api/games/submit/" + parsed.gameID);
                            postData = "{\"devkey\": \"5820df82d7d4995d08393b9f\", \"playerID\": \"" + parsed.playerID + "\", \"move\": \"" + m_actions[m_random.Next(0, m_actions.Length)] + "\" }";
                            data = Encoding.ASCII.GetBytes(postData);
                            request.Method = "POST";
                            request.ContentType = "application/json";
                            request.ContentLength = data.Length;

                            notComplete = parsed.state != "complete";

                            using (var stream = request.GetRequestStream())
                            {
                                stream.Write(data, 0, data.Length);
                            }
                            response = (HttpWebResponse)request.GetResponse();
                        }
                        Console.Write("Sleeping");
                        System.Threading.Thread.Sleep(1000);
                        Console.Write("Start Iteration");
                    } while (notComplete);

                }
                catch
                {

                }


                /*var playerID = response.GetResponseHeader("playerID");
                var gameID = response.GetResponseHeader("gameID");
                while (response.GetResponseHeader("state") != "complete")
                {
                    request = (HttpWebRequest)WebRequest.Create("http://aicomp.io/api/games/submit/" + gameID);
                    postData = "{\"devkey\": \"5820df82d7d4995d08393b9f\", \"playerID\": \"" + playerID + "\", move\": \"b\" }";
                    response = (HttpWebResponse)request.GetResponse();
                }*/
                // var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
        }
    }
}
