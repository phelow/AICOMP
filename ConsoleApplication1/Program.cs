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
    public class ServerResponse
    {
        [JsonProperty("gameID")]
        public string gameID { get; set; }
        [JsonProperty("playerID")]
        public string playerID { get; set; }
        [JsonProperty("state")]
        public string state { get; set; }
        [JsonProperty("hardBlockBoard")]
        public int[] hardBlockBoard { get; set; }
        [JsonProperty("boardSize")]
        public int boardSize { get; set; }
        [JsonProperty("bombMap")]
        public Dictionary<string, Dictionary<string, int>> bombMap { get; set; }
        [JsonProperty("moveIterator")]
        public int moveIterator { get; set; }
        [JsonProperty("player")]
        public Dictionary<string, object> player { get; set; }
        [JsonProperty("softBlockBoard")]
        public int[] softBlockBoard { get; set; }
        [JsonProperty("moveOrder")]
        public int[] moveOrder { get; set; }
        [JsonProperty("opponent")]
        public Dictionary<string, object> opponent { get; set; }

    }

    public class AStarTile
    {
        public enum blockType
        {
            HardBlock,
            SoftBlock,
            Passable
        }

        public blockType m_blockType;

        public int cost = 0;
        public bool isSafe;

        public int X;
        public int Y;

        public AStarTile CameFrom;
        public float CostFromStart;
        public float EstimatedCostToGoal;

        public AStarTile(int x, int y, ServerResponse server)
        {
            m_blockType = blockType.Passable;
            if (1 == server.hardBlockBoard[x + y * server.boardSize])
            {
                m_blockType = blockType.HardBlock;
            }
            if (1 == server.softBlockBoard[x + y * server.boardSize])
            {
                m_blockType = blockType.SoftBlock;
            }


            X = x;
            Y = y;

            isSafe = true;

            foreach (KeyValuePair<string, Dictionary<string, int>> bomb in server.bombMap)
            {
                Console.Write(bomb.Key);
                int bombX = Int32.Parse(bomb.Key.Split(new Char[] { ',' })[0]);
                int bombY = Int32.Parse(bomb.Key.Split(new Char[] { ',' })[1]);

                if (bombX == x)
                {
                    isSafe = false;
                }

                if (bombY == y)
                {
                    isSafe = false;
                }
            }
        }
    }

    class Program
    {
        static string[] m_actions = { "mu", "ml", "md", "mr", "tu", "tl", "td", "tr", "b", "op", "bp" };
        static string[] m_buyActions = { "buy_count", "buy_pierce", "buy_range", "buy_block" };
        static Random m_random;
        static AStarTile m_playerTile;
        static AStarTile m_opponentTile;

        static void Main(string[] args)
        {
            m_random = new Random();
            PlayGame();
        }
        static int HeuristicCalculation(AStarTile from, AStarTile to)
        {
            return Math.Abs(to.X - from.X) + Math.Abs(to.Y - from.Y);
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
                            ServerResponse parsed = JsonConvert.DeserializeObject<ServerResponse>(responseString);
                            Console.Write(parsed.playerID);
                            Console.Write(parsed.gameID);
                            request = (HttpWebRequest)WebRequest.Create("http://aicomp.io/api/games/submit/" + parsed.gameID);
                            postData = "{\"devkey\": \"5820df82d7d4995d08393b9f\", \"playerID\": \"" + parsed.playerID + "\", \"move\": \"" + "b"/*m_actions[m_random.Next(0, m_actions.Length)]*/ + "\" }";
                            data = Encoding.ASCII.GetBytes(postData);
                            request.Method = "POST";
                            request.ContentType = "application/json";
                            request.ContentLength = data.Length;

                            AStarTile[,] m_worldRepresentation = new AStarTile[parsed.boardSize, parsed.boardSize];

                            for (int x = 0; x < parsed.boardSize; x++)
                            {
                                for (int y = 0; y < parsed.boardSize; y++)
                                {
                                    m_worldRepresentation[x, y] = new AStarTile(x, y, parsed);
                                }
                            }

                            notComplete = parsed.state != "complete";

                            object playerX;
                            object playerY;
                            parsed.player.TryGetValue("x", out playerX);
                            parsed.player.TryGetValue("y", out playerY);

                            m_playerTile = m_worldRepresentation[(int)playerX, (int)playerY];

                            object opponentX;
                            object opponentY;
                            parsed.player.TryGetValue("x", out opponentX);
                            parsed.player.TryGetValue("y", out opponentY);

                            m_opponentTile = m_worldRepresentation[(int)playerX, (int)playerY];

                            HashSet<AStarTile> closedSet = new HashSet<AStarTile>(); //the already evaluated set of nodes

                            HashSet<AStarTile> openSet = new HashSet<AStarTile>(); //the set of currently discovered nodes to be evaluated

                            AStarTile endingTile = null;

                            if (parsed.bombMap.Count == 0)
                            {
                                endingTile = m_opponentTile;
                            }

                            AStarTile startingTile = m_playerTile;

                            openSet.Add(startingTile);

                            List<AStarTile> safeMoves = new List<AStarTile>();

                            //TODO: find the closest safe tile to the player

                            HashSet<AStarTile> visited = new HashSet<AStarTile>();


                            Queue<AStarTile> nextTiles = new Queue<AStarTile>();

                            nextTiles.Enqueue(startingTile);

                            while (endingTile == null)
                            {
                                AStarTile current = nextTiles.Dequeue();

                                if (current.m_blockType != AStarTile.blockType.Passable)
                                {
                                    continue;
                                }
                                if (current.isSafe)
                                {
                                    safeMoves.Add(current);
                                }


                                if (current.isSafe)
                                {
                                    endingTile = current;
                                }

                                if (current.X + 1 < parsed.boardSize)
                                {
                                    nextTiles.Enqueue(m_worldRepresentation[current.X + 1, current.Y]);
                                }

                                if (current.Y + 1 < parsed.boardSize)
                                {
                                    nextTiles.Enqueue(m_worldRepresentation[current.X, current.Y + 1]);
                                }



                                if (current.X - 1 > 0)
                                {
                                    nextTiles.Enqueue(m_worldRepresentation[current.X - 1, current.Y]);
                                }

                                if (current.Y - 1 < 0)
                                {
                                    nextTiles.Enqueue(m_worldRepresentation[current.X, current.Y - 1]);
                                }

                            }
                            
                            startingTile.CostFromStart = 0;
                            startingTile.EstimatedCostToGoal = HeuristicCalculation(startingTile, endingTile);
                            float t = 0.0f;
                            while (openSet.Count > 0)
                            {
                                AStarTile current = null;
                                foreach (AStarTile tile in openSet)
                                {
                                    if (current == null || tile.EstimatedCostToGoal < current.EstimatedCostToGoal)
                                    {
                                        current = tile;
                                    }
                                }

                                
                                openSet.Remove(current);
                                closedSet.Add(current);

                                if (current.m_blockType != AStarTile.blockType.Passable)
                                {
                                    continue;
                                }
                                if (current == endingTile)
                                {
                                    break;
                                }

                                HashSet<AStarTile> neighbors = new HashSet<AStarTile>();

                                if (current.X - 1 > 0)
                                {
                                    if (current.Y - 1 > 0)
                                    {
                                        neighbors.Add(m_worldRepresentation[current.X - 1, current.Y - 1]);
                                    }

                                    if (current.Y + 1 < parsed.boardSize)
                                    {
                                        neighbors.Add(m_worldRepresentation[current.X - 1, current.Y + 1]);

                                    }
                                    neighbors.Add(m_worldRepresentation[current.X - 1, current.Y]);
                                }
                                if (current.Y - 1 > 0)
                                {
                                    neighbors.Add(m_worldRepresentation[current.X, current.Y - 1]);
                                }

                                if (current.Y + 1 < parsed.boardSize)
                                {
                                    neighbors.Add(m_worldRepresentation[current.X, current.Y + 1]);

                                }

                                if (current.X + 1 < parsed.boardSize)
                                {
                                    if (current.Y - 1 > 0)
                                    {
                                        neighbors.Add(m_worldRepresentation[current.X + 1, current.Y - 1]);
                                    }

                                    if (current.Y + 1 < parsed.boardSize)
                                    {
                                        neighbors.Add(m_worldRepresentation[current.X + 1, current.Y + 1]);
                                    }

                                    neighbors.Add(m_worldRepresentation[current.X + 1, current.Y]);
                                }

                                foreach (AStarTile neighbor in neighbors)
                                {

                                    if (closedSet.Contains(neighbor))
                                    {
                                        continue;
                                    }

                                    float tentativeScore = current.CostFromStart + 1; //TODO: 1 is the distance between the two nodes (will always be one for now, change this tater)

                                    if (!openSet.Contains(neighbor))
                                    {
                                        openSet.Add(neighbor);
                                    }
                                    else if (tentativeScore >= neighbor.CostFromStart)
                                    {
                                        continue;
                                    }
                                    neighbor.CameFrom = current;
                                    neighbor.CostFromStart = tentativeScore;
                                    neighbor.EstimatedCostToGoal = HeuristicCalculation(neighbor, endingTile) + current.CostFromStart;
                                }
                            }


                            //pick a safe move.

                            //find out which position maps to it

                            //move to that position


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
