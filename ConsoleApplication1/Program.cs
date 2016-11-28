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
            return m_piercesLeft > -1 ? true : false;
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

    public class Tile
    {
        public enum blockType
        {
            HardBlock,
            SoftBlock,
            Passable
        }

        private blockType m_blockType;

        public blockType GetBlockType()
        {
            return m_blockType;
        }

        public int m_numTargets;
        public int m_bombTick = 99999;

        public int X;
        public int Y;

        public Tile CameFrom;


        public Tile(int x, int y, ServerResponse server)
        {
            m_blockType = blockType.Passable;
            if (1 == server.hardBlockBoard[x * server.boardSize + y])
            {
                m_blockType = blockType.HardBlock;
            }
            if (1 == server.softBlockBoard[x * server.boardSize + y])
            {
                m_blockType = blockType.SoftBlock;
            }


            X = x;
            Y = y;
        }

        public Tile(int x, int y, blockType bt, int tick)
        {
            X = x;
            Y = y;
            m_bombTick = tick;
            m_blockType = bt;
        }
    }



    class Program
    {
        public static System.Diagnostics.Stopwatch watch;
        public static int enemyBombs = 0;
        public struct ShortenedBoardState
        {
            public int m_x;
            public int m_y;
            public int m_orientation;
            public int m_cost;
            public float m_stateScore;
            public float m_bombCount;
            public float m_portalCount;
        }


        public class AStarBoardState
        {
            public Tile m_projectedPlayerTile;
            public int m_projectedPlayerOrientation;

            public int m_cost;
            public int m_coinsAvailable;
            public int m_coinsEarned;

            public string m_moveToGetHere;


            public Dictionary<KeyValuePair<int, int>, Portal> m_portals;
            public Tile[,] m_boardState;
            public AStarBoardState m_cameFrom;

            public int m_range;
            public int m_count;
            public int m_pierce;

            public bool m_dead = true;

            public List<AStarBoardState> m_safeMoves; //TODO: check cost and score of other points, replace it the same way you do in regular astar

            public Dictionary<KeyValuePair<int, int>, Bomb> m_bombMap;

            public float? cachedStateScore = null;

            public void Kill()
            {
                m_dead = true;
            }

            public ShortenedBoardState GetShortenedState()
            {
                ShortenedBoardState newState = new ShortenedBoardState();
                newState.m_x = m_playerTile.X;
                newState.m_y = m_playerTile.Y;
                newState.m_orientation = this.m_projectedPlayerOrientation;
                newState.m_cost = m_cost;
                newState.m_stateScore = this.StateScore();
                newState.m_bombCount = 0;

                foreach (Bomb b in this.m_bombMap.Values)
                {
                    newState.m_bombCount += b.m_ticksLeft * (100 * b.m_x + 10000 * b.m_y); //TODO: this is pretty crude
                }

                newState.m_portalCount = 0;

                foreach (Portal p in this.m_portals.Values)
                {
                    newState.m_portalCount += (100 * p.m_x + 10000 * p.m_y); //TODO: this is pretty crude
                }


                return newState;
            }

            public float StateScore(bool finalTally = false)
            {
                if (cachedStateScore != null)
                {
                    return (float)cachedStateScore;
                }

                if (m_dead && finalTally)
                {
                    cachedStateScore = -100006;
                    return (float)cachedStateScore;
                }

                int targeted = 0;


                if (m_bombMap.ContainsKey(new KeyValuePair<int, int>(m_opponentTile.X, m_opponentTile.Y)))
                {
                    targeted = 100;
                }

                //TODO: add portal utility
                float portalUtility = 0;//m_portals.Count ;//m_portals.Count;

                //foreach(Portal p in m_portals)
                //{
                //    foreach(Portal p2 in m_portals)
                //    {
                //        if(p == p2)
                //        {
                //            continue;
                //        }

                //        if(p.m_owner == p2.m_owner)
                //        {
                //            portalUtility += Math.Abs(p.m_x - p2.m_x) + Math.Abs(p.m_y - p2.m_y);
                //        }
                //    }
                //}

                cachedStateScore = 600 * 5 * (m_pierce + Math.Min(m_count - 1, 0) + m_range - 3) + 500 * m_coinsAvailable + targeted + portalUtility;
                return (float)cachedStateScore;
            }

            float? calculatedScore = null;
            int cc;
            public float GetScore(float tabs = 0)
            {
                if (m_dead)
                {
                    return -1000.0f;
                }
                if (calculatedScore != null)
                {
                    return (float)calculatedScore;
                }


                if (watch.ElapsedMilliseconds > 14000)
                {
                    Console.WriteLine("Time UP");
                    return -500;
                }

                //if(m_safeMoves.Count == 0)
                //{
                //    return -100.0f;
                //}

                string t = "";

                for (int i = 0; i < tabs; i++)
                {
                    t += " ";
                }
                //Console.WriteLine(t + " m_moveToGetHere:" + m_moveToGetHere);


                float score = 0;

                score = StateScore(true);


                float scoreAdd = -9999999999;
                float scoreAve = 0;


                foreach (AStarBoardState child in m_safeMoves)
                {
                    float tr = child.GetScore(tabs + 1);
                    scoreAve += .5f * tr;
                    if (tr > scoreAdd)
                    {
                        scoreAdd = tr;
                    }

                }
                score += .99f * scoreAdd; //TODO: use scoreAve as well


                calculatedScore = score;
                return score;

            }

            public AStarBoardState GetBestMove()
            {
                AStarBoardState bestMove = null;
                foreach (AStarBoardState move in m_safeMoves)
                {

                    if (watch.ElapsedMilliseconds > 14000)
                    {
                        Console.WriteLine("Time UP");
                        return bestMove;
                    }

                    Console.WriteLine("*move.m_moveToGetHere:" + move.m_moveToGetHere + "move.GetScore():" + move.GetScore() + " cost:" + move.m_cost);

                    if (bestMove == null || bestMove.GetScore() < move.GetScore())
                    {
                        bestMove = move;
                    }
                }


                return bestMove;

            }

            public void AddSafeMove(AStarBoardState move)
            {
                m_safeMoves.Add(move);
            }

            public HashSet<Tile> GetBombedSquares(int bombX, int bombY, int ownerPiercing, int ownerRange)
            {
                HashSet<Tile> bombedTiles = new HashSet<Tile>();
                HashSet<BombSearchState> visited = new HashSet<BombSearchState>();

                Queue<BombSearchState> explosionFrontier = new Queue<BombSearchState>();


                explosionFrontier.Enqueue(new BombSearchState(ownerRange, ownerPiercing, 0, bombX, bombY));
                explosionFrontier.Enqueue(new BombSearchState(ownerRange, ownerPiercing, 1, bombX, bombY));
                explosionFrontier.Enqueue(new BombSearchState(ownerRange, ownerPiercing, 2, bombX, bombY));
                explosionFrontier.Enqueue(new BombSearchState(ownerRange, ownerPiercing, 3, bombX, bombY));

                while (explosionFrontier.Count > 0)
                {
                    BombSearchState current = explosionFrontier.Dequeue();

                    if (current == null)
                    {
                        continue;
                    }

                    if (visited.Contains(current) != false)
                    {
                        continue;
                    }



                    if (m_portals.ContainsKey(new KeyValuePair<int, int>(current.X, current.Y)))
                    {
                        BombSearchState t = m_portals[new KeyValuePair<int, int>(current.X, current.Y)].GetBombOutlet(current);
                        if (t != null)
                        {
                            explosionFrontier.Enqueue(t);
                            continue;

                        }
                    }


                    visited.Add(current);
                    bombedTiles.Add(m_worldRepresentation[current.X, current.Y]);
                    if (current.ChargesLeft == 0)
                    {
                        continue;
                    }

                    if ((m_worldRepresentation[current.X, current.Y].GetBlockType() == Tile.blockType.SoftBlock || m_worldRepresentation[current.X, current.Y].GetBlockType() == Tile.blockType.HardBlock) && !current.DestroyBlock())
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

            public AStarBoardState(Tile projectedPlayerTile, int projectedPlayerOrientation, Dictionary<KeyValuePair<int, int>, Portal> portals, Tile[,] boardState, int cost, Dictionary<KeyValuePair<int, int>, Bomb> bombMap, int range, int count, int pierce, int coinsAvailable)
            {
                m_bombMap = new Dictionary<KeyValuePair<int, int>, Bomb>();
                m_coinsAvailable = coinsAvailable;
                m_range = range;
                m_count = count;
                m_pierce = pierce;

                m_safeMoves = new List<AStarBoardState>();

                m_portals = new Dictionary<KeyValuePair<int, int>, Portal>();
                m_boardState = new Tile[m_parsed.boardSize, m_parsed.boardSize];
                //Deep copy all of the elements
                for (int x = 0; x < m_parsed.boardSize; x++)
                {
                    for (int y = 0; y < m_parsed.boardSize; y++)
                    {
                        m_boardState[x, y] = boardState[x, y];
                    }

                }


                foreach (KeyValuePair<KeyValuePair<int, int>, Bomb> b in bombMap)
                {
                    AddBombToMap(b.Value.m_x, b.Value.m_y, b.Value.m_range, b.Value.m_piercing, b.Value.m_ticksLeft);

                }


                m_projectedPlayerTile = m_boardState[projectedPlayerTile.X, projectedPlayerTile.Y];

                m_projectedPlayerOrientation = projectedPlayerOrientation;

                foreach (KeyValuePair<KeyValuePair<int, int>, Portal> p in portals)
                {
                    m_portals.Add(p.Key, p.Value.CopyConstructor());
                }

                m_cost = cost;

            }

            public bool AddBombToMap(int x, int y, int range, int piercing, int tick)
            {
                if (this.m_bombMap.ContainsKey(new KeyValuePair<int, int>(x, y)))
                {
                    return false;
                }


                m_bombMap.Add(new KeyValuePair<int, int>(x, y), new Bomb(x, y, range, piercing, tick));

                return true;
            }

            public AStarBoardState DropBomb(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 0, m_portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);

                if (state.m_count < m_bombMap.Count - enemyBombs)
                {
                    return null;
                }

                if (!state.AddBombToMap(m_projectedPlayerTile.X, m_projectedPlayerTile.Y, m_range, m_pierce, 7))
                {
                    return null;
                }

                state.m_moveToGetHere = "b";
                state.m_cameFrom = last;
                return state;

            }

            public AStarBoardState BuyPierce(AStarBoardState last)
            {
                if (m_coinsAvailable < 5)
                {
                    return null;
                }

                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 0, m_portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce + 1, m_coinsAvailable - 5);
                state.m_moveToGetHere = "buy_pierce";
                state.m_cameFrom = last;
                return state;

            }

            public AStarBoardState DoNothing(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 0, m_portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);
                state.m_moveToGetHere = "";
                state.m_cameFrom = last;
                return state;
            }

            public AStarBoardState BuyRange(AStarBoardState last)
            {
                if (m_coinsAvailable < 5)
                {
                    return null;
                }

                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 0, m_portals, m_boardState, m_cost + 2, m_bombMap, m_range + 1, m_count, m_pierce, m_coinsAvailable - 5);
                state.m_moveToGetHere = "buy_range";
                state.m_cameFrom = last;
                return state;

            }

            public AStarBoardState BuyBombs(AStarBoardState last)
            {
                if (m_coinsAvailable < 5)
                {
                    return null;
                }

                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 0, m_portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count + 1, m_pierce, m_coinsAvailable - 5);
                state.m_moveToGetHere = "buy_count";
                state.m_cameFrom = last;
                return state;

            }


            public void TickBombs()
            {
                this.m_dead = false;
                Dictionary<KeyValuePair<int, int>, Bomb> toRemove = new Dictionary<KeyValuePair<int, int>, Bomb>();
                for (int i = 0; i < m_bombMap.Count; i++)
                {
                    Bomb key = m_bombMap.Values.ElementAt(i);
                    key.m_ticksLeft -= 2;

                    if (key.m_ticksLeft < 0)
                    {
                        HashSet<Tile> bombedSquares = GetBombedSquares(key.m_x, key.m_y, key.m_piercing, key.m_range);

                        Queue<Tile> BombFrontier = new Queue<Tile>();

                        toRemove.Add(new KeyValuePair<int, int>(key.m_x, key.m_y), key);

                        foreach (Tile t in bombedSquares)
                        {
                            BombFrontier.Enqueue(t);
                        }

                        while (BombFrontier.Count > 0)
                        {
                            Tile t = BombFrontier.Dequeue();

                            if (m_bombMap.ContainsKey(new KeyValuePair<int, int>(t.X, t.Y)) && !toRemove.ContainsKey(new KeyValuePair<int, int>(t.X, t.Y)))
                            {
                                Bomb b = m_bombMap[new KeyValuePair<int, int>(t.X, t.Y)];

                                bombedSquares = GetBombedSquares(b.m_x, b.m_y, b.m_piercing, b.m_range);
                                toRemove.Add(new KeyValuePair<int, int>(t.X, t.Y), b);
                                foreach (Tile tile in bombedSquares)
                                {
                                    BombFrontier.Enqueue(tile);
                                }

                            }
                            if (t.X == m_projectedPlayerTile.X && t.Y == m_projectedPlayerTile.Y)
                            {
                                this.Kill();
                            }
                            if (t.GetBlockType() == Tile.blockType.SoftBlock)
                            {
                                m_worldRepresentation[t.X, t.Y] = new Tile(t.X, t.Y, Tile.blockType.Passable, 999);
                                m_coinsAvailable += (int)Math.Floor((double)(m_parsed.boardSize - 1 - t.X) * t.X * (m_parsed.boardSize - 1 - t.Y) * t.Y * 10 / ((m_parsed.boardSize - 1) ^ 4 / 16));
                            }



                        }
                    }
                }

                foreach (KeyValuePair<int, int> b in toRemove.Keys)
                {
                    m_bombMap.Remove(b);
                }

            }

            public AStarBoardState MoveLeft(AStarBoardState last)
            {

                if (last.m_bombMap.ContainsKey(new KeyValuePair<int, int>(m_projectedPlayerTile.X + 1, m_projectedPlayerTile.Y)))
                {
                    return null;
                }
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X + 1, m_projectedPlayerTile.Y], 0, m_portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);
                //foreach (Bomb t in last.m_bombMap)
                //{
                //    if (t.m_x == state.m_projectedPlayerTile.X && t.m_y == state.m_projectedPlayerTile.Y)
                //    {
                //        state.m_projectedPlayerTile = state.m_boardState[last.m_projectedPlayerTile.X, last.m_projectedPlayerTile.Y];
                //    }
                //}

                state.m_moveToGetHere = "mr";
                state.m_cameFrom = last;
                return state;
            }


            public AStarBoardState MoveRight(AStarBoardState last)
            {

                if (last.m_bombMap.ContainsKey(new KeyValuePair<int, int>(m_projectedPlayerTile.X - 1, m_projectedPlayerTile.Y)))
                {
                    return null;
                }
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X - 1, m_projectedPlayerTile.Y], 2, m_portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);

                state.m_moveToGetHere = "ml";
                state.m_cameFrom = last;
                return state;

            }


            public AStarBoardState MoveUp(AStarBoardState last)
            {

                if (last.m_bombMap.ContainsKey(new KeyValuePair<int, int>(m_projectedPlayerTile.X, m_projectedPlayerTile.Y - 1)))
                {
                    return null;
                }
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y - 1], 1, m_portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);

                state.m_moveToGetHere = "mu";
                state.m_cameFrom = last;
                return state;
            }


            public AStarBoardState MoveDown(AStarBoardState last)
            {

                if (last.m_bombMap.ContainsKey(new KeyValuePair<int, int>(m_projectedPlayerTile.X, m_projectedPlayerTile.Y + 1)))
                {
                    return null;
                }
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y + 1], 3, m_portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);

                state.m_moveToGetHere = "md";
                state.m_cameFrom = last;
                return state;

            }



            public AStarBoardState TurnLeft(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 0, m_portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);

                state.m_moveToGetHere = "tl";
                state.m_cameFrom = last;
                return state;

            }


            public AStarBoardState TurnRight(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 2, m_portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);
                state.m_moveToGetHere = "tr";
                state.m_cameFrom = last;
                return state;

            }


            public AStarBoardState TurnUp(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 1, m_portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);
                state.m_moveToGetHere = "tu";
                state.m_cameFrom = last;
                return state;

            }


            public AStarBoardState TurnDown(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 3, m_portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);
                state.m_moveToGetHere = "td";
                state.m_cameFrom = last;
                return state;

            }



            public AStarBoardState ShootBluePortal(AStarBoardState last)
            {
                AStarBoardState state = ShoootPortal(false);
                if (state == null)
                {
                    return null;
                }

                state.m_moveToGetHere = "bp";
                state.m_cameFrom = last;
                return state;

            }

            public AStarBoardState ShoootPortal(bool isOrange)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], m_projectedPlayerOrientation, m_portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);


                Tile tileIt = state.m_projectedPlayerTile;

                while (!(tileIt.GetBlockType() == Tile.blockType.HardBlock || tileIt.GetBlockType() == Tile.blockType.SoftBlock))
                {
                    if (state.m_projectedPlayerOrientation == 0)
                    {
                        if (tileIt.X - 1 < 0)
                        {
                            ////////Console.WriteLine("Out of range");
                            continue;

                        }
                        tileIt = state.m_boardState[tileIt.X - 1, tileIt.Y];
                    }
                    else if (state.m_projectedPlayerOrientation == 1)
                    {
                        if (tileIt.Y - 1 < 0)
                        {
                            ////////Console.WriteLine("Out of range");
                            continue;

                        }
                        tileIt = state.m_boardState[tileIt.X, tileIt.Y - 1];

                    }
                    else if (state.m_projectedPlayerOrientation == 2)
                    {
                        if (tileIt.X + 1 > m_parsed.boardSize)
                        {
                            ////////Console.WriteLine("Out of range");
                            continue;

                        }
                        tileIt = state.m_boardState[tileIt.X - 1, tileIt.Y];
                    }
                    else if (state.m_projectedPlayerOrientation == 3)
                    {
                        if (tileIt.Y + 1 < 0)
                        {
                            ////////Console.WriteLine("Out of range");
                            continue;

                        }
                        tileIt = state.m_boardState[tileIt.X, tileIt.Y + 1];

                    }
                }
                int newOrientation = 0;

                if (state.m_projectedPlayerOrientation == 0)
                {
                    newOrientation = 2;
                }
                else if (state.m_projectedPlayerOrientation == 1)
                {
                    newOrientation = 3;
                }
                else if (state.m_projectedPlayerOrientation == 2)
                {
                    newOrientation = 0;
                }
                else if (state.m_projectedPlayerOrientation == 3)
                {
                    newOrientation = 1;
                }

                List<KeyValuePair<int, int>> toRemove = new List<KeyValuePair<int, int>>();

                foreach (KeyValuePair<int, int> pKey in state.m_portals.Keys)
                {
                    Portal p = state.m_portals[pKey];

                    if (p.m_isOrange == isOrange && p.m_owner == m_parsed.playerIndex)
                    {
                        if (p.m_orientation == newOrientation)
                        {
                            return null;
                        }
                        toRemove.Add(pKey);
                    }

                    if ((p.m_x == tileIt.X && p.m_y == tileIt.Y && p.m_orientation == newOrientation))
                    {
                        toRemove.Add(pKey);
                    }
                }
                foreach (KeyValuePair<int, int> r in toRemove)
                {
                    state.m_portals.Remove(r);
                }

                if (m_portals.ContainsKey(new KeyValuePair<int, int>(tileIt.X, tileIt.Y)))
                {
                    return null;
                }

                state.m_portals.Add(new KeyValuePair<int, int>(tileIt.X, tileIt.Y), new Portal(tileIt.X, tileIt.Y, newOrientation, m_parsed.playerIndex, isOrange));
                List<Portal> playerPortals = new List<Portal>();
                List<Portal> opponentPortals = new List<Portal>();
                foreach (Portal p in state.m_portals.Values)
                {
                    if (p.m_owner == m_parsed.playerIndex)
                    {
                        playerPortals.Add(p);
                    }
                    else
                    {
                        opponentPortals.Add(p);
                    }
                }

                if (opponentPortals.Count == 2)
                {
                    Portal.LinkPortals(opponentPortals[0], opponentPortals[1]);
                }


                if (playerPortals.Count == 2)
                {
                    Portal.LinkPortals(playerPortals[0], playerPortals[1]);
                }

                return state;
            }

            public AStarBoardState ShootOrangePortal(AStarBoardState last)
            {
                AStarBoardState state = ShoootPortal(true);
                if (state == null)
                {
                    return null;
                }

                state.m_moveToGetHere = "op";
                state.m_cameFrom = last;
                return state;

            }

        }

        public class Bomb
        {
            public int m_x;
            public int m_y;
            public int m_range;
            public int m_piercing;
            public int m_ticksLeft;

            public Bomb(int x, int y, int range, int piercing, int ticksLeft)
            {
                m_x = x;
                m_y = y;
                m_range = range;
                m_piercing = piercing;
                m_ticksLeft = ticksLeft;
            }
        }

        public class Portal
        {
            private Portal m_linkedPortal;
            public int m_x;
            public int m_y;
            public int m_orientation;
            public int m_owner;
            public bool m_isOrange;
            public BombSearchState GetBombOutlet(BombSearchState inlet)
            {
                if (m_x != inlet.X || m_y != inlet.Y)
                {
                    return null;
                }

                if (!((inlet.Orientation == 0 && this.m_orientation == 2) /*inlet is left and this is right*/
                    || (inlet.Orientation == 2 && this.m_orientation == 0 /*inlet is right and this is left*/
                    || (inlet.Orientation == 1 && this.m_orientation == 3) /*inlet is up and this is down*/
                    || (inlet.Orientation == 3 && this.m_orientation == 1) /*inlet is down and this is up*/

                    )))
                {
                    return null;
                }

                if (m_linkedPortal == null)
                {
                    return null;
                }

                int newOrientation = 0;

                if (m_linkedPortal.m_orientation == 0)
                {
                    newOrientation = 0;
                }


                if (m_linkedPortal.m_orientation == 2)
                {
                    newOrientation = 2;
                }


                if (m_linkedPortal.m_orientation == 1)
                {
                    newOrientation = 1;
                }


                if (m_linkedPortal.m_orientation == 3)
                {
                    newOrientation = 3;
                }


                //flip the orientation
                return new BombSearchState(inlet.ChargesLeft + 1, inlet.PiercesLeft, newOrientation, m_linkedPortal.m_x, m_linkedPortal.m_y);

            }


            public Tile OutletAStarTile()
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

                Tile ret = m_linkedPortal.OutletAStarTile();
                AStarBoardState retu = new AStarBoardState(inlet.m_boardState[ret.X, ret.Y], 0, inlet.m_portals, inlet.m_boardState, inlet.m_cost + 2, inlet.m_bombMap, inlet.m_range, inlet.m_count, inlet.m_pierce, inlet.m_coinsAvailable);

                if (this.m_orientation == 2)
                {
                    retu.m_moveToGetHere = "ml";

                }
                else if (this.m_orientation == 0)
                {
                    retu.m_moveToGetHere = "mr";
                }
                else if (this.m_orientation == 3)
                {
                    retu.m_moveToGetHere = "mu";
                }
                else
                {
                    retu.m_moveToGetHere = "md";
                }

                retu.m_cost = inlet.m_cost + 2;
                retu.m_cameFrom = inlet;



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
        static Tile m_playerTile;
        static Tile m_opponentTile;
        static Tile[,] m_worldRepresentation;
        static bool locked = false;

        static ServerResponse m_parsed;
        static Random m_random;

        static void Main(string[] args)
        {
            //// Attempt to open output file.
            //StreamWriter writer = new StreamWriter("out.txt");
            //// Redirect standard output from the console to the output file.
            //Console.SetOut(writer);
            //// Redirect standard input from the console to the input file.

            m_random = new Random(0);
            Thread player = new Thread(PlayPlayerThread);
            //Thread opponent = new Thread(PlayOpponentThread);

            player.Start();
            //opponent.Start();


        }
        static int HeuristicCalculation(Tile from, Tile to)
        {
            return Math.Abs(to.X - from.X) + Math.Abs(to.Y - from.Y);
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
            var request = (HttpWebRequest)WebRequest.Create("http://aicomp.io/api/games/practice");//(HttpWebRequest)WebRequest.Create("http://aicomp.io/api/games/search");

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
                while (locked)
                {
                    ////Console.WriteLine("Waiting for control");
                    Thread.Sleep(500);
                }
                locked = true;

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string chosenAction = "";
                    var responseString = reader.ReadToEnd();
                    watch = System.Diagnostics.Stopwatch.StartNew();
                    ////////////Console.Write(responseString);
                    if (responseString == "\"Game ID is undefined, maybe the game ended or does not exist!\"")
                    {
                        return false;
                    }
                    m_parsed = JsonConvert.DeserializeObject<ServerResponse>(responseString);
                    ////////Console.Write("Data has been parsed\n" + postData);

                    m_worldRepresentation = new Tile[m_parsed.boardSize, m_parsed.boardSize];

                    //make the portals
                    Dictionary<KeyValuePair<int, int>, Portal> portals = new Dictionary<KeyValuePair<int, int>, Portal>();
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

                            if (string_color == "orange")
                            {
                                bool_color = true;
                            }
                            else if (string_color == "blue")
                            {
                                bool_color = false;
                            }
                            else
                            {
                                //////Console.WriteLine("Color error");
                            }

                            Portal portal = new Portal(coords[0], coords[1], Convert.ToInt32(kvp.Key), int_owner, bool_color);

                            portals.Add(new KeyValuePair<int, int>(coords[0], coords[1]), portal);

                            if (int_owner == 0)
                            {
                                playerOnePortals.Add(portal);
                            }
                            else
                            {
                                playerTwoPortals.Add(portal);

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

                    for (int x = 0; x < m_parsed.boardSize; x++)
                    {
                        for (int y = 0; y < m_parsed.boardSize; y++)
                        {
                            m_worldRepresentation[x, y] = new Tile(x, y, m_parsed);
                        }
                    }

                    int playerBombs = 0;

                    foreach (KeyValuePair<string, Dictionary<string, int>> bomb in m_parsed.bombMap)
                    {
                        int bombX = Int32.Parse(bomb.Key.Split(new Char[] { ',' })[0]);
                        int bombY = Int32.Parse(bomb.Key.Split(new Char[] { ',' })[1]);
                        int owner;
                        bomb.Value.TryGetValue("owner", out owner);

                        if (owner == m_parsed.playerIndex)
                        {
                            playerBombs++;
                        }
                        else
                        {
                            enemyBombs++;
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
                        Console.ReadLine();
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
                        Console.ReadLine();
                        return true;
                    }

                    m_opponentTile = m_worldRepresentation[int_ox, int_oy];

                    HashSet<Tile> closedSet = new HashSet<Tile>(); //the already evaluated set of nodes


                    Tile endingTile = null;

                    if (m_parsed.bombMap.Count == 0)
                    {
                        endingTile = m_opponentTile;
                    }


                    Queue<AStarBoardState> nextTiles = new Queue<AStarBoardState>();



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


                    int int_opponent_bombRange;
                    int int_opponent_bombCount;
                    int int_opponent_bombPierce;

                    m_parsed.opponent.TryGetValue("bombRange", out object_bombRange);
                    m_parsed.opponent.TryGetValue("bombCount", out object_bombCount);
                    m_parsed.opponent.TryGetValue("bombPierce", out object_bombPierce);

                    int_opponent_bombRange = Convert.ToInt32(object_bombRange);
                    int_opponent_bombCount = Convert.ToInt32(object_bombCount);
                    int_opponent_bombPierce = Convert.ToInt32(object_bombPierce);


                    int m_maxBombCount = 2;
                    int m_maxBombPierce = 8;
                    int m_maxBombRange = 8;


                    int mostNeededCost = 0;

                    m_parsed.player.TryGetValue("coins", out object_coins);
                    int_coins = Convert.ToInt32(object_coins);

                    Dictionary<KeyValuePair<int, int>, Bomb> newBombMap = new Dictionary<KeyValuePair<int, int>, Bomb>();
                    foreach (string k in m_parsed.bombMap.Keys)
                    {
                        int v = m_parsed.bombMap[k]["tick"] + 2;
                        string[] s = k.Split(',');

                        int owner = m_parsed.bombMap[k]["owner"];

                        if (owner == m_parsed.playerIndex)
                        {
                            newBombMap.Add(new KeyValuePair<int, int>(Convert.ToInt32(s[0]), Convert.ToInt32(s[1])), new Bomb(Convert.ToInt32(s[0]), Convert.ToInt32(s[1]), int_bombRange, int_bombPierce, v));

                        }
                        else
                        {

                            newBombMap.Add(new KeyValuePair<int, int>(Convert.ToInt32(s[0]), Convert.ToInt32(s[1])), new Bomb(Convert.ToInt32(s[0]), Convert.ToInt32(s[1]), int_opponent_bombRange, int_opponent_bombPierce, v));
                        }


                    }



                    AStarBoardState firstMove = new AStarBoardState(m_playerTile, int_orientation, portals, m_worldRepresentation, 1, newBombMap, int_bombRange, int_bombCount, int_bombPierce, int_coins);

                    HashSet<KeyValuePair<int, int>> m_trails = new HashSet<KeyValuePair<int, int>>();

                    if (m_parsed.trailmap.Count > 0)
                    {
                        foreach (KeyValuePair<string, object> kvp in m_parsed.trailmap)
                        {
                            int[] bombCoords = kvp.Key.Split(',').Select(num => int.Parse(num)).ToArray();
                            m_trails.Add(new KeyValuePair<int, int>(bombCoords[0], bombCoords[1]));
                        }
                    }

                    nextTiles.Enqueue(firstMove);


                    Dictionary<ShortenedBoardState, AStarBoardState> visited = new Dictionary<ShortenedBoardState, AStarBoardState>();



                    AStarBoardState leadingState = firstMove;
                    //BFS search to find all safe tiles
                    int turnsWithoutProgress = 0;

                    float turnTime = 13000;
                    while (nextTiles.Count > 0 && watch.ElapsedMilliseconds < turnTime)
                    {
                        AStarBoardState current = nextTiles.Dequeue();


                        if (current == null)
                        {
                            continue;
                        }

                        if (current.m_projectedPlayerTile.GetBlockType() != Tile.blockType.Passable || (current.m_projectedPlayerTile.X == m_opponentTile.X && current.m_projectedPlayerTile.Y == m_opponentTile.Y))
                        {
                            continue;
                        }
                        if ((current.m_cost <= 3 && m_trails.Contains(new KeyValuePair<int, int>(current.m_projectedPlayerTile.X, current.m_projectedPlayerTile.Y))))
                        {
                            continue;
                        }

                        if (current.m_cost > 1000)
                        {
                            continue;
                        }

                        if (current.StateScore() > leadingState.StateScore())
                        {
                            leadingState = current;
                        }

                        if (turnsWithoutProgress > 10)
                        {
                            turnsWithoutProgress = 0;
                            leadingState = current;
                        }

                        if ((current.StateScore() + 200 < leadingState.StateScore() * .8f && current.m_cost > 10 + leadingState.m_cost))
                        {
                            nextTiles.Enqueue(current);
                            turnsWithoutProgress++;
                            continue;
                        }


                        ShortenedBoardState shortState = current.GetShortenedState();

                        if (!visited.ContainsKey(shortState))
                        {
                            visited.Add(shortState, current);
                        }
                        else
                        {
                            continue;
                        }


                        if (current.m_cameFrom != null)
                        {
                            current.m_cameFrom.AddSafeMove(current);
                        }

                        current.TickBombs();

                        if (current.m_dead == true)
                        {
                            continue;
                        }

                        KeyValuePair<int, int> k = new KeyValuePair<int, int>(current.m_projectedPlayerTile.X, current.m_projectedPlayerTile.Y);
                        if (current.m_portals.ContainsKey(k))
                        {
                            AStarBoardState neighbor = current.m_portals[new KeyValuePair<int, int>(current.m_projectedPlayerTile.X, current.m_projectedPlayerTile.Y)].GetTileOutlet(current);
                        }

                        //foreach (Portal p in portals)
                        //{
                        //    AStarBoardState neighbor = p.GetTileOutlet(current);
                        //    if (neighbor != null)
                        //    {
                        //        nextTiles.Enqueue(neighbor);
                        //    }
                        //}
                        nextTiles.Enqueue(current.DropBomb(current));
                        nextTiles.Enqueue(current.MoveLeft(current));
                        nextTiles.Enqueue(current.MoveUp(current));
                        nextTiles.Enqueue(current.MoveDown(current));
                        nextTiles.Enqueue(current.MoveRight(current));


                        nextTiles.Enqueue(current.ShootBluePortal(current));
                        nextTiles.Enqueue(current.ShootOrangePortal(current));


                        nextTiles.Enqueue(current.DoNothing(current));
                        if (current.m_coinsAvailable >= 5)
                        {

                            int choice = m_random.Next(0, 3);
                            if (choice == 0)
                            {
                                nextTiles.Enqueue(current.BuyPierce(current));
                            }
                            else if (choice == 1)
                            {
                                nextTiles.Enqueue(current.BuyBombs(current));
                            }
                            else if (choice == 2)
                            {
                                nextTiles.Enqueue(current.BuyRange(current));
                            }
                        }
                    }




                    int cost;

                    AStarBoardState bestMove = firstMove.GetBestMove();

                    if (bestMove != null)
                    {
                        chosenAction = bestMove.m_moveToGetHere;
                    }
                    else
                    {
                        chosenAction = "";
                    }
                    Console.WriteLine("Time taken: " + watch.ElapsedMilliseconds);
                    Console.WriteLine("ChosenAction:" + chosenAction);
                    Console.WriteLine("Current Tile: " + firstMove.m_projectedPlayerTile.X + " " + firstMove.m_projectedPlayerTile.Y + " " + firstMove.m_projectedPlayerTile.GetBlockType());
                    Console.WriteLine("Next Tile: " + bestMove.m_projectedPlayerTile.X + " " + bestMove.m_projectedPlayerTile.Y + " " + bestMove.m_projectedPlayerTile.GetBlockType());
                    request = (HttpWebRequest)WebRequest.Create("http://aicomp.io/api/games/submit/" + m_parsed.gameID);
                    postData = "{\"devkey\": \"" + key + "\", \"playerID\": \"" + m_parsed.playerID + "\", \"move\": \"" + chosenAction/*m_actions[m_random.Next(0, m_actions.Length)]*/ + "\" }";
                    data = Encoding.ASCII.GetBytes(postData);
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    request.ContentLength = data.Length;

                    // the code that you want to measure comes here
                    watch.Stop();

                    ////Console.WriteLine("Time:" + watch.ElapsedMilliseconds);


                    Console.Write("\nAction is:" + chosenAction + "\n");
                    Console.WriteLine();
                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                    locked = false;
                    Thread.Sleep(500);
                    response = (HttpWebResponse)request.GetResponse();
                    Thread.Sleep(500);
                }
                //////////Console.Write("Start Iteration");
            } while (gameNotCompleted);
            Console.ReadLine();
            return false;

        }

    }
}