using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Threading;

namespace ConsoleApplication1
{
    public class ServerResponse
    {
        [JsonProperty("gameID")]
        public string gameID { get; set; }
        [JsonProperty("playerID")]
        public string playerID { get; set; }
        [JsonProperty("playerIndex")]
        public int playerIndex { get; set; }
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
        [JsonProperty("portalMap")]
        public Dictionary<string, Dictionary<string, Dictionary<string, object>>> portalMap { get; set; }
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

            set
            {
                m_piercesLeft = value;
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

    public class AStarTile
    {
        public enum blockType
        {
            HardBlock,
            SoftBlock,
            Passable
        }

        public blockType m_blockType;

        public int cost = -1;
        public int m_numTargets;

        private List<int> turnsUntilDangerous; //if this is 0 then it is currently dangerous

        public bool isSafeUntil(int until)
        {
            foreach (int detonationTurn in turnsUntilDangerous)
            {
                if (detonationTurn <= until)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsSuperSafe()
        {
            return turnsUntilDangerous.Count == 0;
        }

        public List<int> GetDangerousTurns()
        {
            return turnsUntilDangerous;
        }

        public bool SafeOnStep(int step)
        {
            if (turnsUntilDangerous.Count == 0)
            {
                //////Console.Write("\n" + X + " " + Y + " is completely safe");
                return true;
            }

            foreach (int dangerousTurn in turnsUntilDangerous)
            {
                if (dangerousTurn == step) //TODO: This is extremely sloppy coding please fix this
                {
                    //////Console.Write("\n" + X + " " + Y + " is not safe " + dangerousTurn + " is too similar to " + step);
                    return false;
                }
            }

            //////Console.Write("\n" + X + " " + Y + " is safe on turn" + step + " it is unsafe on:");

            foreach (int dangerousTurn in turnsUntilDangerous)
            {
                //////Console.Write("\n" + dangerousTurn);
            }

            return true; //TODO: this seems hack, re-evaluate and talk to darwin.
        }

        public void SetDangerous(int newDanger)
        {
            turnsUntilDangerous.Add(newDanger);
        }

        public int X;
        public int Y;

        public AStarTile CameFrom;
        public string MoveToGetHere = "";
        public float EstimatedCostToGoal;

        public AStarTile(int x, int y, ServerResponse server)
        {
            turnsUntilDangerous = new List<int>();
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
        }

        public AStarTile(int x, int y, blockType bt)
        {
            turnsUntilDangerous = new List<int>();
            X = y;
            Y = y;
            m_blockType = bt;
        }

        public AStarTile CopyConstructor()
        {
            AStarTile newTile = new AStarTile(this.X, this.Y, m_blockType);

            foreach (int i in turnsUntilDangerous)
            {
                newTile.SetDangerous(i);
            }

            return newTile;
        }
    }



    class Program
    {



        public class AStarBoardState
        {
            public AStarTile m_projectedPlayerTile;
            public int m_projectedPlayerOrientation;

            public int m_cost;
            
            public string m_moveToGetHere;


            public List<Portal> m_portals;
            public AStarTile[,] m_boardState;
            public AStarBoardState m_cameFrom;

            public AStarBoardState(AStarTile projectedPlayerTile, int projectedPlayerOrientation, List<Portal> portals, AStarTile[,] boardState, int cost)
            {
                m_portals = new List<Portal>();
                m_boardState = new AStarTile[m_parsed.boardSize, m_parsed.boardSize];
                //Deep copy all of the elements
                for (int x = 0; x < m_parsed.boardSize; x++)
                {
                    for (int y = 0; y < m_parsed.boardSize; y++)
                    {

                        m_boardState[x, y] = boardState[x, y];//TODO: this boardState[x, y].CopyConstructor();
                    }

                }

                m_projectedPlayerTile = m_boardState[projectedPlayerTile.X, projectedPlayerTile.Y];

                m_projectedPlayerOrientation = projectedPlayerOrientation;

                foreach (Portal p in portals)
                {
                    m_portals.Add(p.CopyConstructor());
                }

                m_cost = cost;

            }

            //TODO: update bomb ticks and whatnot
            public void Update()
            {

            }
            //TODO: this is all wiggity wack you
            public AStarBoardState MoveLeft(AStarBoardState last)
            {
                AStarBoardState state =  new AStarBoardState(m_boardState[m_projectedPlayerTile.X + 1, m_projectedPlayerTile.Y], 0, portals, m_boardState, m_cost + 2);
                state.m_moveToGetHere = "mr";
                state.m_cameFrom = last;
                return state;
            }


            public AStarBoardState MoveRight(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X - 1, m_projectedPlayerTile.Y], 2, portals, m_boardState, m_cost + 2);
                state.m_moveToGetHere = "ml";
                state.m_cameFrom = last;
                return state;

            }


            public AStarBoardState MoveUp(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y - 1], 1, portals, m_boardState, m_cost + 2);
                state.m_moveToGetHere = "mu";
                state.m_cameFrom = last;
                return state;
            }


            public AStarBoardState MoveDown(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y + 1], 3, portals, m_boardState, m_cost + 2);
                state.m_moveToGetHere = "md";
                state.m_cameFrom = last;
                return state;

            }



