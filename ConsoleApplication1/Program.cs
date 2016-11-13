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



        public bool IsSuperSafe()
        {
            return turnsUntilDangerous.Count == 0;
        }

        public List<int> GetDangerousTurns()
        {
            return turnsUntilDangerous;
        }

        public bool IsSafe()
        {
            return SafeOnStep(0);
        }

        public bool SafeOnStep(int step)
        {
            if (turnsUntilDangerous.Count == 0)
            {
                ////Console.Write("\n" + X + " " + Y + " is completely safe");
                return true;
            }

            foreach (int dangerousTurn in turnsUntilDangerous)
            {
                if (dangerousTurn == step) //TODO: This is extremely sloppy coding please fix this
                {
                    ////Console.Write("\n" + X + " " + Y + " is not safe " + dangerousTurn + " is too similar to " + step);
                    return false;
                }
            }

            ////Console.Write("\n" + X + " " + Y + " is safe on turn" + step + " it is unsafe on:");

            foreach (int dangerousTurn in turnsUntilDangerous)
            {
                ////Console.Write("\n" + dangerousTurn);
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
    }



    class Program
    {
        public class Portal
        {
            private Portal m_linkedPortal;
            private int m_x;
            private int m_y;
            private int m_orientation;
            public int m_owner;
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

            public AStarTile GetTileOutlet(AStarTile inlet)
            {
                if (!((inlet.Y == this.m_y && inlet.X - 1 == this.m_x) && this.m_orientation == 2) /*inlet is left and this is right*/
                    || ((inlet.Y == this.m_y && inlet.X + 1 == this.m_x) && this.m_orientation == 0 /*inlet is right and this is left*/
                    || ((inlet.Y == this.m_y - 1 && inlet.X == this.m_x) && this.m_orientation == 3) /*inlet is up and this is down*/
                    || ((inlet.Y == this.m_y + 1 && inlet.X == this.m_x) && this.m_orientation == 1) /*inlet is down and this is up*/

                    ))
                {
                    return null;
                }

                if (m_linkedPortal == null)
                {
                    return null;
                }

                AStarTile ret = m_linkedPortal.OutletAStarTile();

                if (this.m_orientation == 2) //TODO: check these orientations
                {
                    ret.MoveToGetHere = "mr";
                }
                else if (this.m_orientation == 0)
                {
                    ret.MoveToGetHere = "ml";
                }
                else if (this.m_orientation == 3)
                {
                    ret.MoveToGetHere = "mu";
                }
                else
                {
                    ret.MoveToGetHere = "md";
                }



                return ret;

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

            public Portal(int x, int y, int orientation, int owner)
            {
                m_owner = owner;
                m_x = x;
                m_y = y;
                m_orientation = orientation;
            }
        }

        static string[] m_actions = { /*"mu", "ml", "md", "mr", "tu", "tl", "td", "tr", "b", */"op", "bp" };
        static string[] m_buyActions = { "buy_count", "buy_pierce", "buy_range", "buy_block" };
        static Random m_random;
        static AStarTile m_playerTile;
        static AStarTile m_opponentTile;
        static AStarTile[,] m_worldRepresentation;
        static List<Portal> portals;

        static ServerResponse m_parsed;

        static void Main(string[] args)
        {
            m_random = new Random(0);
            PlayGame();
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

                ////Console.Write("\n current explosion frontier tile is" + current.X + " " + current.Y);

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


                if (m_worldRepresentation[current.X, current.Y].m_blockType == AStarTile.blockType.HardBlock)
                {
                    continue;
                }

                if (m_worldRepresentation[current.X, current.Y].m_blockType == AStarTile.blockType.SoftBlock && !current.DestroyBlock())
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

        static void PlayGame()
        {
            while (true)
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
                bool gameNotCompleted = true;
                do
                {

                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string chosenAction = "";
                        var responseString = reader.ReadToEnd();
                        var watch = System.Diagnostics.Stopwatch.StartNew();
                        ////Console.Write(responseString);
                        m_parsed = JsonConvert.DeserializeObject<ServerResponse>(responseString);
                        Console.Write("Data has been parsed\n");

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

                                if (int_owner == 0)
                                {
                                    playerOnePortals.Add(new Portal(coords[0], coords[1], Convert.ToInt32(kvp.Key), int_owner));
                                }
                                else
                                {
                                    playerTwoPortals.Add(new Portal(coords[0], coords[1], Convert.ToInt32(kvp.Key), int_owner));

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
                                ////Console.Write("\n Setting " + tile.X + " " + tile.Y + " to dangerous on " + tick);

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

                        m_playerTile.cost = 0;
                        nextTiles.Enqueue(m_playerTile);


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
                            AStarTile current = nextTiles.Dequeue();

                            if (visited.Contains(current))
                            {
                                continue;
                            }

                            visited.Add(current);

                            List<AStarTile> neighbors = new List<AStarTile>();



                            //TODO:this is a little bit tricky, try to solve it I guess
                            foreach (Portal p in portals)
                            {
                                AStarTile outlet = p.GetTileOutlet(current);
                                if (outlet != null)
                                {
                                    neighbors.Add(outlet);
                                }
                            }

                            if (current.m_blockType == AStarTile.blockType.Passable)
                            {

                                if (current.X + 1 < m_parsed.boardSize)
                                {
                                    neighbors.Add(m_worldRepresentation[current.X + 1, current.Y]);
                                }

                                if (current.Y + 1 < m_parsed.boardSize)
                                {
                                    neighbors.Add(m_worldRepresentation[current.X, current.Y + 1]);
                                }

                                if (current.X - 1 > 0)
                                {
                                    neighbors.Add(m_worldRepresentation[current.X - 1, current.Y]);
                                }

                                if (current.Y - 1 > 0)
                                {
                                    neighbors.Add(m_worldRepresentation[current.X, current.Y - 1]);
                                }

                                if (current.SafeOnStep(current.cost * 2))
                                {
                                    safeMoves.Add(current);
                                }
                            }



                            foreach (AStarTile neighbor in neighbors)
                            {
                                if (neighbor.CameFrom == null) //TODO: this is not write, it should overwrite. This is a hack fix. Plzfix
                                {
                                    neighbor.CameFrom = current;
                                }
                                if (neighbor.cost < 0 || current.cost + 1 < neighbor.cost)
                                {
                                    neighbor.cost = current.cost + 1;
                                }
                                nextTiles.Enqueue(neighbor);
                            }
                        }


                        //if we can drop a bomb and survive do that
                        bool canBomb = false;
                        object orientation;
                        m_parsed.player.TryGetValue("orientation", out orientation);
                        int int_orientation = Convert.ToInt32(orientation);


                        List<AStarTile> superSafeMoves = safeMoves.Where(item => item.IsSuperSafe()).ToList(); //moves that are always safe

                        List<AStarTile> superDuperSafeMoves = new List<AStarTile>(); //safe moves with paths consisting of safe moves


                        foreach (AStarTile tile in superSafeMoves)
                        {
                            bool isSuperDuperSafe = true;
                            AStarTile it = tile;
                            do
                            {
                                if (!it.SafeOnStep((it.cost) * 2)) //TODO: consider passing manual check here
                                {
                                    ////Console.Write("\n XXXX" + it.X + " " + it.Y + " is not safe " + ((it.cost) * 2) + " the only time when the player would cross it");
                                    isSuperDuperSafe = false;
                                }
                                //else
                                //{
                                //    ////Console.Write("\n " + it.X + " " + it.Y + " is compleely safe on turn " + ((it.cost) * 2) + " the only time when the player would cross it");
                                //}
                                ////Console.Write("\n\t" + it.X + " " + it.Y + " is not safe on:");

                                //foreach (int dangerousTurns in it.GetDangerousTurns())
                                //{
                                //    ////Console.Write("\n\t\t" + dangerousTurns);
                                //}

                                it = it.CameFrom;
                            } while (isSuperDuperSafe == true && it != null && it.CameFrom != m_playerTile && !(it.X == m_playerTile.X && it.Y == m_playerTile.Y));

                            if (!it.SafeOnStep((it.cost) * 2))
                            {
                                isSuperDuperSafe = false;
                            }

                            if (isSuperDuperSafe)
                            {
                                ////Console.Write("\n Adding" + tile.X + " " + tile.Y + " because the path too it is safe if timed correctly.");
                                superDuperSafeMoves.Add(tile);
                            }
                        }

                        foreach(AStarTile tile in superDuperSafeMoves)
                        {

                            List<AStarTile> hypotheticalBombedTiles = GetBombedSquares(tile.X, tile.Y, playerPiercing, playerRange);
                            int bombCount = 0;
                            foreach (AStarTile t in hypotheticalBombedTiles)
                            {
                                if (t.m_blockType == AStarTile.blockType.SoftBlock)
                                {
                                    bombCount++;
                                }
                            }

                            tile.m_numTargets = bombCount;
                            
                            if(superDuperSafeMoves.Except(hypotheticalBombedTiles).ToList().Count == 0){
                                tile.m_numTargets = 0;
                            }
                        }



                        List<AStarTile> bTiles = GetBombedSquares(m_playerTile.X, m_playerTile.Y, playerPiercing, playerRange);

                        List<AStarTile> safeHavens = superDuperSafeMoves.Except(bTiles).ToList();

                        int availableBombs;

                        object object_availableBombs;

                        m_parsed.player.TryGetValue("bombCount", out object_availableBombs);

                        availableBombs = Convert.ToInt32(object_availableBombs);


                        foreach (AStarTile haven in safeHavens)
                        {
                            if (haven.cost <= 3 && playerBombs < availableBombs)//TODO: calculate how many bombs you have and drop that many.
                            {
                                canBomb = true;
                            }
                        }


                        AStarTile targetTile = null;

                        //Pick your target tile
                        if (superDuperSafeMoves.Count == 0)
                        {
                            targetTile = m_playerTile;
                        }
                        else
                        {
                            foreach (AStarTile tile in superDuperSafeMoves)
                            {
                                if (targetTile == null)
                                {
                                    targetTile = tile;
                                    continue;
                                }

                                if (tile.m_numTargets / (float)tile.cost > targetTile.m_numTargets / (float)targetTile.cost)
                                {
                                    targetTile = tile;
                                }

                                Console.Write(tile.X + " " + tile.Y + " " + tile.m_numTargets / (float)tile.cost);

                            }
                        }


                        AStarTile origTargetTile = targetTile;
                        while (targetTile.CameFrom != m_playerTile && targetTile.CameFrom != null)
                        {
                            targetTile = targetTile.CameFrom;
                        }

                        //////Console.Write("\nSafeMoves:");
                        //foreach (AStarTile safeMove in safeMoves)
                        //{
                        //    ////Console.Write("\n" + safeMove.X + " " + safeMove.Y);
                        //}

                        //////Console.Write("\nSuperSafeMoves:");
                        //foreach (AStarTile safeMove in superSafeMoves)
                        //{
                        //    ////Console.Write("\n" + safeMove.X + " " + safeMove.Y);
                        //}

                        //////Console.Write("\nSuperDuperSafeMoves:");
                        //foreach (AStarTile safeMove in superDuperSafeMoves)
                        //{
                        //    ////Console.Write("\n" + safeMove.X + " " + safeMove.Y);
                        //}
                        ////Console.Write("\n");
                        ////Console.Write("\nTarget tile final:" + targetTile.X + " " + targetTile.Y + " " + targetTile.m_blockType + "\n");
                        ////Console.Write("\nMy position:" + m_playerTile.X + " " + m_playerTile.Y + "\n");

                        if (origTargetTile.X == m_playerTile.X && origTargetTile.Y == m_playerTile.Y)
                        {
                            chosenAction = "";
                        }

                        else if (targetTile.X > m_playerTile.X)
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


                        if (targetTile.MoveToGetHere != "")
                        {
                            chosenAction = targetTile.MoveToGetHere;
                            Console.Write("Making portal move: " + chosenAction);
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


                        if (m_parsed.bombMap.Count == 0 && m_random.Next(10) < 3) { chosenAction = m_actions[m_random.Next(m_actions.Length)]; }
                        //find out which position maps to it

                        //move to that position

                        ////Console.Write(m_parsed.playerID);
                        ////Console.Write(m_parsed.gameID);
                        request = (HttpWebRequest)WebRequest.Create("http://aicomp.io/api/games/submit/" + m_parsed.gameID);
                        postData = "{\"devkey\": \"5820df82d7d4995d08393b9f\", \"playerID\": \"" + m_parsed.playerID + "\", \"move\": \"" + chosenAction/*m_actions[m_random.Next(0, m_actions.Length)]*/ + "\" }";
                        data = Encoding.ASCII.GetBytes(postData);
                        request.Method = "POST";
                        request.ContentType = "application/json";
                        request.ContentLength = data.Length;

                        // the code that you want to measure comes here
                        watch.Stop();

                        Console.WriteLine(watch.ElapsedMilliseconds);


                        ////Console.Write("\nAction is:" + chosenAction + "\n");

                        using (var stream = request.GetRequestStream())
                        {
                            stream.Write(data, 0, data.Length);
                        }
                        response = (HttpWebResponse)request.GetResponse();
                    }
                    ////Console.Write("Sleeping");
                    System.Threading.Thread.Sleep(100);
                    ////Console.Write("Start Iteration");
                } while (gameNotCompleted);


            }
        }
    }
}
