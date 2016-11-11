﻿using System;
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
        private List<int> turnsUntilDangerous; //if this is 0 then it is currently dangerous

        public bool IsSuperSafe()
        {
            return turnsUntilDangerous.Count == 0;
        }

        public bool IsSafe()
        {
            return SafeOnStep(0);
        }

        public bool SafeOnStep(int step)
        {
            if (turnsUntilDangerous.Count == 0)
            {
                Console.Write("\n" + X + " " + Y + " is completely safe");
                return true;
            }

            foreach (int dangerousTurn in turnsUntilDangerous)
            {
                if (dangerousTurn == step) //TODO: This is extremely sloppy coding please fix this
                {
                    Console.Write("\n" + X + " " + Y + " is not safe " + dangerousTurn + " is too similar to " + step);
                    return false;
                }
            }

            Console.Write("\n" + X + " " + Y + " is safe on turn" + step + " it is unsafe on:");

            foreach (int dangerousTurn in turnsUntilDangerous)
            {
                Console.Write("\n" + dangerousTurn);
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
        public float CostFromStart;
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

                //flip the orientation
                return new BombSearchState(inlet.ChargesLeft - 1, inlet.PiercesLeft, m_linkedPortal.m_orientation, m_linkedPortal.m_x, m_linkedPortal.m_y); //TODO: check if charges is right

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

                return m_linkedPortal.OutletAStarTile();

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
                if (current.ChargesLeft == 0)
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
                        explosionFrontier.Enqueue(portalState);
                        shouldContinue = true;
                    }
                }


                if (shouldContinue)
                {
                    continue;
                }


                bombedTiles.Add(m_worldRepresentation[current.X, current.Y]);

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

                        explosionFrontier.Enqueue(new BombSearchState(current.ChargesLeft - 1, current.PiercesLeft, current.Orientation, current.X - 1, current.Y));
                        break;
                    case 1:
                        if (current.Y - 1 < 0)
                        {
                            continue;
                        }
                        explosionFrontier.Enqueue(new BombSearchState(current.ChargesLeft - 1, current.PiercesLeft, current.Orientation, current.X, current.Y - 1));
                        break;
                    case 2:
                        if (current.X + 1 >= m_parsed.boardSize)
                        {
                            continue;
                        }

                        explosionFrontier.Enqueue(new BombSearchState(current.ChargesLeft - 1, current.PiercesLeft, current.Orientation, current.X + 1, current.Y));
                        break;
                    case 3:
                        if (current.Y + 1 >= m_parsed.boardSize)
                        {
                            continue;
                        }

                        explosionFrontier.Enqueue(new BombSearchState(current.ChargesLeft - 1, current.PiercesLeft, current.Orientation, current.X, current.Y + 1));
                        break;
                }
            }

            return bombedTiles;
        }

        static void PlayGame()
        {

            while (true)
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


                        //TODO: move this search
                        foreach (KeyValuePair<string, Dictionary<string, int>> bomb in m_parsed.bombMap)
                        {
                            int bombX = Int32.Parse(bomb.Key.Split(new Char[] { ',' })[0]);
                            int bombY = Int32.Parse(bomb.Key.Split(new Char[] { ',' })[1]);
                            //TODO: calculate if in range of bomb including portal traversal and blocking
                            List<AStarTile> bombedSquares = GetBombedSquares(bombX, bombY);

                            foreach (AStarTile tile in bombedSquares)
                            {
                                int tick;
                                bomb.Value.TryGetValue("tick", out tick);
                                tile.SetDangerous(tick-1);
                                tile.SetDangerous(tick);
                                tile.SetDangerous(tick+1);
                                tile.SetDangerous(tick + 2);

                                tile.SetDangerous(tick + 3);
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

                            if (current.SafeOnStep(current.cost * 2))
                            {
                                safeMoves.Add(current);
                            }

                            List<AStarTile> neighbors = new List<AStarTile>();

                            foreach (Portal p in portals)
                            {
                                AStarTile outlet = p.GetTileOutlet(current);
                                if (outlet != null)
                                {
                                    neighbors.Add(outlet);
                                }
                            }


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
                                    isSuperDuperSafe = false;
                                }
                                else
                                {
                                    Console.Write("\n " + it.X +" " + it.Y + " is compleely safe on turn " + ((it.cost +1)*2) + " the only time when the player would cross it");
                                }
                                it = it.CameFrom;
                            } while (it != null && it.CameFrom != m_playerTile && !(it.X == m_playerTile.X && it.Y == m_playerTile.Y));

                            if (!it.SafeOnStep((it.cost) * 2))
                            {
                                isSuperDuperSafe = false;
                            }

                            if (isSuperDuperSafe)
                            {
                                Console.Write("\n Adding" + tile.X + " " + tile.Y + " because the path too it is safe if timed correctly.");
                                superDuperSafeMoves.Add(tile);
                            }
                        }

                        List<AStarTile> bTiles = GetBombedSquares(m_playerTile.X, m_playerTile.Y);

                        List<AStarTile> safeHavens = superDuperSafeMoves.Except(bTiles).ToList();
                        

                        foreach(AStarTile haven in safeHavens)
                        {
                            if(HeuristicCalculation(m_playerTile,haven) <= 2 && m_parsed.bombMap.Count == 0 && haven.X != m_playerTile.X && haven.Y != m_playerTile.Y)//TODO: calculate how many bombs you have and drop that many.
                            {
                                canBomb = true;
                            }
                        }
                        

                        AStarTile targetTile;
                        //Pick your target tile
                        if (superDuperSafeMoves.Count == 0)
                        {
                            targetTile = m_playerTile;
                        }
                        else {
                            targetTile = superDuperSafeMoves[m_random.Next(0, superDuperSafeMoves.Count)];
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

                            //TODO: convert portals to dictionary by position

                            foreach (Portal p in portals)
                            {
                                AStarTile outlet = p.GetTileOutlet(current);

                                if (outlet != null)
                                {
                                    neighbors.Add(outlet);
                                }
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
                                if (neighbor.CameFrom == null) //TODO: HACKFIXPLZFIX
                                {
                                    neighbor.CameFrom = current;
                                    neighbor.CostFromStart = tentativeScore;
                                }
                                neighbor.EstimatedCostToGoal = HeuristicCalculation(neighbor, endingTile) + current.CostFromStart;

                            }
                        }
                        AStarTile origTargetTile = targetTile;
                        while (targetTile.CameFrom != m_playerTile && targetTile.CameFrom != null)
                        {
                            targetTile = targetTile.CameFrom;
                        }

                        Console.Write("\nSafeMoves:");
                        foreach (AStarTile safeMove in safeMoves)
                        {
                            Console.Write("\n" + safeMove.X + " " + safeMove.Y);
                        }

                        Console.Write("\nSuperSafeMoves:");
                        foreach (AStarTile safeMove in superSafeMoves)
                        {
                            Console.Write("\n" + safeMove.X + " " + safeMove.Y);
                        }

                        Console.Write("\nSuperDuperSafeMoves:");
                        foreach (AStarTile safeMove in superDuperSafeMoves)
                        {
                            Console.Write("\n" + safeMove.X + " " + safeMove.Y);
                        }
                        Console.Write("\n");
                        Console.Write("\nTarget tile final:" + targetTile.X + " " + targetTile.Y + " " + targetTile.m_blockType + "\n");
                        Console.Write("\nMy position:" + m_playerTile.X + " " + m_playerTile.Y + "\n");

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
        }
    }
}
