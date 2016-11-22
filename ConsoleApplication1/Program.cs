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

    public struct ShortenedBoardState
    {
        public int m_x;
        public int m_y;
        public int m_orientation;
        public float m_stateScore;
        public Dictionary<KeyValuePair<int, int>, int> m_bombMap;
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

        public Tile CopyConstructor()
        {
            Tile newTile = this; //TODO: refactor

            return newTile;
        }
    }



    class Program
    {



        public class AStarBoardState
        {
            public Tile m_projectedPlayerTile;
            public int m_projectedPlayerOrientation;

            public int m_cost;
            public int m_coinsAvailable;
            public int m_coinsEarned;

            public string m_moveToGetHere;


            public List<Portal> m_portals;
            public Tile[,] m_boardState;
            public AStarBoardState m_cameFrom;

            public int m_range;
            public int m_count;
            public int m_pierce;

            public List<AStarBoardState> m_safeMoves;

            public Dictionary<KeyValuePair<int, int>, int> m_bombMap;

            public float? cachedStateScore = null;

            public ShortenedBoardState GetShortenedState()
            {
                ShortenedBoardState newState = new ShortenedBoardState();
                newState.m_x = m_playerTile.X;
                newState.m_y = m_playerTile.Y;
                newState.m_orientation = this.m_projectedPlayerOrientation;
                newState.m_stateScore = this.StateScore();
                newState.m_bombMap = this.m_bombMap;
                return newState;
            }

            public float StateScore(bool finalTally = false)
            {
                if (finalTally)
                {
                    if (m_safeMoves.Count == 0)
                    {
                        return -100;
                    }
                }

                if (cachedStateScore != null)
                {
                    return (float)cachedStateScore;
                }


                //float bombableTiles = 0;

                //if (!m_bombMap.ContainsKey(new KeyValuePair<int, int>(m_projectedPlayerTile.X, m_projectedPlayerTile.Y)) && m_bombMap.Count == 0 /*TODO: remove this hack*/)
                //{
                //    bombableTiles = GetBombedSquares(m_projectedPlayerTile.X, m_projectedPlayerTile.Y, m_pierce, m_range).Where(item => item.GetBlockType() == Tile.blockType.SoftBlock).ToList().Count;

                //}

                float danger = 0;
                foreach (KeyValuePair<KeyValuePair<int, int>, int> kvp in m_bombMap)
                {
                    List<Tile> bombedTiles = GetBombedSquares(kvp.Key.Key, kvp.Key.Value, m_pierce, m_range);
                    foreach (Tile t in bombedTiles)
                    {
                        if (t.X == m_projectedPlayerTile.X && t.Y == m_projectedPlayerTile.Y)
                        {
                            danger = -100;
                        }

                    }
                }



                //foreach (KeyValuePair<KeyValuePair<int, int>, int> kvp in m_bombMap)
                //{
                //    bombableTiles += GetBombedSquares(kvp.Key.Key, kvp.Key.Value, m_pierce, m_range).Where(item => item.GetBlockType() == Tile.blockType.SoftBlock).ToList().Count * 4.0f;
                ////}
                //float portalProximity = 100;

                ////TODO: include distance to portals.
                //foreach (Portal p in m_portals)
                //{
                //    portalProximity -= Math.Abs(p.m_x - m_playerTile.X) + Math.Abs(p.m_y - m_playerTile.Y);
                //}


                cachedStateScore = 600000 * (m_pierce + m_count + m_range - 3) + 500000 * m_coinsAvailable/* + bombableTiles * 101 */+ danger + m_bombMap.Count * 5.0f - m_cost / 100.0f /*+ portalProximity*m_portals.Count*/;
                return (float)cachedStateScore;
            }

            float? calculatedScore = null;
            int cc;
            public float GetScore(float tabs = 0)
            {//TODO: cache sco                    
                if (calculatedScore != null)
                {
                    return (float)calculatedScore;
                }

                string t = "";

                for (int i = 0; i < tabs; i++)
                {
                    t += " ";
                }
                if ( this.Safe() == false)
                {
                    //Console.WriteLine(t + " m_moveToGetHere:" + m_moveToGetHere + " is unsafe");
                    return -1000;
                }

                float score = 0;

                score = StateScore(true);


                float scoreAdd = 0;
                float scoreAve = 0;
                //Console.WriteLine(t + "StateScore():" + score + " child.m_moveToGetHere:" + this.m_moveToGetHere + " bombs:" + this.m_bombMap.Count + " cost:" + this.m_cost + " isSafe:" + this.Safe());

                foreach (AStarBoardState child in m_safeMoves)
                {
                    float tr = child.GetScore(tabs + 1);
                    scoreAve += .5f * tr;
                    if (tr > scoreAdd)
                    {
                        scoreAdd = tr;
                    }

                }
                score += .99f * scoreAdd / m_safeMoves.Count;


                calculatedScore = score;

                return score;

            }

            public AStarBoardState GetBestMove()
            {
                //TODO: bake victory into best move selection
                AStarBoardState bestMove = null;
                foreach (AStarBoardState move in m_safeMoves)
                {
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
                ////Console.WriteLine(m_safeMoves.Count);
            }

            public bool Safe() //TODO: account for transitive exploding
            {
                int tick = 999;
                bool debug_flag = false;
                foreach (KeyValuePair<int, int> key in m_bombMap.Keys)
                {
                    if (m_bombMap[key] < tick)
                    {
                        //if(m_projectedPlayerTile.X == 2 && m_projectedPlayerTile.Y == 1 && (key.Key == 1 && key.Value == 2))
                        //{
                        //    Console.WriteLine("break");
                        //    debug_flag = true;
                        //}

                        List<Tile> bombedTile = GetBombedSquares(key.Key, key.Value, m_pierce, m_range);//TODO: account for owner range not just player range
                        foreach (Tile t in bombedTile)
                        {
                            if (t.X == m_projectedPlayerTile.X && t.Y == m_projectedPlayerTile.Y)
                            {
                                tick = m_bombMap[key];
                                //Console.WriteLine("Danger found: " + key.Key + " " + key.Value + " " + tick);
                            }
                        }
                    }
                }

                //if (debug_flag)
                //{
                //    Console.WriteLine(tick);
                //}

                if (tick > 0)
                {

                    return true;
                }
                else
                {
                    return false;
                }
            }


            public List<Tile> GetBombedSquares(int bombX, int bombY, int ownerPiercing, int ownerRange)
            {
                List<Tile> bombedTiles = new List<Tile>();

                Queue<BombSearchState> explosionFrontier = new Queue<BombSearchState>();

                //TODO: get the stats from the state instead of from the parsed
                int int_bombRange = ownerRange;

                object object_bombPiercing;

                int int_bombPiercing = ownerPiercing;

                explosionFrontier.Enqueue(new BombSearchState(int_bombRange, int_bombPiercing, 0, bombX, bombY));
                explosionFrontier.Enqueue(new BombSearchState(int_bombRange, int_bombPiercing, 1, bombX, bombY));
                explosionFrontier.Enqueue(new BombSearchState(int_bombRange, int_bombPiercing, 2, bombX, bombY));
                explosionFrontier.Enqueue(new BombSearchState(int_bombRange, int_bombPiercing, 3, bombX, bombY));

                while (explosionFrontier.Count > 0)
                {
                    BombSearchState current = explosionFrontier.Dequeue();

                    //////////Console.Write("\n current explosion frontier tile is" + current.X + " " + current.Y);

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
                            //Console.Write("\n bomb projection has entered a portal " + current.X + " " + current.Y + " " + portalState.X + " " + portalState.Y + " left:" + portalState.ChargesLeft);
                            explosionFrontier.Enqueue(portalState);
                        }
                    }


                    if (shouldContinue)
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

            public AStarBoardState(Tile projectedPlayerTile, int projectedPlayerOrientation, List<Portal> portals, Tile[,] boardState, int cost, Dictionary<KeyValuePair<int, int>, int> bombMap, int range, int count, int pierce, int coinsAvailable)
            {
                m_bombMap = new Dictionary<KeyValuePair<int, int>, int>();
                m_coinsAvailable = coinsAvailable;
                m_range = range;
                m_count = count;
                m_pierce = pierce;

                m_safeMoves = new List<AStarBoardState>();

                m_portals = new List<Portal>();
                m_boardState = new Tile[m_parsed.boardSize, m_parsed.boardSize];
                //Deep copy all of the elements
                for (int x = 0; x < m_parsed.boardSize; x++)
                {
                    for (int y = 0; y < m_parsed.boardSize; y++)
                    {
                        m_boardState[x, y] = boardState[x, y].CopyConstructor();//TODO: this boardState[x, y].CopyConstructor();
                    }

                }


                foreach (KeyValuePair<int, int> key in bombMap.Keys)
                {
                    AddBombToMap(key.Key, key.Value, bombMap[key] + 2);

                }


                m_projectedPlayerTile = m_boardState[projectedPlayerTile.X, projectedPlayerTile.Y];

                m_projectedPlayerOrientation = projectedPlayerOrientation;

                foreach (Portal p in portals)
                {
                    m_portals.Add(p.CopyConstructor());
                }

                m_cost = cost;

            }

            public bool AddBombToMap(int x, int y, int tick) //TODO: include transitive bombing
            {
                if (m_bombMap.ContainsKey(new KeyValuePair<int, int>(x, y)))
                {
                    return false;
                }

                //TODO: grab piercing and range from player state in this tile
                m_bombMap.Add(new KeyValuePair<int, int>(x, y), tick);

                return true;
            }

            public AStarBoardState DropBomb(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 0, portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);



                if (state.m_bombMap.ContainsKey(new KeyValuePair<int, int>(m_projectedPlayerTile.X, m_projectedPlayerTile.Y)))
                {
                    return null;
                }

                if (!state.AddBombToMap(m_projectedPlayerTile.X, m_projectedPlayerTile.Y, 9))
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

                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 0, portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce + 1, m_coinsAvailable - 5);
                state.m_moveToGetHere = "buy_pierce";
                state.m_cameFrom = last;
                return state;

            }

            public AStarBoardState DoNothing(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 0, portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);
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

                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 0, portals, m_boardState, m_cost + 2, m_bombMap, m_range + 1, m_count, m_pierce, m_coinsAvailable - 5);
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

                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 0, portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count + 1, m_pierce, m_coinsAvailable - 5);
                state.m_moveToGetHere = "buy_count";
                state.m_cameFrom = last;
                return state;

            }


            public void TickBombs()
            {
                List<KeyValuePair<int, int>> toRemove = new List<KeyValuePair<int, int>>();
                for (int i = 0; i < m_bombMap.Keys.Count; i++)
                {
                    KeyValuePair<int, int> key = m_bombMap.Keys.ElementAt(i);
                    m_bombMap[key] = m_bombMap[key] - 2;

                    if (m_bombMap[key] < -1 && m_bombMap[key] > -6) //TODO: not accounting for trail
                    {
                        List<Tile> bombedSquares = GetBombedSquares(key.Key, key.Value, m_pierce, m_range);

                        foreach (Tile t in bombedSquares)
                        {
                            if (t.GetBlockType() == Tile.blockType.SoftBlock)
                            {
                                m_worldRepresentation[t.X, t.Y] = new Tile(t.X, t.Y, Tile.blockType.Passable, 999);
                                m_coinsAvailable += (int)Math.Floor((double)(m_parsed.boardSize - 1 - t.X) * t.X * (m_parsed.boardSize - 1 - t.Y) * t.Y * 10 / ((m_parsed.boardSize - 1) ^ 4 / 16));
                            }
                        }
                    }
                    if (m_bombMap[key] <= -3)
                    {
                        toRemove.Add(key);
                    }
                }

                foreach (KeyValuePair<int, int> kvp in toRemove)
                {
                    m_bombMap.Remove(kvp);
                }

            }

            //TODO: this is all wiggity wack you
            public AStarBoardState MoveLeft(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X + 1, m_projectedPlayerTile.Y], 0, portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);
                state.m_moveToGetHere = "mr";
                state.m_cameFrom = last;
                return state;
            }


            public AStarBoardState MoveRight(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X - 1, m_projectedPlayerTile.Y], 2, portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);
                state.m_moveToGetHere = "ml";
                state.m_cameFrom = last;
                return state;

            }


            public AStarBoardState MoveUp(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y - 1], 1, portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);
                state.m_moveToGetHere = "mu";
                state.m_cameFrom = last;
                return state;
            }


            public AStarBoardState MoveDown(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y + 1], 3, portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);
                state.m_moveToGetHere = "md";
                state.m_cameFrom = last;
                return state;

            }



            public AStarBoardState TurnLeft(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 0, portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);
                state.m_moveToGetHere = "tl";
                state.m_cameFrom = last;
                return state;

            }


            public AStarBoardState TurnRight(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 2, portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);
                state.m_moveToGetHere = "tr";
                state.m_cameFrom = last;
                return state;

            }


            public AStarBoardState TurnUp(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 1, portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);
                state.m_moveToGetHere = "tu";
                state.m_cameFrom = last;
                return state;

            }


            public AStarBoardState TurnDown(AStarBoardState last)
            {
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], 3, portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);
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
                AStarBoardState state = new AStarBoardState(m_boardState[m_projectedPlayerTile.X, m_projectedPlayerTile.Y], m_projectedPlayerOrientation, portals, m_boardState, m_cost + 2, m_bombMap, m_range, m_count, m_pierce, m_coinsAvailable);


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
                Portal toRemove = null;
                foreach (Portal p in state.m_portals)
                {
                    if (p.m_isOrange == isOrange && p.m_owner == m_parsed.playerIndex)
                    {
                        if (p.m_orientation == newOrientation)
                        {
                            return null;
                        }
                        toRemove = p;
                    }

                    if ((p.m_x == tileIt.X && p.m_y == tileIt.Y && p.m_orientation == newOrientation))
                    {
                        toRemove = p;
                    }
                }

                if (toRemove != null)
                {
                    state.m_portals.Remove(toRemove);
                }


                state.m_portals.Add(new Portal(tileIt.X, tileIt.Y, newOrientation, m_parsed.playerIndex, isOrange));
                List<Portal> playerPortals = new List<Portal>();
                List<Portal> opponentPortals = new List<Portal>();
                foreach (Portal p in state.m_portals)
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
        static List<Portal> portals;
        static bool locked = false;

        static ServerResponse m_parsed;

        static void Main(string[] args)
        {
            //// Attempt to open output file.
            //StreamWriter writer = new StreamWriter("out.txt");
            //// Redirect standard output from the console to the output file.
            //Console.SetOut(writer);
            //// Redirect standard input from the console to the input file.

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
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    ////////////Console.Write(responseString);
                    if (responseString == "\"Game ID is undefined, maybe the game ended or does not exist!\"")
                    {
                        return false;
                    }
                    m_parsed = JsonConvert.DeserializeObject<ServerResponse>(responseString);
                    ////////Console.Write("Data has been parsed\n" + postData);

                    m_worldRepresentation = new Tile[m_parsed.boardSize, m_parsed.boardSize];

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

                            if (int_owner == 0)
                            {
                                playerOnePortals.Add(new Portal(coords[0], coords[1], Convert.ToInt32(kvp.Key), int_owner, bool_color));
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
                            m_worldRepresentation[x, y] = new Tile(x, y, m_parsed);
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

                    int m_maxBombCount = 2;
                    int m_maxBombPierce = 8;
                    int m_maxBombRange = 8;


                    int mostNeededCost = 0;

                    m_parsed.player.TryGetValue("coins", out object_coins);
                    int_coins = Convert.ToInt32(object_coins);

                    Dictionary<KeyValuePair<int, int>, int> newBombMap = new Dictionary<KeyValuePair<int, int>, int>();
                    foreach (string k in m_parsed.bombMap.Keys)
                    {
                        int v = m_parsed.bombMap[k]["tick"];
                        string[] s = k.Split(',');

                        newBombMap.Add(new KeyValuePair<int, int>(Convert.ToInt32(s[0]), Convert.ToInt32(s[1])), v);

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


                    Dictionary<ShortenedBoardState, int> visited = new Dictionary<ShortenedBoardState, int>();



                    AStarBoardState leadingState = firstMove;
                    //BFS search to find all safe tiles
                    while (nextTiles.Count > 0 && watch.ElapsedMilliseconds < 10000)
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
                        if (!current.Safe())
                        {
                            //Console.WriteLine(current.m_projectedPlayerTile.X + " " + current.m_projectedPlayerTile.Y + " is not safe " + current.m_cost);
                            continue;
                        }
                        else
                        {


                            if (current.m_cost <= 3 && m_trails.Contains(new KeyValuePair<int, int>(current.m_projectedPlayerTile.X, current.m_projectedPlayerTile.Y)))
                            {
                                continue;
                            }
                            //Console.WriteLine(current.m_projectedPlayerTile.X + " " + current.m_projectedPlayerTile.Y + " is safe " + current.m_cost);

                        }
                        //if (current.StateScore() > leadingState.StateScore())
                        //{
                        //    leadingState = current;
                        //}

                        //if (current.StateScore() < leadingState.StateScore() && current.m_cost > 20 + leadingState.m_cost)
                        //{
                        //    continue;
                        //}

                        ////Console.WriteLine(nextTiles.Count + " " + visited.Count + " Visiting:" + current.m_projectedPlayerTile.X + " " + current.m_projectedPlayerTile.Y + " current.m_cost:" + current.m_cost);

                        //bool shouldContinue = false;
                        //foreach (AStarBoardState it in visited)
                        //{
                        //    bool different = false;


                        //    if (it.m_bombMap.Count == current.m_bombMap.Count)
                        //    {
                        //        for (int i = 0; i < current.m_bombMap.Keys.Count; i++)
                        //        {
                        //            if (current.m_bombMap.ElementAt(i).Key.Key == it.m_bombMap.ElementAt(i).Key.Key && current.m_bombMap.ElementAt(i).Key.Value == it.m_bombMap.ElementAt(i).Key.Value && current.m_bombMap[it.m_bombMap.ElementAt(i).Key] == it.m_bombMap[it.m_bombMap.ElementAt(i).Key] && current.m_bombMap.ElementAt(i).Value == it.m_bombMap.ElementAt(i).Value)
                        //            {

                        //            }
                        //            else
                        //            {
                        //                different = true;
                        //            }
                        //        }
                        //    }
                        //    else
                        //    {
                        //        different = true;
                        //    }

                        //    if (it.m_portals.Count == current.m_portals.Count)
                        //    {
                        //        for (int i = 0; i < it.m_portals.Count; i++)
                        //        {
                        //            if (it.m_portals[i].m_x == current.m_portals[i].m_x && it.m_portals[i].m_y == current.m_portals[i].m_y && it.m_portals[i].m_isOrange == current.m_portals[i].m_isOrange)
                        //            {
                        //            }
                        //            else
                        //            {
                        //                different = true;
                        //            }
                        //        }
                        //    }
                        //    else
                        //    {
                        //        different = true;
                        //    }

                        //    if (!different && (it.m_projectedPlayerTile.X == current.m_projectedPlayerTile.X && it.m_projectedPlayerTile.Y == current.m_projectedPlayerTile.Y && it.m_projectedPlayerOrientation == current.m_projectedPlayerOrientation) && it.m_cost == current.m_cost)
                        //    {
                        //        shouldContinue = true;
                        //    }
                        //}

                        //if (current.m_projectedPlayerTile.GetBlockType() != Tile.blockType.Passable)
                        //{
                        //    continue;
                        //}


                        //if (shouldContinue || current.m_cost > 50) //TODO: This will no longer work fix this
                        //{
                        //    continue;
                        //}
                        ////////Console.WriteLine("Visiting " + current.m_moveToGetHere + " " + current.m_cost + " " + current.m_projectedPlayerTile.X + " " + current.m_projectedPlayerTile.Y + " " + current.m_projectedPlayerOrientation);

                        //visited.Add(current);

                        ShortenedBoardState shortState = current.GetShortenedState();

                        if (!visited.ContainsKey(shortState))
                        {
                            visited.Add(shortState, current.m_cost);
                        }
                        else if (visited[shortState] <= current.m_cost)
                        {
                            continue;
                        }
                        else
                        {
                            visited[shortState] = current.m_cost;
                        }


                        //TODO: calculate safe on step in individual tiles
                        if (current.m_cameFrom != null)
                        {
                            current.m_cameFrom.AddSafeMove(current);
                        }

                        current.TickBombs();


                        //TODO:this is a little bit tricky, try to solve it I guess
                        foreach (Portal p in portals)
                        {
                            AStarBoardState neighbor = p.GetTileOutlet(current);
                            if (neighbor != null)
                            {
                                nextTiles.Enqueue(neighbor);
                            }
                        }
                        if (current.m_projectedPlayerTile.X + 1 < m_parsed.boardSize)
                        {

                            nextTiles.Enqueue(current.MoveLeft(current));
                        }

                        if (current.m_projectedPlayerTile.Y - 1 > 0)
                        {
                            nextTiles.Enqueue(current.MoveUp(current));
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

                        if (current.m_coinsAvailable >= 5)
                        {
                            nextTiles.Enqueue(current.BuyPierce(current));
                            nextTiles.Enqueue(current.BuyBombs(current));
                            nextTiles.Enqueue(current.BuyRange(current));
                        }
                        nextTiles.Enqueue(current.DropBomb(current));
                        nextTiles.Enqueue(current.ShootBluePortal(current));
                        nextTiles.Enqueue(current.ShootOrangePortal(current));
                        nextTiles.Enqueue(current.TurnDown(current));
                        nextTiles.Enqueue(current.TurnLeft(current));
                        nextTiles.Enqueue(current.TurnRight(current));
                        nextTiles.Enqueue(current.TurnUp(current));
                        nextTiles.Enqueue(current.DoNothing(current));



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

                    Console.WriteLine("ChosenAction:" + chosenAction);
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
            Thread.Sleep(999999999);
            return false;
        }

    }
}
