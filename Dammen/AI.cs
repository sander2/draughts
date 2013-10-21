using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace Dammen
{
    public abstract class Player
    {
        public abstract Move GetMove();
    }

    public class HumanPlayer : Player
    {
        private Board b;
        public HumanPlayer(Board b)
        {
            this.b = b;
        }

        public override Move GetMove()
        {
            List<Move> allowedMoves = b.GetAllAllowedMoves();
            while (true)
            {
                Console.WriteLine("Enter a move:");
                string s = Console.ReadLine();
                foreach (var a in allowedMoves)
                    if (a.GetMoveDescription().Equals(s))
                        return a;
                Console.WriteLine("Move not valid; valid moves are: ");
                foreach (var a in allowedMoves)
                    Console.WriteLine(a.GetMoveDescription());
                allowedMoves = b.GetAllAllowedMoves();
            }
        }
    }

    public class AI : Player
    {
        private int tempBestMoveIndex;
        private Board b;
        private float tempBestScore;
        private bool stop;
        private Thread t;
        private ulong hash;
        private int time;

        public int bestMoveIndex { get; private set; }
        public float BestScore { get; private set; }

        private struct HashMatch
        {
            #if DEBUG
            public string perfectHash;
            #endif
            public ulong hash;
            public int bestMoveIndex;
            public char numTaken;
        };
        private const int HASH_TABLE_SIZE = 32*1024*1024;
        private HashMatch[] hashCollection = new HashMatch[HASH_TABLE_SIZE];
        public static ulong[,] zobristPieceMask;
        public static ulong zobristColorMask;

        public AI(Board b, int time)
        {
            this.b = b;
            this.time = time;
            Random rand = new Random();
            if (zobristPieceMask == null)
            {
                zobristPieceMask = new ulong[50, 4];
                for (int i = 0; i < 50; i++)
                    for (int j = 0; j < 4; j++)
                        zobristPieceMask[i,j] = ((ulong)rand.Next()) << 32 | (ulong)rand.Next();
                zobristColorMask = ((ulong)rand.Next()) << 32 | (ulong)rand.Next();
            }

            ClearHashes();
        }

        /// <summary>
        /// Calculates the hash.
        /// </summary>
        /// <returns>The hash.</returns>
        public ulong CalculateHash(Board brd)
        {
            ulong hash = 0;
            for (int i = 0; i < 50; i++)
                if (brd.pieces1D[i].color != Color.None)
                    hash ^= (ulong)AI.zobristPieceMask[i, brd.pieces1D[i].HashCode];
            if (brd.currentColor == Color.Black)
                hash ^= AI.zobristColorMask;
            return hash;
        }

        private void ClearHashes()
        {
            for (int i = 0; i < hashCollection.Length; i++)
            {
                hashCollection[i].bestMoveIndex = 0;
                hashCollection[i].hash = 0;
            }
        }

        public override Move GetMove()
        {
            stop = false;
            this.hash = CalculateHash(b);

            Console.WriteLine("Current score: " + b.RateBoard());
            Stopwatch totalsw = new Stopwatch();
            totalsw.Start();

            t = new Thread(new ThreadStart(Start));
            t.IsBackground = false;
            t.Start();
            int timeLeft = time;
            while (t.IsAlive)
            {
                Thread.Sleep(100);
                timeLeft -= 100;
                if (timeLeft <= 0)
                    stop = true;
            }
            List<Move> moves = b.GetAllAllowedMoves();
            
            totalsw.Stop();
            Console.WriteLine("Total time taken: " + totalsw.ElapsedMilliseconds / 1000.0 + " s");

            return moves [bestMoveIndex];
        }

        private void Start()
        {
            bool isMaximizing = (b.currentColor == Color.White);
            Console.WriteLine("Playing for " + (isMaximizing ? "white" : "black"));
            List<Move> moves = b.GetAllAllowedMoves();
            Console.WriteLine("test");
            for (int i = 1; i < 20; i++)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                tempBestMoveIndex = 0; // required for when theres only one valid move
                tempBestScore = MiniMax(i, isMaximizing, true, float.NegativeInfinity, float.PositiveInfinity, hash);
                if (stop)
                    return;
                sw.Stop();
                bestMoveIndex = tempBestMoveIndex;
                BestScore = tempBestScore;
                Console.WriteLine("Depth " + i + ": " + b.NumBoardRatings + " nodes in " + sw.ElapsedMilliseconds / 1000.0 + " s (" + b.NumBoardRatings / ((float)sw.ElapsedMilliseconds) + " kN/s)" +
                                  " Projected score: " + BestScore + " Best move: " + moves[bestMoveIndex].GetMoveDescription() + " q: " + Math.Pow((double)sw.ElapsedMilliseconds, 1/((double)i)));
            }
        }

        private float MiniMax(int depth, bool maximizing, bool isRootNode, float alpha, float beta, ulong hash)
        {
            if (this.stop)
                return (maximizing ? float.PositiveInfinity : float.NegativeInfinity);

//            #if USE_HASHES
            int hashIndex = (int)(hash % ((ulong)HASH_TABLE_SIZE));

            if (depth <= 0)
                return b.RateBoard();

            int bestBet = 0;
            List<Move> moves;
            if (hashCollection [hashIndex].hash == hash)
            {
                moves = b.GetAllAllowedMoves(hashCollection [hashIndex].numTaken);
                bestBet = hashCollection [hashIndex].bestMoveIndex;
            }
            else
                moves = b.GetAllAllowedMoves();
            
//            #if USE_HASHES
            int length = moves.Count;
            if (length == 0)
                return b.RateBoard();

            hash ^= AI.zobristColorMask; // invert for recursed calls
//            #endif




           
            float bestScore = (maximizing? float.NegativeInfinity : float.PositiveInfinity);
            int bestIndex = 0;
            
            Color c = b.currentColor;

            // no need to think if there's only one option
            if (isRootNode && length == 1)
                return 0;


            // import to do AFTER one-move checking
            b.currentColor = (b.currentColor == Color.Black ? Color.White : Color.Black);
            int i = bestBet;
            do
            {
                Move m = moves[i];
                ulong newHash = m.Apply(b, hash);
              
                #if CHECK_APPLY_UNDO
                Piece[,] pieces2 = new Piece[10,10];
                for (int q = 0; q < 10; q++)
                    for (int j = 0; j < 10; j++)
                        if (pieces[q,j] != null)
                            pieces2[q,j] = new Piece(pieces[q,j].color, pieces[q,j].type);
                #endif
                m.score = MiniMax(depth - 1, !maximizing, false, alpha, beta, newHash);
                
                #if CHECK_APPLY_UNDO
                for (int q = 0; q < 10; q++)
                    for (int j = 0; j < 10; j++)
                        if (pieces [q, j] != null)
                            if (pieces2 [q, j].color != pieces [q, j].color || (pieces2 [q, j].color != Color.None && pieces [q, j].type != pieces2 [q, j].type))
                        {
                            PrintBoard();
                            int qewqwe = 5;
                        }
                #endif
                if (maximizing && m.score > alpha)
                    alpha = m.score;
                else if (!maximizing && m.score < beta)
                    beta = m.score;
                
                if ((maximizing && m.score > bestScore) || (!maximizing && m.score < bestScore))
                {
                    bestScore = m.score;
                    bestIndex = i;
                }
                
                m.Undo(b, hash);
                
                if (beta <= alpha)
                    break;
                
                i++;
                if (i == length)
                    i = 0;
            }
            while (i != bestBet);

            b.currentColor = c;
            
            #if USE_HASHES
            if (bestIndex != 0/* && hashCollection [hashIndex].hash == 0*/)
            {
                hashCollection [hashIndex].bestMoveIndex = bestIndex;
                hashCollection [hashIndex].numTaken = (char)moves[bestIndex].numTaken;
                if (bestIndex >= length)
                    throw new Exception("can not add items that do not exist!");
                hashCollection [hashIndex].hash = hash ^ AI.zobristColorMask;
//                #if DEBUG
//                b.hashCollection[hashIndex].perfectHash = b.PerfectHash();
//                #endif
            }
            #endif
            tempBestMoveIndex = bestIndex;
            return bestScore;
        }
    }
}

