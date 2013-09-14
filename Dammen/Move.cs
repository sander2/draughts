#if DEBUG
#define CHECK_APPLY_UNDO
#endif
using System;
using System.Collections.Generic;
using System.Collections;

namespace Dammen
{
    public class TakingMove : Move
    {
#if CHECK_APPLY_UNDO
        Piece[,] pieces2 = new Piece[10,10];
        Piece[,] pieces4 = new Piece[10,10];
#endif
        public struct Hop 
        {
            public int targetX; 
            public int targetY;
            public int victimX;
            public int victimY;
            public Piece.Type victimType;
        }
        public List<Hop> hops;

        public void AddHop(int x, int y, int victimX, int victimY, Piece.Type victimType)
        {
            for (int i = 0; i < hops.Count; i++)
                if (hops[i].victimX == victimX && hops[i].victimY == victimY)
                    throw new Exception("Cant jump same piece twice!");

            hops.Add(new Hop{targetX = x, targetY = y,victimX = victimX, victimY = victimY, victimType = victimType});
            numTaken++;
        }
        public TakingMove() : base()
        {
            hops = new List<Hop>();
        }
        public TakingMove(int x, int y) : base()
        {
            base.x = x;
            base.y = y;
            hops = new List<Hop>();
        }
        public TakingMove Clone(int depth)
        {
            TakingMove tm = new TakingMove();
            tm.x = this.x;
            tm.y = this.y;
            foreach (var hop in hops)
            {
                if (depth <= 0)
                    return tm;
                depth--;
                tm.AddHop(hop.targetX, hop.targetY, hop.victimX, hop.victimY, hop.victimType);
            }
            return tm;
        }

        public override ulong Apply(Board b, ulong hash = 0)
        {
#if CHECK_APPLY_UNDO
            for (int q = 0; q < 10; q++)
                for (int j = 0; j < 10; j++)
                    if (b.pieces[q,j] != null)
                        pieces2[q,j] = new Piece(q,j,b.pieces[q,j].color, b.pieces[q,j].type);
#endif
#if DEBUG
#if USE_HASHES
                if (hash != b.CalculateHash())
                    throw new Exception();
#endif
#endif

            typePriorToApply = b.pieces [x, y].type;
            int len = hops.Count;
            
            // remove origin
            hash ^= Board.hashDing[b.pieces[x, y].squareNumIndex, b.pieces [x, y].HashCode];

            // update origin and destination
            // required to do in steps for the case when begin == end
            Color c = b.pieces [x, y].color;
            b.pieces [x, y].color = Color.None; // dont care what the type is if it in not occupied
            b.pieces [hops [len - 1].targetX, hops [len - 1].targetY].color = c;
            b.pieces [hops [len - 1].targetX, hops [len - 1].targetY].type = typePriorToApply;

            // Promotion:
            if (hops [len - 1].targetY == (c == Color.White ? 9 : 0))
                b.pieces [hops [len - 1].targetX, hops [len - 1].targetY].type = Piece.Type.Dam;

            // add destination
            hash ^= Board.hashDing[b.pieces[hops [len - 1].targetX, hops [len - 1].targetY].squareNumIndex, 
                                   b.pieces [hops [len - 1].targetX, hops [len - 1].targetY].HashCode];


            // remove victims
            for (int i = 0; i < len; i++)
            {
#if DEBUG
                if (b.pieces [hops [i].victimX, hops [i].victimY].color == Color.None)
                    throw new Exception();
#endif
                hash ^= Board.hashDing[b.pieces[hops [i].victimX, hops [i].victimY].squareNumIndex, 
                                       b.pieces [hops [i].victimX, hops [i].victimY].HashCode];

                b.pieces [hops [i].victimX, hops [i].victimY].color = Color.None;
            }
#if DEBUG
#if USE_HASHES
            if (hash != b.CalculateHash())
                throw new Exception();
#endif
#endif
#if CHECK_APPLY_UNDO
            for (int q = 0; q < 10; q++)
                for (int j = 0; j < 10; j++)
                    if (b.pieces[q,j] != null)
                        pieces4[q,j] = new Piece(q, j, b.pieces[q,j].color, b.pieces[q,j].type);
#endif
            return hash;
        }


