//#define AVOID_HORIZON_EFFECT
#if DEBUG
#define CHECK_APPLY_UNDO
#endif
//#define USE_HASHES
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Dammen
{
    public enum Color {Black = 0, White = 1, None = 4};
    
    public class Board
    {
        int totalNumNodes = 0;

        public ulong bitboardWhitePieces;
        public ulong bitboardBlackPieces;

        public Piece[,] pieces = new Piece[10,10];
        public Piece[] pieces1D = new Piece[50];
        public Color currentColor;
        public static int[] squareIndexToX;
        public static int[] squareIndexToY;
        public int numNodes = 0;
        public static Random rand = new Random();

        public struct HashMatch
        {
#if DEBUG
            public string perfectHash;
#endif
            public ulong hash;
            public int bestMoveIndex;
            public char numTaken;
        };
        public static ulong[,] hashDing = new ulong[50, 4];
        public static ulong currentColorHash;
        public const int HASH_TABLE_SIZE = 32*1024*1024;
        public HashMatch[] hashCollection = new HashMatch[HASH_TABLE_SIZE];
        
        public void SetupTestBoard1()
        {
            for (int i = 0; i < 40; i += 2) {
                pieces[i%10 + ((i/10)%2 != 0 ? 1 : 0), i/10] = new Piece(i%10 + ((i/10)%2 != 0 ? 1 : 0), i/10, Color.White, Piece.Type.Normal);
                pieces[i%10 + ((i/10)%2 != 0 ? 1 : 0), i/10 + 6] = new Piece(i%10 + ((i/10)%2 != 0 ? 1 : 0), i/10 + 6, Color.Black, Piece.Type.Normal);
                //pieces[9 - (i%10), 9 - i/10] = new Piece(Color.Black, Piece.Type.Normal);
            }
            for (int i = 0; i < 20; i += 2) {
                pieces [i % 10 + ((i / 10) % 2 != 0 ? 1 : 0), i / 10 + 4] = new Piece (i % 10 + ((i / 10) % 2 != 0 ? 1 : 0), i / 10 + 4, Color.None, Piece.Type.Normal);
            }
            pieces [0, 4].color = Color.White;
            pieces [2, 4].color = Color.White;
            pieces [3, 7].color = Color.None;

        }
        uint seed = 12312412;
        int fakeRand(uint max)
        {
            return rand.Next((int)max);
            seed = seed * 214013 + 2531011;
            return (int)(seed % max);
        }
        public void SetupRandomBoard()
        {
            for (int i = 0; i < 50; i++)
            {
                pieces1D[i].color = Color.None;
            }
            int numBlack = 10 + fakeRand(10);
            int numWhite = 10 + fakeRand(10);
            while (true)
            {
                int i = 5 + fakeRand(40);
                if (pieces1D[i].color == Color.None)
                {
                    pieces1D[i].color = Color.White;
                    pieces1D[i].type = Piece.Type.Normal;
                    if (numBlack-- <= 0)
                        break;
                }
            }
            while (true)
            {
                int i = 5 + fakeRand(40);
                if (pieces1D[i].color == Color.None)
                {
                    pieces1D[i].type = Piece.Type.Normal;
                    pieces1D[i].color = Color.Black;
                    if (numWhite-- <= 0)
                        break;
                }
            }
        }
        AI ai;
        public Board ()
        {
            squareIndexToX = new int[50];
            squareIndexToY = new int[50];
            for (int i = 0; i < 100; i += 2)
            {
                squareIndexToX[i/2] = (i % 10 + ((i / 10) % 2 != 0 ? 0 : 1));
                squareIndexToY[i/2] =  9 - i / 10;
            }

            for (int i = 0; i < 20; i++) {
                pieces[squareIndexToX[49 - i], squareIndexToY[49 - i]] = new Piece(squareIndexToX[49 - i], squareIndexToY[49 - i], Color.White, Piece.Type.Normal);
                pieces[squareIndexToX[i], squareIndexToY[i]] = new Piece(squareIndexToX[i], squareIndexToY[i], Color.Black, Piece.Type.Normal);
                //pieces[9 - (i%10), 9 - i/10] = new Piece(Color.Black, Piece.Type.Normal);
            }
            for (int i = 0; i < 20; i += 2) {
                pieces [i % 10 + ((i / 10) % 2 != 0 ? 1 : 0), i / 10 + 4] = new Piece (i % 10 + ((i / 10) % 2 != 0 ? 1 : 0), i / 10 + 4, Color.None, Piece.Type.Normal);
            }

            for (int i = 0; i < 50; i++)
            {
                pieces1D [i] = pieces [squareIndexToX [i], squareIndexToY [i]];
                pieces1D[i].squareNumIndex = i;
            }
            for (int i = 0; i < 50; i++)
                for (int j = 0; j < 4; j++)
                    hashDing[i,j] = ((ulong)rand.Next()) << 32 | (ulong)rand.Next();
            currentColorHash = ((ulong)rand.Next()) << 32 | (ulong)rand.Next();

            //SetupRandomBoard();
            ClearHashes();
            DoPerft();
            //SetupTestBoard1();

            ai = new AI(this);


            Stopwatch sw = new Stopwatch();
            sw.Start();
            currentColor = Color.White;

            PlaySelf();
//            PlayPlayer();
//            Console.WriteLine("Node counting test.. " + (Perft(8) == 6465506 ? "passed" : "failed"));
//            sw.Stop();
            Console.WriteLine("Total positions considered (less is better): " + totalNumNodes + "; baseline = 2014327");
            Console.WriteLine("Game finished in " + sw.ElapsedMilliseconds / 1000.0f + " s (baseline = 10 s; 9 without debugger)");
//            PlayPlayer();
        }

        private void ClearHashes()
        {
            // reset hashes
            for (int i = 0; i < hashCollection.Length; i++)
            {
                hashCollection[i].bestMoveIndex = 0;
                hashCollection[i].hash = 0;
            }
        }

        /// <summary>
        /// Calculates the hash.
        /// </summary>
        /// <returns>The hash.</returns>
        public ulong CalculateHash()
        {
            ulong hash = 0;
            for (int i = 0; i < 50; i++)
                if (pieces1D[i].color != Color.None)
                hash ^= (ulong)hashDing[i, pieces1D[i].HashCode];
            if (currentColor == Color.Black)
                hash ^= currentColorHash;
            return hash;
        }

        public void PlaySelf()
        {
            int movenumber = 0;
                PrintBoard();
            
            while (true)
            {
                Move bestMove = GetBestMove();
                if (bestMove == null)
                    break;
                //Console.WriteLine("Best move: " + bestMove.GetMoveDescription());
                bestMove.Apply(this, CalculateHash());
                PrintBoard();

                
                currentColor = (currentColor == Color.Black ? Color.White : Color.Black);
                Console.WriteLine("Move " + movenumber + "; score " + RateBoard());
                movenumber++;
               // Console.WriteLine("nodes: " + numNodes.ToString());
                
            }
            Console.WriteLine("No more moves found");
        }
//        public void PlayPlayer()
//        {
//            int movenumber = 0;
//            while (true)
//            {
//                PrintBoard();
//                Move bestMove = GetBestMove();
//                if (bestMove == null)
//                    break;
//                bestMove.Apply(this);
//                Console.WriteLine("Best move: " + bestMove.GetMoveDescription());
//                Console.WriteLine("Move " + movenumber + "; score " + RateBoard());
//                currentColor = (currentColor == Color.Black ? Color.White : Color.Black);
//                movenumber++;
//                PrintBoard();
//                Move playerMove = GetPlayerMove();
//                playerMove.Apply(this);
//                currentColor = (currentColor == Color.Black ? Color.White : Color.Black);
//                movenumber++;
//            }
//            Console.WriteLine("No more moves found");
//        }
//        Move GetPlayerMove()
//        {
//            List<Move> allowedMoves = GetAllAllowedMoves();
//            while (true)
//            {
//                Console.WriteLine("Enter a move:");
//                string s = Console.ReadLine();
//                foreach (var a in allowedMoves)
//                    if (a.GetMoveDescription().Equals(s))
//                        return a;
//                Console.WriteLine("Move not valid; valid moves are: ");
//                foreach (var a in allowedMoves)
//                    Console.WriteLine(a.GetMoveDescription());
//                allowedMoves = GetAllAllowedMoves();
//            }
//        }


        public void RemoveIllegalMoves(MoveSet ms)
        {
            ms.moves.RemoveAll(a => a.numTaken < ms.minScore);
        }
        public List<Move> GetAllAllowedMoves(int minPiecesToTake = 0)
        {
            //   00  01  02  03  04
            // 05  06  07  08  09
            //   10  11  12  13  14
            // 15  16  17  18  19           etc


            // calculate see if we can take any pieces// assume white playing
//            ulong takebits =11 bitboardWhitePieces & (bitboardWhitePieces << 5)  & somemask);
//            takebits 

            MoveSet ms = new MoveSet();
            ms.minScore = minPiecesToTake;

            for (int i = 0; i < 50; i++) 
                if (pieces1D[i].color == currentColor)
                    pieces1D[i].GetAllMoves (this, ms);

            RemoveIllegalMoves(ms);

            if (ms.minScore > 0 && ms.moves.Count == 0)
                throw new Exception("Invalid minScore value");

            return ms.moves;
        }
        public Move GetBestMove()
        {
            return ai.CalculateBestMove(10000);

#if DEBUG
                if (c != currentColor)
                    throw new Exception();
#endif

            // we dont care about inefficiency here, as this is only executed
            // once per turn:
         

        }





       


        public string PerfectHash()
        {
            string hash = "";
            string bla = "abcd";
            for (int i = 0; i < 50; i++)
                if (pieces1D [i].color != Color.None)
                    hash += bla [pieces1D [i].HashCode];
                else
                    hash += ".";
            if (currentColor == Color.Black)
                hash = "BLACK" + hash;
            else
                hash = "WHITE" + hash;
            return hash;
        }

        public void PrintBoard()
        {
            int squarenum = 1;
            Console.WriteLine("┌──┬──┬──┬──┬──┬──┬──┬──┬──┬──┐ ┌──┬──┬──┬──┬──┬──┬──┬──┬──┬──┐");
            for (int i = 9; i >= 0; i--)
            {
                if (i != 9)
                    Console.WriteLine("├──┼──┼──┼──┼──┼──┼──┼──┼──┼──┤ ├──┼──┼──┼──┼──┼──┼──┼──┼──┼──┤");
                for (int j = 0; j < 10; j++)
                {
                    if (pieces [j, i] != null && pieces [j, i].color == Color.Black)
                        Console.Write(pieces [j, i].type == Piece.Type.Dam ? "│⛁ " : "│⛀ ");
                    else if (pieces [j, i] != null && pieces [j, i].color == Color.White)
                        Console.Write(pieces [j, i].type == Piece.Type.Dam ? "│⛃ " : "│⛂ ");
                    else
                        Console.Write("│  ");
                }
                Console.Write("│ ");
                for (int j = 0; j < 10; j++)
                {
                    if (pieces [j, i] != null)
                    {
                        Console.Write("│" + squarenum.ToString("D2"));
                        squarenum++;
                    }
                    else
                        Console.Write("│  ");
                }
                                      Console.WriteLine("│ ");
            }
            Console.WriteLine("└──┴──┴──┴──┴──┴──┴──┴──┴──┴──┘ └──┴──┴──┴──┴──┴──┴──┴──┴──┴──┘");
        }
        int[] ids = new int[] {22,23,26,27};
        
        public float RateBoard() // positive for white, negative for black
        {
            int totalValue = 0;
            int numBlackPawns = 0, numWhitePawns = 0, numBlackKings = 0, numWhiteKings = 0;
            int whitePosY = 0, blackPosY = 0;

            for (int i = 0; i < 50; i++)
            {
                Piece p = pieces1D[i];
                if (p.color == Color.White)
                {
                    if (p.type == Piece.Type.Normal)
                    {
                        numWhitePawns++;
                        whitePosY += p.y;
                    }
                    else
                        numWhiteKings++;
                }
                else if (p.color == Color.Black)
                {
                    if (p.type == Piece.Type.Normal)
                    {
                        numBlackPawns++;
                        blackPosY = (9 - p.y);
                    }
                    else
                        numBlackKings++;
                }              
            }


            int numBlack = numBlackKings + numBlackPawns;
            int numWhite = numWhiteKings+numWhitePawns;
            if (numBlack == 0)
                return float.PositiveInfinity;
            else if (numWhite == 0)
                return float.NegativeInfinity;

            // count pieces; 5 pts for kings, 1 for pawns
            float val = 5.0f * (numWhiteKings - numBlackKings) + (numWhitePawns - numBlackPawns);

            // ratio of pieces
            val += ((float)numWhite / ((float)numWhite + numBlack)) - 0.5f;


            val += 0.125f * (((float)whitePosY / (float)numWhite) - ((float)blackPosY / (float)numBlack));

            for (int i = 0; i < 4; i++)
            {
                if (pieces1D[ids[i]].color == Color.Black)
                    val -= 0.1f;
                else if (pieces1D[ids[i]].color == Color.White)
                    val += 0.1f;
            }
            numNodes++;
            totalNumNodes++;
            return val;
        }


        void DoPerft()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            ulong num = Perft(9);
            num += Perft(9);
            num += Perft(9);
            sw.Stop();
            Console.WriteLine("Perft: " + num/3 + " nodes in " + sw.ElapsedMilliseconds / 3000.0 + " s");
            Console.WriteLine("Previous result: 41022614 nodes in 18.453 s");
        }

        ulong Perft(int depth)
        {
            if (depth <= 0)
                return 1;
            ulong numNodes = 0;
            List<Move> moves = GetAllAllowedMoves();
            Color c = currentColor;
            currentColor = (currentColor == Color.Black ? Color.White : Color.Black);
            int length = moves.Count;
           
            for (int i = 0; i < length; i++)
            {
                Move m = moves[i];
                m.Apply(this);
                numNodes += Perft(depth - 1);
                m.Undo(this);
            }
            currentColor = c;
            return numNodes;
        }
    }
}