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
        public Piece[,] pieces = new Piece[10,10];
        public Piece[] pieces1D = new Piece[50];
        public Color currentColor;

        public int NumBoardRatings { get; private set; }

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
            //return rand.Next((int)max);
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
        
        private int squareIndexToX(int index)
        {
            return 2 * (index % 5) + ((index / 5) % 2 == 0 ? 1 : 0);
        }
        private int squareIndexToY(int index)
        {
            return 9 - index / 5;
        }
        
        public Board ()
        {
            for (int i = 0; i < 20; i++) {
                pieces[squareIndexToX(49 - i), squareIndexToY(49 - i)] = new Piece(squareIndexToX(49 - i), squareIndexToY(49 - i), Color.White, Piece.Type.Normal);
                pieces[squareIndexToX(i), squareIndexToY(i)] = new Piece(squareIndexToX(i), squareIndexToY(i), Color.Black, Piece.Type.Normal);
                //pieces[9 - (i%10), 9 - i/10] = new Piece(Color.Black, Piece.Type.Normal);
            }
            for (int i = 0; i < 20; i += 2) {
                pieces [i % 10 + ((i / 10) % 2 != 0 ? 1 : 0), i / 10 + 4] = new Piece (i % 10 + ((i / 10) % 2 != 0 ? 1 : 0), i / 10 + 4, Color.None, Piece.Type.Normal);
            }
            
            for (int i = 0; i < 50; i++)
            {
                pieces1D [i] = pieces [squareIndexToX(i), squareIndexToY (i)];
                pieces1D[i].squareNumIndex = i;
            }

            currentColor = Color.White;
        }
        
        private void RemoveIllegalMoves(MoveSet ms)
        {
            ms.moves.RemoveAll(a => a.numTaken < ms.minScore);
        }
        public List<Move> GetAllAllowedMoves(int minPiecesToTake = 0)
        {
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
            NumBoardRatings++;
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