        /// <summary>
        /// Undo the move
        /// </summary>
        /// <param name="b">the board.</param>
        /// <param name="hash">Hash.</param>
        public override void Undo(Board b, ulong hash = 0)
        {
            #if CHECK_APPLY_UNDO
                
                Piece[,] pieces3 = new Piece[10,10];
                for (int q = 0; q < 10; q++)
                    for (int j = 0; j < 10; j++)
                        if (b.pieces[q,j] != null)
                            pieces3[q,j] = new Piece(q, j, b.pieces[q,j].color, b.pieces[q,j].type);

                for (int q = 0; q < 10; q++)
                    for (int j = 0; j < 10; j++)
                        if (b.pieces [q, j] != null)
                            if (pieces4 [q, j].color != b.pieces [q, j].color || (pieces4 [q, j].color != Color.None && b.pieces [q, j].type != pieces4 [q, j].type))
                            {
                                b.PrintBoard();
                                int qewqwe = 5;
                            }
            #endif

            int len = hops.Count;

            // required to do in steps for the case when begin == end
            
            Color c = b.pieces [hops [len - 1].targetX, hops [len - 1].targetY].color;
            b.pieces [hops [len - 1].targetX, hops [len - 1].targetY].color = Color.None;
            b.pieces [x, y].color = c;
            b.pieces [x, y].type = typePriorToApply;

            Color victimColor = (c == Color.Black ? Color.White : Color.Black);
            for (int i = 0; i < len; i++)
            {
                b.pieces [hops [i].victimX, hops [i].victimY].color = victimColor;
                b.pieces [hops [i].victimX, hops [i].victimY].type = hops[i].victimType;
            }

#if CHECK_APPLY_UNDO
            for (int q = 0; q < 10; q++)
                for (int j = 0; j < 10; j++)
                    if (b.pieces [q, j] != null)
                        if (pieces2 [q, j].color != b.pieces [q, j].color || (pieces2 [q, j].color != Color.None && b.pieces [q, j].type != pieces2 [q, j].type))
                        {
                            b.PrintBoard();
                            int qewqwe = 5;
                        }
#endif
        }


        public override string GetMoveDescription()
        {
            string s = ToHumanReadablePos(x, y).ToString();
            foreach (var a in hops)
                s += " x " + ToHumanReadablePos(a.targetX, a.targetY).ToString();
            return s;
        }
    }

    public class Move
    {
        public int x;
        public int y;
        public int targetX;
        public int targetY;
        public int numTaken;
        public float score;
        public Piece.Type typePriorToApply;
        protected Move()
        {
        }
        public Move(int x, int y, int targetX, int targetY, int score = 0)
        {
            this.x = x;
            this.y = y;
            this.targetX = targetX;
            this.targetY = targetY;
            this.numTaken = score;
        }

        
        public virtual ulong Apply(Board b, ulong hash = 0)
        {
            typePriorToApply = b.pieces [x, y].type;
            b.pieces [targetX, targetY].color = b.pieces [x, y].color;
            b.pieces [targetX, targetY].type = b.pieces [x, y].type;
            if (targetY == (b.pieces [targetX, targetY].color == Color.White ? 9 : 0))
                b.pieces [targetX, targetY].type = Piece.Type.Dam;

            // update hash
            hash ^= Board.hashDing[ToHumanReadablePos(x, y) - 1, b.pieces [x, y].HashCode];
            hash ^= Board.hashDing[ToHumanReadablePos(targetX, targetY) - 1, b.pieces [targetX, targetY].HashCode];

            b.pieces [x, y].color = Color.None;

            return hash;
        }
        public virtual void Undo(Board b, ulong hash = 0)
        {
            b.pieces [x, y].color = b.pieces [targetX, targetY].color;
            b.pieces [x, y].type = typePriorToApply;
            b.pieces [targetX, targetY].color = Color.None;
        }
        
        public int HumanReadablePos
        {
            get
            {
                return 50 - ((9-x) + 10 * y + (y%2 == 1 ? 1 : 0)) / 2;
            }
        }
        public static int ToHumanReadablePos(int x, int y)
        {
            return 50 - ((9-x) + 10 * y + (y%2 == 1 ? 1 : 0)) / 2;
        }
        public virtual string GetMoveDescription()
        {
            string s = ToHumanReadablePos(x, y) + " " + ToHumanReadablePos(targetX, targetY);
            return s;
        }
    }
}

