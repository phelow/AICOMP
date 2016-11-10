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
        [JsonProperty("trailmap")]
        public Dictionary<string, object> trailmap { get; set; }

    }

    public class BombSearchState
    {
        private int m_movementLeft;
        private int m_orientation;

        private int m_x;
        private int m_y;

        private int m_piercesLeft;

        public bool DestroyBlock()
        {
            m_piercesLeft--;
            return m_piercesLeft > 0 ? true : false;
        }

        public int ChargesLeft
        {
            get
            {
                return m_movementLeft;
            }
        }
        public int PiercesLeft
        {
            get
            {
                return m_piercesLeft;
            }
        }

        public int X
        {
            get
            {
                return m_x;
            }
        }

        public int Y
        {
            get
            {
                return m_y;
            }
        }

        public int Orientation
        {
            get
            {
                return m_orientation;
            }
        }

        public int MovementLeft
        {
            get
            {
                return m_movementLeft;
            }
        }

        public BombSearchState(int charges, int piercingPower, int orientation, int x, int y)
        {
            m_movementLeft = charges;
            m_orientation = orientation;
            m_x = x;
            m_y = y;
            m_piercesLeft = piercingPower;
        }
    }

    public class Portal
    {
        private Portal m_linkedPortal;
        private int m_x;
        private int m_y;
        private int m_orientation;
        public BombSearchState GetOutlet(BombSearchState inlet)
        {
            if (!(inlet.Orientation == 0 && this.m_orientation == 2) /*inlet is left and this is right*/
                || (inlet.Orientation == 2 && this.m_orientation == 0 /*inlet is right and this is left*/
                || (inlet.Orientation == 1 && this.m_orientation == 3) /*inlet is up and this is down*/
                || (inlet.Orientation == 3 && this.m_orientation == 1) /*inlet is down and this is up*/

                ))
            {
                return null;
            }

            //flip the orientation
            return new BombSearchState(inlet.ChargesLeft, inlet.PiercesLeft, m_linkedPortal.m_orientation, m_linkedPortal.m_x, m_linkedPortal.m_y); //TODO: check if charges is right

        }
        public static void LinkPortals(Portal a, Portal b)
        {
            a.LinkPortal(b);
            b.LinkPortal(a);
        }

        private void LinkPortal(Portal linkTo)
        {
            m_linkedPortal = linkTo;
        }

        public Portal(int x, int y, int orientation)
        {
            m_x = x;
            m_y = y;
            m_orientation = orientation;
        }
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
            if (1 == server.hardBlockBoard[y + x * server.boardSize])
            {
                m_blockType = blockType.HardBlock;
            }
            if (1 == server.softBlockBoard[y + x * server.boardSize])
            {
                m_blockType = blockType.SoftBlock;
            }


            X = x;
            Y = y;

            isSafe = true;
        }
    }



    class Program
    {
        static string[] m_actions = { "mu", "ml", "md", "mr", "tu", "tl", "td", "tr", "b", "op", "bp" };
        static string[] m_buyActions = { "buy_count", "buy_pierce", "buy_range", "buy_block" };
        static Random m_random;
        static AStarTile m_playerTile;
        static AStarTile m_opponentTile;
        static AStarTile[,] m_worldRepresentation;
        static List<Portal> portals;

        static ServerResponse m_parsed;

        static void Main(string[] args)
        {
            m_random = new Random();
            PlayGame();
        }
        static int HeuristicCalculation(AStarTile from, AStarTile to)
        {
            return Math.Abs(to.X - from.X) + Math.Abs(to.Y - from.Y);
        }

        static List<AStarTile> GetBombedSquares(int bombX, int bombY)
        {
            List<AStarTile> bombedTiles = new List<AStarTile>();

            Queue<BombSearchState> explosionFrontier = new Queue<BombSearchState>();

            object object_bombRange;

            m_parsed.player.TryGetValue("bombRange", out object_bombRange);

            int int_bombRange = Convert.ToInt32(object_bombRange);

            object object_bombPiercing;

            m_parsed.player.TryGetValue("bombPierce", out object_bombPiercing);

            int int_bombPiercing = Convert.ToInt32(object_bombPiercing);

            explosionFrontier.Enqueue(new BombSearchState(int_bombRange, int_bombPiercing, 0, bombX, bombY));
            explosionFrontier.Enqueue(new BombSearchState(int_bombRange, int_bombPiercing, 1, bombX, bombY));
            explosionFrontier.Enqueue(new BombSearchState(int_bombRange, int_bombPiercing, 2, bombX, bombY));
            explosionFrontier.Enqueue(new BombSearchState(int_bombRange, int_bombPiercing, 3, bombX, bombY));

            while (explosionFrontier.Count > 0)
            {
                BombSearchState current = explosionFrontier.Dequeue();

                m_worldRepresentation[current.X, current.Y].isSafe = false;
                bool shouldContinue = false;
                //TODO: check for portals
                foreach (Portal p in portals)
                {
                    BombSearchState portalState = p.GetOutlet(current);
                    if (current != null)
                    {
                        explosionFrontier.Enqueue(portalState);
                        shouldContinue = true;
                    }
                }

                bombedTiles.Add(m_worldRepresentation[current.X, current.Y]);

                if (shouldContinue)
                {
                    continue;
                }


                if (m_worldRepresentation[current.X, current.Y].m_blockType == AStarTile.blockType.HardBlock)
                {
                    continue;
                }

                if (m_worldRepresentation[current.X, current.Y].m_blockType == AStarTile.blockType.SoftBlock && !current.DestroyBlock())
                {
                    continue;
                }

                switch (current.Orientation)
                {
                    case 0:
                        if (current.X - 1 < 0)
                        {
                            continue;
                        }

                        explosionFrontier.Enqueue(new BombSearchState(current.ChargesLeft, current.PiercesLeft, current.Orientation, current.X - 1, current.Y));
                        break;
                    case 1:
                        if (current.Y - 1 < 0)
                        {
                            continue;
                        }
                        explosionFrontier.Enqueue(new BombSearchState(current.ChargesLeft, current.PiercesLeft, current.Orientation, current.X, current.Y - 1));
                        break;
                    case 2:
                        if (current.X + 1 >= m_parsed.boardSize)
                        {
                            continue;
                        }

                        explosionFrontier.Enqueue(new BombSearchState(current.ChargesLeft, current.PiercesLeft, current.Orientation, current.X + 1, current.Y));
                        break;
                    case 3:
                        if (current.Y + 1 >= m_parsed.boardSize)
                        {
                            continue;
                        }

                        explosionFrontier.Enqueue(new BombSearchState(current.ChargesLeft, current.PiercesLeft, current.Orientation, current.X, current.Y + 1));
                        break;
                }
            }

            return bombedTiles;
        }

        static void PlayGame()
        {

            while (true)
            {
                try
                {
                    Console.Write("nSetting up game with server\n");
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
                    bool gameNotCompleted = true;
                    do
                    {
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            string chosenAction = "";
                            var responseString = reader.ReadToEnd();
                            Console.Write(responseString);
                            m_parsed = JsonConvert.DeserializeObject<ServerResponse>(responseString);

                            m_worldRepresentation = new AStarTile[m_parsed.boardSize, m_parsed.boardSize];
                            //make the portals
                            portals = new List<Portal>();


                            //TODO: use portals in A* linkages

                            for (int x = 0; x < m_parsed.boardSize; x++)
                            {
                                for (int y = 0; y < m_parsed.boardSize; y++)
                                {
                                    m_worldRepresentation[x, y] = new AStarTile(x, y, m_parsed);
                                }
                            }


                            //TODO: move this search
                            foreach (KeyValuePair<string, Dictionary<string, int>> bomb in m_parsed.bombMap)
                            {
                                int bombX = Int32.Parse(bomb.Key.Split(new Char[] { ',' })[0]);
                                int bombY = Int32.Parse(bomb.Key.Split(new Char[] { ',' })[1]);
                                //TODO: calculate if in range of bomb including portal traversal and blocking
                                List<AStarTile> bombedSquares = GetBombedSquares(bombX, bombY);
                            }



                            gameNotCompleted = m_parsed.state != "complete";

                            object object_playerX;
                            object object_playerY;
                            m_parsed.player.TryGetValue("x", out object_playerX);
                            m_parsed.player.TryGetValue("y", out object_playerY);

                            int int_px = Convert.ToInt32(object_playerX);
                            int int_py = Convert.ToInt32(object_playerY);

                            m_playerTile = m_worldRepresentation[int_px, int_py];

                            object opponentX;
                            object opponentY;
                            m_parsed.opponent.TryGetValue("x", out opponentX);
                            m_parsed.opponent.TryGetValue("y", out opponentY);
                            int int_ox = Convert.ToInt32(opponentX);
                            int int_oy = Convert.ToInt32(opponentY);

                            m_opponentTile = m_worldRepresentation[int_ox, int_oy];

                            HashSet<AStarTile> closedSet = new HashSet<AStarTile>(); //the already evaluated set of nodes


                            AStarTile endingTile = null;

                            if (m_parsed.bombMap.Count == 0)
                            {
                                endingTile = m_opponentTile;
                            }


                            List<AStarTile> safeMoves = new List<AStarTile>();
                            HashSet<AStarTile> visited = new HashSet<AStarTile>();
                            Queue<AStarTile> nextTiles = new Queue<AStarTile>();

                            nextTiles.Enqueue(m_playerTile);

                            //BFS search to find all safe tiles
                            while (nextTiles.Count > 0)
                            {
                                AStarTile current = nextTiles.Dequeue();

                                if (visited.Contains(current))
                                {
                                    continue;
                                }

                                visited.Add(current);

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

                                if (current.X + 1 < m_parsed.boardSize)
                                {
                                    nextTiles.Enqueue(m_worldRepresentation[current.X + 1, current.Y]);
                                }

                                if (current.Y + 1 < m_parsed.boardSize)
                                {
                                    nextTiles.Enqueue(m_worldRepresentation[current.X, current.Y + 1]);
                                }



                                if (current.X - 1 > 0)
                                {
                                    nextTiles.Enqueue(m_worldRepresentation[current.X - 1, current.Y]);
                                }

                                if (current.Y - 1 > 0)
                                {
                                    nextTiles.Enqueue(m_worldRepresentation[current.X, current.Y - 1]);
                                }
                            }


                            //if we can drop a bomb and survive do that
                            bool canBomb = false;
                            object orientation;
                            m_parsed.player.TryGetValue("orientation", out orientation);
                            int int_orientation = Convert.ToInt32(orientation);

                            foreach (AStarTile tile in safeMoves)
                            {
                                if (tile.X != m_playerTile.X && tile.Y != m_playerTile.Y && m_parsed.bombMap.Count == 0 && HeuristicCalculation(tile, m_playerTile) <= 3/* && orientationWorks*/)
                                {
                                    canBomb = true;
                                }
                            }

                            //Pick your target tile
                            AStarTile targetTile = null;
                            if (safeMoves.Count == 0)
                            {
                                targetTile = m_playerTile;
                            }

                            if (safeMoves.Count == 1)
                            {
                                targetTile = safeMoves[0];
                            }
                            else {
                                //else pick a safe move.


                                targetTile = safeMoves[m_random.Next(0, safeMoves.Count)];

                                if (m_parsed.bombMap.Count > 0)
                                {
                                    for (int i = 0; i < safeMoves.Count; i++)
                                    { //TODO: always pick the lowest cost
                                        if (HeuristicCalculation(m_playerTile, targetTile) > HeuristicCalculation(m_playerTile, safeMoves[i]))
                                        {
                                            targetTile = safeMoves[i];
                                        }
                                    }
                                }
                            }


                            for (int x = 0; x < m_parsed.boardSize; x++)
                            {
                                for (int y = 0; y < m_parsed.boardSize; y++)
                                {

                                    Console.Write((int)m_worldRepresentation[x, y].m_blockType);
                                }
                                Console.Write("\n");
                            }

                            Console.Write("\n" + targetTile.X + " " + targetTile.Y + " " + targetTile + " \n");



                            HashSet<AStarTile> openSet = new HashSet<AStarTile>(); //the set of currently discovered nodes to be evaluated
                            m_playerTile.CostFromStart = 0;
                            endingTile = targetTile;
                            if (endingTile == null)
                            {
                                m_playerTile.EstimatedCostToGoal = 0;
                            }
                            else {
                                m_playerTile.EstimatedCostToGoal = HeuristicCalculation(m_playerTile, endingTile);
                            }
                            float t = 0.0f;
                            openSet.Add(m_playerTile);

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

                                HashSet<AStarTile> neighbors = new HashSet<AStarTile>();

                                if (current.X - 1 > 0)
                                {
                                    neighbors.Add(m_worldRepresentation[current.X - 1, current.Y]);
                                }
                                if (current.Y - 1 > 0)
                                {
                                    neighbors.Add(m_worldRepresentation[current.X, current.Y - 1]);
                                }

                                if (current.Y + 1 < m_parsed.boardSize)
                                {
                                    neighbors.Add(m_worldRepresentation[current.X, current.Y + 1]);

                                }

                                if (current.X + 1 < m_parsed.boardSize)
                                {
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
                                    if (endingTile == null)
                                    {
                                        neighbor.EstimatedCostToGoal = current.CostFromStart;

                                    }
                                    else {
                                        neighbor.EstimatedCostToGoal = HeuristicCalculation(neighbor, endingTile) + current.CostFromStart;
                                    }
                                }
                            }

                            while (targetTile.CameFrom != m_playerTile && targetTile.CameFrom != null)
                            {
                                targetTile = targetTile.CameFrom;
                            }

                            Console.Write("\nTarget tile final:" + targetTile.X + " " + targetTile.Y + " " + targetTile.m_blockType + "\n");
                            Console.Write("\nMy position:" + m_playerTile.X + " " + m_playerTile.Y + "\n");

                            if (targetTile.X > m_playerTile.X)
                            {
                                chosenAction = "mr";
                            }
                            else if (targetTile.X < m_playerTile.X)
                            {
                                chosenAction = "ml";
                            }
                            else if (targetTile.Y < m_playerTile.Y)
                            {
                                chosenAction = "mu";
                            }
                            else if (targetTile.Y > m_playerTile.Y)
                            {
                                chosenAction = "md";
                            }

                            bool hasBenefit = false;

                            List<AStarTile> bombedTiles = GetBombedSquares(m_playerTile.X, m_playerTile.Y);
                            List<AStarTile> bombableSoftBlocks = bombedTiles.Where(item => item.m_blockType == AStarTile.blockType.SoftBlock).ToList();

                            if (bombedTiles.Contains(m_opponentTile) || bombableSoftBlocks.Count > 0)
                            {
                                hasBenefit = true;
                            }

                            if (canBomb && hasBenefit)
                            {
                                chosenAction = "b";
                            }
                            if (m_parsed.trailmap.Count > 0)
                            {
                                chosenAction = "";
                            }

                            //find out which position maps to it

                            //move to that position

                            Console.Write(m_parsed.playerID);
                            Console.Write(m_parsed.gameID);
                            request = (HttpWebRequest)WebRequest.Create("http://aicomp.io/api/games/submit/" + m_parsed.gameID);
                            postData = "{\"devkey\": \"5820df82d7d4995d08393b9f\", \"playerID\": \"" + m_parsed.playerID + "\", \"move\": \"" + chosenAction/*m_actions[m_random.Next(0, m_actions.Length)]*/ + "\" }";
                            data = Encoding.ASCII.GetBytes(postData);
                            request.Method = "POST";
                            request.ContentType = "application/json";
                            request.ContentLength = data.Length;


                            Console.Write("\nAction is:" + chosenAction + "\n");

                            using (var stream = request.GetRequestStream())
                            {
                                stream.Write(data, 0, data.Length);
                            }
                            response = (HttpWebResponse)request.GetResponse();
                        }
                        Console.Write("Sleeping");
                        System.Threading.Thread.Sleep(1000);
                        Console.Write("Start Iteration");
                    } while (gameNotCompleted);
                }
                catch
                {
                    Console.Write("\n An error was encountered \n");
                }
            }
        }
    }
}