            public AStarBoardState TurnLeft(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 0, portals, m_boardState, m_cost + 2);
                state.m_moveToGetHere = "tl";
                state.m_cameFrom = last;
                return state;

            }


            public AStarBoardState TurnRight(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 2, portals, m_boardState, m_cost + 2);
                state.m_moveToGetHere = "tr";
                state.m_cameFrom = last;
                return state;

            }


            public AStarBoardState TurnUp(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 1, portals, m_boardState, m_cost + 2);
                state.m_moveToGetHere = "tu";
                state.m_cameFrom = last;
                return state;

            }


            public AStarBoardState TurnDown(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 3, portals, m_boardState, m_cost + 2);
                state.m_moveToGetHere = "td";
                state.m_cameFrom = last;
                return state;

            }



            public AStarBoardState ShootBluePortal(AStarBoardState last)
            {
                AStarBoardState state = ShoootPortal(false);
                state.m_moveToGetHere = "op";
                state.m_cameFrom = last;
                return state;

            }

            public AStarBoardState ShoootPortal(bool isOrange)
            {

                AStarTile tileIt = m_projectedPlayerTile;

                while (! (tileIt.m_blockType == AStarTile.blockType.HardBlock || tileIt.m_blockType == AStarTile.blockType.SoftBlock))
                {
                    if (m_projectedPlayerOrientation == 0)
                    {
                        if (tileIt.X + 1 > m_parsed.boardSize)
                        {
                            ////Console.WriteLine("Out of range");
                            continue;

                        }
                        tileIt = m_boardState[tileIt.X - 1, tileIt.Y];
                    }
                    else if (m_projectedPlayerOrientation == 1)
                    {
                        if (tileIt.Y - 1 < 0)
                        {
                            ////Console.WriteLine("Out of range");
                            continue;

                        }
                        tileIt = m_boardState[tileIt.X, tileIt.Y - 1];

                    }
                    else if (m_projectedPlayerOrientation == 2)
                    {
                        if (tileIt.X - 1 < 0)
                        {
                            ////Console.WriteLine("Out of range");
                            continue;

                        }
                        tileIt = m_boardState[tileIt.X - 1, tileIt.Y];
                    }
                    else if (m_projectedPlayerOrientation == 3)
                    {
                        if (tileIt.Y + 1 < 0)
                        {
                            ////Console.WriteLine("Out of range");
                            continue;

                        }
                        tileIt = m_boardState[tileIt.X, tileIt.Y + 1];

                    }
                }
                Portal toRemove = null;
                foreach (Portal p in portals)
                {
                    if (p.m_isOrange == isOrange && p.m_owner == m_parsed.playerIndex)
                    {
                        toRemove = p;
                    }
                }

                if (toRemove != null)
                {
                    portals.Remove(toRemove);
                }

                int newOrientation = 0;

                if (m_projectedPlayerOrientation == 0)
                {
                    newOrientation = 2;
                }
                else if (m_projectedPlayerOrientation == 1)
                {
                    newOrientation = 3;
                }
                else if (m_projectedPlayerOrientation == 2)
                {
                    newOrientation = 0;
                }
                else if (m_projectedPlayerOrientation == 3)
                {
                    newOrientation = 1;
                }

                portals.Add(new Portal(tileIt.X, tileIt.Y, newOrientation, m_parsed.playerIndex, isOrange));

                return new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], m_projectedPlayerOrientation, portals, m_boardState, m_cost + 2);
            }

            public AStarBoardState ShootORangePortal(AStarBoardState last)
            {
                AStarBoardState state = ShoootPortal(true);
                state.m_moveToGetHere = "bp";
                state.m_cameFrom = last;
                return state;

            }

        }
        public class Portal
        {
            private Portal m_linkedPortal;
            public int m_x;
            public int m_y;
            private int m_orientation;
            public int m_owner;
            public bool m_isOrange;
            public BombSearchState GetBombOutlet(BombSearchState inlet)
            {
                if (m_x != inlet.X || m_y != inlet.Y)
                {
                    return null;
                }

                if (!(inlet.Orientation == 0 && this.m_orientation == 2) /*inlet is left and this is right*/
                    || (inlet.Orientation == 2 && this.m_orientation == 0 /*inlet is right and this is left*/
                    || (inlet.Orientation == 1 && this.m_orientation == 3) /*inlet is up and this is down*/
                    || (inlet.Orientation == 3 && this.m_orientation == 1) /*inlet is down and this is up*/

                    ))
                {
                    return null;
                }

                if (m_linkedPortal == null)
                {
                    return null;
                }

                int xMod = 0;

                int yMod = 0;

                if (m_linkedPortal.m_orientation == 2)
                {
                    xMod = 1;
                }


                if (m_linkedPortal.m_orientation == 0)
                {
                    xMod = -1;
                }


                if (m_linkedPortal.m_orientation == 1)
                {
                    yMod = -1;
                }


                if (m_linkedPortal.m_orientation == 3)
                {
                    yMod = +1;
                }


                //flip the orientation
                return new BombSearchState(inlet.ChargesLeft - 1, inlet.PiercesLeft, inlet.Orientation, m_linkedPortal.m_x + xMod, m_linkedPortal.m_y + yMod); //TODO: check if charges is right

            }

            public AStarTile OutletAStarTile()
            {
                if (this.m_orientation == 2)
                {
                    return m_worldRepresentation[this.m_x + 1, this.m_y];
                }
                else if (this.m_orientation == 0)
                {
                    return m_worldRepresentation[this.m_x - 1, this.m_y];
                }
                else if (this.m_orientation == 3)
                {
                    return m_worldRepresentation[this.m_x, this.m_y + 1];
                }
                else
                {
                    return m_worldRepresentation[this.m_x, this.m_y - 1];
                }
            }

            public AStarBoardState GetTileOutlet(AStarBoardState inlet)
            {
                if (!((inlet.m_projectedPlayerTile.Y == this.m_y && inlet.m_projectedPlayerTile.X - 1 == this.m_x) && this.m_orientation == 2) /*inlet is left and this is right*/
                    || ((inlet.m_projectedPlayerTile.Y == this.m_y && inlet.m_projectedPlayerTile.X + 1 == this.m_x) && this.m_orientation == 0 /*inlet is right and this is left*/
                    || ((inlet.m_projectedPlayerTile.Y == this.m_y - 1 && inlet.m_projectedPlayerTile.X == this.m_x) && this.m_orientation == 3) /*inlet is up and this is down*/
                    || ((inlet.m_projectedPlayerTile.Y == this.m_y + 1 && inlet.m_projectedPlayerTile.X == this.m_x) && this.m_orientation == 1) /*inlet is down and this is up*/

                    ))
                {
                    return null;
                }

                if (m_linkedPortal == null)
                {
                    return null;
                }

                AStarTile ret = m_linkedPortal.OutletAStarTile();
                AStarBoardState retu = new AStarBoardState( inlet.m_boardState[ret.X, ret.Y],0,inlet.m_portals,inlet.m_boardState,inlet.m_cost + 2);

                if (this.m_orientation == 2) //TODO: check these orientations
                {
                    retu.m_moveToGetHere = "mr";
                    
                }
                else if (this.m_orientation == 0)
                {
                    retu.m_moveToGetHere = "ml";
                }
                else if (this.m_orientation == 3)
                {
                    retu.m_moveToGetHere = "mu";
                }
                else
                {
                    retu.m_moveToGetHere = "md";
                }



                return retu;

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

            public Portal(int x, int y, int orientation, int owner, bool isOrange)
            {
                m_owner = owner;
                m_x = x;
                m_y = y;
                m_orientation = orientation;
                m_isOrange = isOrange;
            }

            public Portal CopyConstructor()
            {
                return new Portal(m_x, m_y, m_orientation, m_owner, m_isOrange);
            }
        }

        static string[] m_actions = { /*"mu", "ml", "md", "mr", "tu", "tl", "td", "tr", "b", */"op", "bp" };
        static string[] m_buyActions = { "buy_count", "buy_pierce", "buy_range", "buy_block" };
        static AStarTile m_playerTile;
        static AStarTile m_opponentTile;
        static AStarTile[,] m_worldRepresentation;
        static List<Portal> portals;

        static ServerResponse m_parsed;

        static void Main(string[] args)
        {
            while (true)
            {
                Thread player = new Thread(PlayPlayerThread);
                Thread opponent = new Thread(PlayOpponentThread);

                player.Start();
                opponent.Start();
                Thread.Sleep(100000);
            }
        }
        static int HeuristicCalculation(AStarTile from, AStarTile to)
        {
            return Math.Abs(to.X - from.X) + Math.Abs(to.Y - from.Y);
        }

        static List<AStarTile> GetBombedSquares(int bombX, int bombY, int ownerPiercing, int ownerRange)
        {
            List<AStarTile> bombedTiles = new List<AStarTile>();

            Queue<BombSearchState> explosionFrontier = new Queue<BombSearchState>();


            int int_bombRange = ownerRange + 1;

            object object_bombPiercing;

            int int_bombPiercing = ownerPiercing;

            explosionFrontier.Enqueue(new BombSearchState(int_bombRange, int_bombPiercing, 0, bombX, bombY));
            explosionFrontier.Enqueue(new BombSearchState(int_bombRange, int_bombPiercing, 1, bombX, bombY));
            explosionFrontier.Enqueue(new BombSearchState(int_bombRange, int_bombPiercing, 2, bombX, bombY));
            explosionFrontier.Enqueue(new BombSearchState(int_bombRange, int_bombPiercing, 3, bombX, bombY));

            while (explosionFrontier.Count > 0)
            {
                BombSearchState current = explosionFrontier.Dequeue();

                //////Console.Write("\n current explosion frontier tile is" + current.X + " " + current.Y);

                bombedTiles.Add(m_worldRepresentation[current.X, current.Y]);
                if (current.ChargesLeft == 1) //TODO: THISISHACKPLZFIX
                {
                    continue;
                }


                bool shouldContinue = false;
                //TODO: check for portals
                foreach (Portal p in portals)
                {
                    BombSearchState portalState = p.GetBombOutlet(current);
                    if (portalState != null)
                    {
                        ////Console.Write("\n bomb projection has entered a portal " + current.X + " " + current.Y + " " + portalState.X + " " + portalState.Y + " left:" + portalState.ChargesLeft);
                        explosionFrontier.Enqueue(portalState);
                    }
                }


                if (shouldContinue)
                {
                    continue;
                }



                if ((m_worldRepresentation[current.X, current.Y].m_blockType == AStarTile.blockType.SoftBlock || m_worldRepresentation[current.X, current.Y].m_blockType == AStarTile.blockType.HardBlock) && !current.DestroyBlock())
                {
                    continue;
                }

                if (current.Orientation == 0)
                {
                    if (current.X - 1 < 0)
                    {
                        continue;
                    }

                    explosionFrontier.Enqueue(new BombSearchState(current.ChargesLeft - 1, current.PiercesLeft, current.Orientation, current.X - 1, current.Y));
                }
                else if (current.Orientation == 1)
                {
                    if (current.Y - 1 < 0)
                    {
                        continue;
                    }
                    explosionFrontier.Enqueue(new BombSearchState(current.ChargesLeft - 1, current.PiercesLeft, current.Orientation, current.X, current.Y - 1));

                }
                else if (current.Orientation == 2)
                {
                    if (current.X + 1 >= m_parsed.boardSize)
                    {
                        continue;
                    }

                    explosionFrontier.Enqueue(new BombSearchState(current.ChargesLeft - 1, current.PiercesLeft, current.Orientation, current.X + 1, current.Y));
                }
                else if (current.Orientation == 3)
                {
                    if (current.Y + 1 >= m_parsed.boardSize)
                    {
                        continue;
                    }

                    explosionFrontier.Enqueue(new BombSearchState(current.ChargesLeft - 1, current.PiercesLeft, current.Orientation, current.X, current.Y + 1));
                }

            }

            return bombedTiles;
        }

        static void PlayPlayerThread()
        {
            PlayGame("{\"devkey\": \"5820df82d7d4995d08393b9f\", \"username\": \"keyboardkommander\" }", "5820df82d7d4995d08393b9f");
        }


        static void PlayOpponentThread()
        {
            PlayGame("{\"devkey\": \"582b14eb8ba62d91533b1312\", \"username\": \"testaccount\" }", "582b14eb8ba62d91533b1312");

        }

        static bool PlayGame(string postData, string key)
        {
            var request = (HttpWebRequest)WebRequest.Create("http://aicomp.io/api/games/search");

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
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    ////////Console.Write(responseString);
                    if (responseString == "\"Game ID is undefined, maybe the game ended or does not exist!\"")
                    {
                        return false;
                    }
                    m_parsed = JsonConvert.DeserializeObject<ServerResponse>(responseString);
                    ////Console.Write("Data has been parsed\n" + postData);

                    m_worldRepresentation = new AStarTile[m_parsed.boardSize, m_parsed.boardSize];

                    //make the portals
                    portals = new List<Portal>();

                    List<Portal> playerOnePortals = new List<Portal>();

                    List<Portal> playerTwoPortals = new List<Portal>();

                    foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, object>>> p in m_parsed.portalMap)
                    {
                        int[] coords = p.Key.Split(',').Select(int.Parse).ToArray();
                        Dictionary<string, object> dict;

                        foreach (KeyValuePair<string, Dictionary<string, object>> kvp in p.Value)
                        {
                            object object_owner;
                            kvp.Value.TryGetValue("owner", out object_owner);
                            int int_owner;
                            int_owner = Convert.ToInt32(object_owner);
                            object object_color;
                            kvp.Value.TryGetValue("portalColor", out object_color);
                            string string_color = object_color.ToString();

                            bool bool_color = false;

                            if(string_color == "orange")
                            {
                                bool_color = true;
                            } else if (string_color == "blue")
                            {
                                bool_color = false;
                            }
                            else
                            {
                                //Console.WriteLine("Color error");
                            }

                            if (int_owner == 0)
                            {
                                playerOnePortals.Add(new Portal(coords[0], coords[1], Convert.ToInt32(kvp.Key), int_owner,bool_color));
                            }
                            else
                            {
                                playerTwoPortals.Add(new Portal(coords[0], coords[1], Convert.ToInt32(kvp.Key), int_owner, bool_color));

                            }
                        }


                    }

                    if (playerOnePortals.Count == 2)
                    {
                        Portal.LinkPortals(playerOnePortals[0], playerOnePortals[1]);
                    }


                    if (playerTwoPortals.Count == 2)
                    {
                        Portal.LinkPortals(playerTwoPortals[0], playerTwoPortals[1]);
                    }

                    portals.AddRange(playerOnePortals);

                    portals.AddRange(playerTwoPortals);

                    //TODO: use portals in A* linkages

                    for (int x = 0; x < m_parsed.boardSize; x++)
                    {
                        for (int y = 0; y < m_parsed.boardSize; y++)
                        {
                            m_worldRepresentation[x, y] = new AStarTile(x, y, m_parsed);
                        }
                    }

                    int playerBombs = 0;

                    //TODO: move this search
                    foreach (KeyValuePair<string, Dictionary<string, int>> bomb in m_parsed.bombMap)
                    {
                        int bombX = Int32.Parse(bomb.Key.Split(new Char[] { ',' })[0]);
                        int bombY = Int32.Parse(bomb.Key.Split(new Char[] { ',' })[1]);
                        //TODO: calculate if in range of bomb including portal traversal and blocking
                        int owner;
                        bomb.Value.TryGetValue("owner", out owner);

                        if (owner == m_parsed.playerIndex)
                        {
                            playerBombs++;
                        }

                        int ownerPiercing;
                        int ownerRange;

                        if (owner == m_parsed.playerIndex)
                        {
                            object object_ownerPiercing;
                            m_parsed.player.TryGetValue("bombPierce", out object_ownerPiercing);

                            ownerPiercing = Convert.ToInt32(object_ownerPiercing);


                            object object_ownerRange;
                            m_parsed.player.TryGetValue("bombRange", out object_ownerRange);

                            ownerRange = Convert.ToInt32(object_ownerRange);
                        }
                        else
                        {
                            object object_ownerPiercing;
                            m_parsed.opponent.TryGetValue("bombPierce", out object_ownerPiercing);

                            ownerPiercing = Convert.ToInt32(object_ownerPiercing);


                            object object_ownerRange;
                            m_parsed.opponent.TryGetValue("bombRange", out object_ownerRange);

                            ownerRange = Convert.ToInt32(object_ownerRange);

                        }



                        List<AStarTile> bombedSquares = GetBombedSquares(bombX, bombY, ownerPiercing, ownerRange);

                        foreach (AStarTile tile in bombedSquares)
                        {
                            int tick;
                            bomb.Value.TryGetValue("tick", out tick);
                            //////Console.Write("\n Setting " + tile.X + " " + tile.Y + " to dangerous on " + tick);

                            tile.SetDangerous(tick + 1);
                            tile.SetDangerous(tick + 2);
                        }
                    }

                    if (m_parsed.trailmap.Count > 0)
                    {
                        foreach (KeyValuePair<string, object> kvp in m_parsed.trailmap)
                        {
                            int[] bombCoords = kvp.Key.Split(',').Select(num => int.Parse(num)).ToArray();

                            m_worldRepresentation[bombCoords[0], bombCoords[1]].SetDangerous(0);
                            m_worldRepresentation[bombCoords[0], bombCoords[1]].SetDangerous(1);
                            m_worldRepresentation[bombCoords[0], bombCoords[1]].SetDangerous(2); //TODO SEEMS HACKISH
                        }
                    }



                    gameNotCompleted = m_parsed.state != "complete";

                    object object_playerOrientation;
                    m_parsed.player.TryGetValue("orientation", out object_playerOrientation);
                    int int_orientation = Convert.ToInt32(object_playerOrientation);

                    object object_playerX;
                    object object_playerY;
                    m_parsed.player.TryGetValue("x", out object_playerX);
                    m_parsed.player.TryGetValue("y", out object_playerY);

                    int int_px = Convert.ToInt32(object_playerX);
                    int int_py = Convert.ToInt32(object_playerY);

                    if (int_px == -1)
                    {
                        return false;
                    }

                    m_playerTile = m_worldRepresentation[int_px, int_py];

                    object opponentX;
                    object opponentY;
                    m_parsed.opponent.TryGetValue("x", out opponentX);
                    m_parsed.opponent.TryGetValue("y", out opponentY);
                    int int_ox = Convert.ToInt32(opponentX);
                    int int_oy = Convert.ToInt32(opponentY);

                    if (int_ox == -1)
                    {
                        return true;
                    }

                    m_opponentTile = m_worldRepresentation[int_ox, int_oy];

                    HashSet<AStarTile> closedSet = new HashSet<AStarTile>(); //the already evaluated set of nodes


                    AStarTile endingTile = null;

                    if (m_parsed.bombMap.Count == 0)
                    {
                        endingTile = m_opponentTile;
                    }


                    List<AStarBoardState> safeMoves = new List<AStarBoardState>();
                    HashSet<AStarBoardState> visited = new HashSet<AStarBoardState>();
                    Queue<AStarBoardState> nextTiles = new Queue<AStarBoardState>();

                    m_playerTile.cost = 1;



                    nextTiles.Enqueue(new AStarBoardState(m_playerTile, int_orientation,portals,m_worldRepresentation,1));


                    int playerPiercing;
                    int playerRange;
                    {
                        object object_ownerPiercing;
                        m_parsed.player.TryGetValue("bombPierce", out object_ownerPiercing);

                        playerPiercing = Convert.ToInt32(object_ownerPiercing);


                        object object_ownerRange;
                        m_parsed.player.TryGetValue("bombRange", out object_ownerRange);

                        playerRange = Convert.ToInt32(object_ownerRange);
                    }

                    //BFS search to find all safe tiles
                    while (nextTiles.Count > 0)
                    {
                        AStarBoardState current = nextTiles.Dequeue();

                        ////Console.WriteLine(nextTiles.Count + " "  + visited.Count + " Visiting:" + current.m_projectedPlayerTile.X + " " + current.m_projectedPlayerTile.Y);

                        bool shouldContinue = false;

                        foreach(AStarBoardState it in visited)
                        {
                            bool different = false;

                            for(int i = 0; i < it.m_portals.Count; i++)
                            {
                                if(it.m_portals[i].m_x == current.m_portals[i].m_x && it.m_portals[i].m_y == current.m_portals[i].m_y)
                                {
                                }
                                else
                                {
                                    different = true;
                                }
                            }

                            if (!different && (it.m_projectedPlayerTile.X == current.m_projectedPlayerTile.X && it.m_projectedPlayerTile.Y == current.m_projectedPlayerTile.Y && it.m_projectedPlayerOrientation == current.m_projectedPlayerOrientation))
                            {
                                shouldContinue = true;
                            }
                        }


                        if (shouldContinue) //TODO: This will no longer work fix this
                        {
                            continue;
                        }

                        visited.Add(current);
                        



                        //TODO:this is a little bit tricky, try to solve it I guess
                        foreach (Portal p in portals)
                        {
                            AStarBoardState neighbor = p.GetTileOutlet(current);
                            if (neighbor != null)
                            {
                                if (neighbor.m_cameFrom == null) //TODO: this is not write, it should overwrite. This is a hack fix. Plzfix
                                {
                                    neighbor.m_cameFrom = current;
                                }
                                nextTiles.Enqueue(neighbor);
                            }
                        }

                        if (current.m_projectedPlayerTile.m_blockType == AStarTile.blockType.Passable)
                        {


                            nextTiles.Enqueue(current.TurnDown(current));

                            nextTiles.Enqueue(current.TurnLeft(current));
                            nextTiles.Enqueue(current.TurnRight(current));
                            nextTiles.Enqueue(current.TurnUp(current));

                            nextTiles.Enqueue(current.ShootBluePortal(current));
                            nextTiles.Enqueue(current.ShootORangePortal(current));
                            if (current.m_projectedPlayerTile.X + 1 < m_parsed.boardSize)
                            {
                               
                                nextTiles.Enqueue(current.MoveLeft(current));
                            }

                            if (current.m_projectedPlayerTile.Y + 1 < m_parsed.boardSize)
                            {
                                nextTiles.Enqueue(current.MoveDown(current));
                            }

                            if (current.m_projectedPlayerTile.X - 1 > 0)
                            {
                                nextTiles.Enqueue(current.MoveRight(current));
                            }

                            if (current.m_projectedPlayerTile.Y - 1 > 0)
                            {
                                nextTiles.Enqueue(current.MoveUp(current));
                            }


                            if (current.m_projectedPlayerTile.SafeOnStep(current.m_cost) && !(current.m_projectedPlayerTile.X == m_opponentTile.X && current.m_projectedPlayerTile.Y == m_opponentTile.Y) )
                            {
                                safeMoves.Add(current);
                            }
                            else
                            {
                                ////Console.Write(current.m_projectedPlayerTile.X + " " + current.m_projectedPlayerTile.Y + " is not safe");
                            }
                        }


                    }


                    //if we can drop a bomb and survive do that
                    bool canBomb = false;


                    List<AStarBoardState> superDuperSafeMoves = new List<AStarBoardState>(); //safe moves with paths consisting of safe moves


                    foreach (AStarBoardState tile in safeMoves)
                    {
                        if (tile.m_projectedPlayerTile.GetDangerousTurns().Count > 0)
                        {
                            continue;
                        }

                        bool isSuperDuperSafe = true;
                        AStarBoardState it = tile;

                        foreach (int dangerous in tile.m_projectedPlayerTile.GetDangerousTurns())
                        {
                            ////Console.WriteLine("Dangerous on " + dangerous);
                        }
                        do
                        {
                            if (!(it.m_projectedPlayerTile.SafeOnStep((it.m_projectedPlayerTile.cost)) && it.m_projectedPlayerTile.SafeOnStep((it.m_projectedPlayerTile.cost + 1)) && it.m_projectedPlayerTile.SafeOnStep((it.m_projectedPlayerTile.cost - 1)))) //TODO: consider passing manual check here
                            {
                                ////Console.Write("\n XXXX" + it.X + " " + it.Y + " is not safe " + ((it.cost - 1) * 2) + " the only time when the player would cross it");
                                isSuperDuperSafe = false;
                            }
                            else
                            {
                                ////Console.Write("\n " + it.X + " " + it.Y + " is compleely safe on turn " + ((it.cost - 1) * 2) + " the only time when the player would cross it");
                            }


                            foreach (int dangerousTurns in it.m_projectedPlayerTile.GetDangerousTurns())
                            {
                                ////Console.Write("\n\t\t" + dangerousTurns);
                            }

                            
                            it = it.m_cameFrom;
                        } while (isSuperDuperSafe == true && it != null && it.m_cameFrom != null /*TODO: this seems sloppy*/ && !(it.m_cameFrom.m_projectedPlayerTile.X == m_playerTile.X && it.m_cameFrom.m_projectedPlayerTile.Y == m_playerTile.Y) );
                        

                        if (isSuperDuperSafe)
                        {
                            //////Console.Write("\n Adding" + tile.X + " " + tile.Y + " because the path too it is safe if timed correctly.");
                            superDuperSafeMoves.Add(tile);
                        }
                    }

                    foreach (AStarBoardState tile in superDuperSafeMoves)
                    {

                        List<AStarTile> hypotheticalBombedTiles = GetBombedSquares(tile.m_projectedPlayerTile.X, tile.m_projectedPlayerTile.Y, playerPiercing, playerRange); //TODO: account for tiles that will be destroyed then
                        int bombCount = 0;
                        foreach (AStarTile t in hypotheticalBombedTiles)
                        {
                            if (t.m_blockType == AStarTile.blockType.SoftBlock || (m_opponentTile.X == t.X && m_opponentTile.Y == t.Y))
                            {
                                bombCount++;
                            }
                        }

                        tile.m_projectedPlayerTile.m_numTargets = bombCount;

                        int safeTilesCount = 0;
                        foreach(AStarBoardState t in superDuperSafeMoves)
                        {
                            bool isSafe = true;
                            foreach (AStarTile bombedTile in hypotheticalBombedTiles)
                            {
                                if(t.m_projectedPlayerTile.X == bombedTile.X && t.m_projectedPlayerTile.Y == bombedTile.Y)
                                {
                                    isSafe = false;
                                }
                            }

                            if (isSafe && t.m_projectedPlayerTile.cost <= 7)
                            {
                                safeTilesCount++;
                            }
                        }


                        if (safeTilesCount == 0)
                        {
                            tile.m_projectedPlayerTile.m_numTargets = 0;
                        }
                    }



                    List<AStarTile> bTiles = GetBombedSquares(m_playerTile.X, m_playerTile.Y, playerPiercing, playerRange);


                    //TODO: stop looking for safe havens just try to stay alive the longest
                    List<AStarBoardState> safeHavens = new List<AStarBoardState>();// = superDuperSafeMoves.Except(bTiles).ToList();

                    foreach(AStarBoardState state in superDuperSafeMoves)
                    {
                        bool isSafeHaven = true;

                        foreach(AStarTile t in bTiles)
                        {
                            if(state.m_projectedPlayerTile.X == t.X && state.m_projectedPlayerTile.Y == t.Y)
                            {
                                isSafeHaven = false;
                            }
                        }

                        if (isSafeHaven)
                        {
                            safeHavens.Add(state);
                        }
                    }

                    foreach(AStarTile tile in bTiles)
                    {
                        List<AStarBoardState> toRemove = new List<AStarBoardState>();
                        foreach(AStarBoardState state in safeHavens)
                        {
                            if(state.m_projectedPlayerTile.X == tile.X && state.m_projectedPlayerTile.Y == tile.Y)
                            {
                                toRemove.Add(state);
                            }
                        }

                        foreach (AStarBoardState r in toRemove)
                        {
                            safeHavens.Remove(r);
                        }
                    }


                    int availableBombs;

                    object object_availableBombs;

                    m_parsed.player.TryGetValue("bombCount", out object_availableBombs);

                    availableBombs = Convert.ToInt32(object_availableBombs);


                    foreach (AStarBoardState haven in safeHavens)
                    {
                        bool isSuperSafeRoute = true;
                        AStarBoardState it = haven;

                        while(it.m_projectedPlayerTile.X == m_playerTile.X && it.m_projectedPlayerTile.Y == m_playerTile.Y)
                        {
                            //Console.WriteLine("Bombing Route: " + it.m_projectedPlayerTile.X + " " + it.m_projectedPlayerTile.Y);
                            if(!it.m_projectedPlayerTile.isSafeUntil(7)) //TODO: this is broken. We don't need to look this far ahead
                            {
                                isSuperSafeRoute = false;
                            }
                            it = haven.m_cameFrom;
                        }


                        ////Console.WriteLine("haven.cost:" + haven.cost);
                        if (m_playerTile.isSafeUntil(7) && haven.m_cost <= 7  && isSuperSafeRoute && playerBombs < availableBombs && !m_parsed.bombMap.ContainsKey(m_playerTile.X + "," + m_playerTile.Y))//TODO: calculate how many bombs you have and drop that many.
                        {
                            Console.WriteLine("The tile we can bomb at is:" + haven.m_projectedPlayerTile.X + " " + haven.m_projectedPlayerTile.Y);
                            canBomb = true;
                        }
                    }


                    AStarBoardState targetTile = null;

                    //Pick your target tile
                    if (superDuperSafeMoves.Count == 0)
                    {
                        targetTile = null;
                    }
                    else
                    {
                        foreach (AStarBoardState tile in superDuperSafeMoves)
                        {

                            ////Console.Write("\n" + tile.X + " " + tile.Y + " " + tile.m_numTargets / (float)tile.cost);
                            if (targetTile == null)
                            {
                                targetTile = tile;
                                continue;
                            }

                            if (tile.m_projectedPlayerTile.m_numTargets / (float)tile.m_cost > targetTile.m_projectedPlayerTile.m_numTargets / (float)targetTile.m_cost && tile.m_projectedPlayerTile.isSafeUntil(2))
                            {
                                targetTile = tile;
                            }

                        }
                    }


                    AStarBoardState origTargetTile = targetTile;
                    while (/*TODO: plzno*/ targetTile != null && targetTile.m_cameFrom != null && !(targetTile.m_cameFrom.m_projectedPlayerTile.X == m_playerTile.X && targetTile.m_cameFrom.m_projectedPlayerTile.Y == m_playerTile.Y) && !(targetTile.m_projectedPlayerTile.X == m_playerTile.X && targetTile.m_projectedPlayerTile.Y == m_playerTile.Y))
                    {
                        targetTile = targetTile.m_cameFrom;
                    }

                    //Console.Write("\nsafemoves:");
                    foreach (AStarBoardState safemove in safeMoves)
                    {
                        //Console.Write("\n" + safemove.m_projectedPlayerTile.X + " " + safemove.m_projectedPlayerTile.Y);
                    }

                    //Console.Write("\nsupersafemoves:");
                    foreach (AStarBoardState safemove in superDuperSafeMoves)
                    {
                        //Console.Write("\n" + safemove.m_projectedPlayerTile.X + " " + safemove.m_projectedPlayerTile.Y);
                    }

                    //Console.Write("\nSuperDuperSafeMoves:");
                    foreach (AStarBoardState safeMove in superDuperSafeMoves)
                    {
                        //Console.Write("\n" + safeMove.m_projectedPlayerTile.X + " " + safeMove.m_projectedPlayerTile.Y);
                    }
                    ////Console.Write("\n");
                    ////Console.Write("\nTarget tile final:" + targetTile.X + " " + targetTile.Y + " " + targetTile.m_blockType + " Target Tile Original" + origTargetTile.X + " " + origTargetTile.Y + "\n");
                    ////Console.Write("\nMy position:" + m_playerTile.X + " " + m_playerTile.Y + "\n");
                    if (origTargetTile == null || (origTargetTile.m_projectedPlayerTile.X == m_playerTile.X && origTargetTile.m_projectedPlayerTile.Y == m_playerTile.Y))
                    {
                        chosenAction = "";
                    }
                    else
                    {
                        chosenAction = targetTile.m_moveToGetHere;
                    }

                    bool hasBenefit = false;

                    List<AStarTile> bombedTiles = bTiles;
                    List<AStarTile> bombableSoftBlocks = bombedTiles.Where(item => item.m_blockType == AStarTile.blockType.SoftBlock).ToList();

                    if (bombedTiles.Contains(m_opponentTile) || bombableSoftBlocks.Count > 0)
                    {
                        hasBenefit = true;
                    }


                    if (canBomb && hasBenefit)
                    {
                        chosenAction = "b";
                    }
                    else if (m_playerTile.IsSuperSafe())
                    {

                        object object_coins;
                        int int_coins;

                        int bombRangeCost = 5;
                        int bombCountCost = 5;
                        int bombPierceCost = 5;


                        object object_bombRange;
                        object object_bombCount;
                        object object_bombPierce;

                        int int_bombRange;
                        int int_bombCount;
                        int int_bombPierce;

                        m_parsed.player.TryGetValue("bombRange", out object_bombRange);
                        m_parsed.player.TryGetValue("bombCount", out object_bombCount);
                        m_parsed.player.TryGetValue("bombPierce", out object_bombPierce);

                        int_bombRange = Convert.ToInt32(object_bombRange);
                        int_bombCount = Convert.ToInt32(object_bombCount);
                        int_bombPierce = Convert.ToInt32(object_bombPierce);

                        int m_maxBombCount = 2;
                        int m_maxBombPierce = 8;
                        int m_maxBombRange = 8;


                        int mostNeededCost = 0;

                        m_parsed.player.TryGetValue("coins", out object_coins);
                        int_coins = Convert.ToInt32(object_coins);

                        if (int_bombCount < m_maxBombCount && int_coins >= bombCountCost)
                        {
                            chosenAction = "buy_count";
                            mostNeededCost = bombCountCost;
                        }
                        else if (int_bombRange < int_bombPierce && int_bombRange < m_maxBombRange && int_coins >= bombRangeCost)
                        {
                            chosenAction = "buy_range";
                            mostNeededCost = bombRangeCost;
                        }
                        else if (int_bombPierce < m_maxBombPierce && int_coins >= bombPierceCost)
                        {
                            chosenAction = "buy_pierce";
                            mostNeededCost = bombPierceCost;
                        }



                        if (chosenAction == "")
                        {
                            //TODO: fix portal dropping, only do if safe chosenAction = m_actions[m_random.Next(m_actions.Length)];
                        }

                    }


                    ////Console.Write("Canbomb:" + canBomb + " hasBenefit:" + hasBenefit);

                    //find out which position maps to it

                    //move to that position

                    //////Console.Write(m_parsed.playerID);
                    //////Console.Write(m_parsed.gameID);

                    //Console.WriteLine("ChosenAction:" + chosenAction);
                    request = (HttpWebRequest)WebRequest.Create("http://aicomp.io/api/games/submit/" + m_parsed.gameID);
                    postData = "{\"devkey\": \"" + key + "\", \"playerID\": \"" + m_parsed.playerID + "\", \"move\": \"" + chosenAction/*m_actions[m_random.Next(0, m_actions.Length)]*/ + "\" }";
                    data = Encoding.ASCII.GetBytes(postData);
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    request.ContentLength = data.Length;

                    // the code that you want to measure comes here
                    watch.Stop();

                    ////Console.WriteLine(watch.ElapsedMilliseconds);


                    ////Console.Write("\nAction is:" + chosenAction + "\n");

                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                    Thread.Sleep(100);
                    response = (HttpWebResponse)request.GetResponse();
                    Thread.Sleep(100);
                }
                //////Console.Write("Start Iteration");
            } while (gameNotCompleted);

            return false;
        }

    }
}
