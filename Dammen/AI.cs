using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace Dammen
{
    public class AI
    {
        private int tempBestMoveIndex;
        private Board b;
        private float tempBestScore;
        private bool stop;
        private Thread t;
        private ulong hash;

        public int bestMoveIndex { get; private set; }
        public float BestScore { get; private set; }

        public AI(Board b)
        {
            this.b = b;
        }

        public Move CalculateBestMove(int time)
        {
            stop = false;
            this.hash = b.CalculateHash();

            Console.WriteLine("Current score: " + b.RateBoard());
            Stopwatch totalsw = new Stopwatch();
            totalsw.Start();

            t = new Thread(new ThreadStart(Start));
            t.IsBackground = false;
            t.Start();
            while (t.IsAlive)
            {
                Thread.Sleep(100);
                time -= 100;
                if (time <= 0)
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
            b.numNodes = 0;
            for (int i = 1; i < 20; i++)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                tempBestMoveIndex = 0; // required for when theres only one valid move
                tempBestScore = ExplainMiniMax(i, isMaximizing, true, float.NegativeInfinity, float.PositiveInfinity, hash);
                if (stop)
                    return;
                sw.Stop();
                bestMoveIndex = tempBestMoveIndex;
                BestScore = tempBestScore;
                Console.WriteLine("Depth " + i + ": " + b.numNodes + " nodes in " + sw.ElapsedMilliseconds / 1000.0 + " s (" + b.numNodes / ((float)sw.ElapsedMilliseconds) + " kN/s)" +
                                  " Projected score: " + BestScore + " Best move: " + moves[bestMoveIndex].GetMoveDescription() + " q: " + Math.Pow((double)sw.ElapsedMilliseconds, 1/((double)i)));
            }
        }

        public float ExplainMiniMax(int depth, bool maximizing, bool isRootNode, float alpha, float beta, ulong hash)
        {
            if (this.stop)
                return (maximizing ? float.PositiveInfinity : float.NegativeInfinity);

//            #if USE_HASHES
            int hashIndex = (int)(hash % ((ulong)Board.HASH_TABLE_SIZE));

            if (depth <= 0)
                return b.RateBoard();

            int bestBet = 0;
            List<Move> moves;
            if (b.hashCollection [hashIndex].hash == hash)
            {
                moves = b.GetAllAllowedMoves(b.hashCollection [hashIndex].numTaken);
                bestBet = b.hashCollection [hashIndex].bestMoveIndex;
            }
            else
                moves = b.GetAllAllowedMoves();
            
//            #if USE_HASHES
            int length = moves.Count;
            if (length == 0)
                return b.RateBoard();

            hash ^= Board.currentColorHash; // invert for recursed calls
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
                #if DEBUG
                #if USE_HASHES
                if (hash != b.CalculateHash())
                    throw new Exception();
                #endif
                #endif
                Move m = moves[i];
                ulong newHash = m.Apply(b, hash);
                #if DEBUG
                #if USE_HASHES
                if (newHash != b.CalculateHash())
                    throw new Exception();
                #endif
                #endif
                #if CHECK_APPLY_UNDO
                Piece[,] pieces2 = new Piece[10,10];
                for (int q = 0; q < 10; q++)
                    for (int j = 0; j < 10; j++)
                        if (pieces[q,j] != null)
                            pieces2[q,j] = new Piece(pieces[q,j].color, pieces[q,j].type);
                #endif
                m.score = ExplainMiniMax(depth - 1, !maximizing, false, alpha, beta, newHash);
                
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
                b.hashCollection [hashIndex].bestMoveIndex = bestIndex;
                b.hashCollection [hashIndex].numTaken = (char)moves[bestIndex].numTaken;
                if (bestIndex >= length)
                    throw new Exception("can not add items that do not exist!");
                b.hashCollection [hashIndex].hash = hash ^ Board.currentColorHash;
                #if DEBUG
                b.hashCollection[hashIndex].perfectHash = b.PerfectHash();
                #endif
            }
            #endif
            tempBestMoveIndex = bestIndex;
            return bestScore;
        }
    }
}

