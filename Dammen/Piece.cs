using System;
using System.Collections.Generic;

namespace Dammen
{
    public class MoveSet
    {
        public List<Move> moves;
        public int minScore;
        public int minScore2;
        public MoveSet()
        {
            
            moves = new List<Move>();
        }
    }
    public class Piece
    {
        
        public Color color;
        public enum Type {Normal = 0, Dam = 2};
        public Type type;
        public Piece(int x, int y, Color c, Type t)
        {
            this.color = c;
            this.type = t;
            this.x = x;
            this.y = y;
        }
        public bool taken;

        public int HashCode {
            get { return (color == Color.None ? 4 : (int)type | (int)color); }
        }

        public int x;
        public int y;
        public int squareNumIndex;

        public bool GetAllMoves(Board b, MoveSet moveset, TakingMove tm = null, int depth = 0, bool needClone = false)
        {
            if (color == Color.None)
                return false;
            bool childNeedsClone = false;
            bool foundNewMove = false;

            //int direction = color == Color.White ? 1 : -1;
            int maxSteps = type == Type.Dam ? 10 : 1;
            for (int yDirection = -1; yDirection <= 1; yDirection += 2) 
            {
                for (int xDirection = -1; xDirection <= 1; xDirection += 2) 
                {
                    for (int i = 1; i < maxSteps+1; i++)
                    {
                        int targetX = x + i * xDirection;
                        int targetY = y + i * yDirection;
                        if (targetX < 0 || targetX > 9 || targetY < 0 || targetY > 9)
                            break;
                        
                        if (b.pieces [targetX, targetY].color == this.color) { // a piece of our own army
                            break;
                        }
                        else if (b.pieces [targetX, targetY].color == Color.None)
                        {
                            // normal move, need to check y direction
                            if ((type == Type.Dam || (yDirection == (color == Color.White ? 1 : -1))) &&
                                tm == null && moveset.minScore == 0)
                            {
                                moveset.moves.Add (new Move(x, y, targetX, targetY)); // normal move, not taking anything
                            }
                        }
                        else // pieces of of opponent
                        {
                            if (b.pieces[targetX, targetY].taken || b.pieces[targetX, targetY] == this || 
                                targetX +xDirection < 0 || targetX + xDirection > 9 || 
                                targetY + yDirection < 0 || targetY + yDirection > 9)
                                break;
                            if (b.pieces [x + (i+1) * xDirection, y + (i+1) * yDirection].color != Color.None)
                                break;

                            foundNewMove = true;

                            // we can take it!
                            Color c = this.color;
                            this.color = Color.None;
                            
                            int victimX = targetX;
                            int victimY = targetY;
                            bool z = b.pieces[victimX, victimY].taken;
                            // mark it as taken so we cantb.pieces[victimX, victimY].taken jump over it again
                            b.pieces[victimX, victimY].taken = true;
                            
                            // We need to find moves depth-first, or our taken scheme will not work
                            do
                            {
                                targetX += xDirection;
                                targetY += yDirection;
                                if (targetX < 0 || targetX > 9 || targetY < 0 || targetY > 9 || 
                                                b.pieces [targetX,targetY].color != Color.None)
                                    break;
                                if (tm == null)
                                {
                                    tm = new TakingMove(x, y);
                                }
                                else if (needClone)
                                {
                                    tm = tm.Clone(depth);
                                }

#if DEBUG
                                if (b.pieces[victimX,victimY].color == Color.None)
                                    throw new Exception();
#endif
                                tm.AddHop(targetX,targetY, victimX, victimY, b.pieces [victimX, victimY].type);


                                b.pieces [targetX,targetY].color = c;
                                //TODO: type!!!
                                b.pieces[targetX, targetY].type = type;
                                childNeedsClone |= b.pieces [targetX,targetY].GetAllMoves(b, moveset, tm, depth + 1, childNeedsClone);
                                b.pieces [targetX,targetY].color = Color.None;
                                needClone = true;
                                
                            }
                            while (++i < maxSteps);


#if DEBUG
                            if (b.pieces[victimX, victimY].taken == false)
                                throw new Exception();
#endif
                            b.pieces[victimX, victimY].taken = false;
                            this.color = c;
                        }
                    }
                }
            }
            if (!foundNewMove && tm != null && tm.numTaken >= moveset.minScore)
            {
                moveset.moves.Add(tm);
                moveset.minScore = tm.numTaken;                
            }
            return foundNewMove;
        }
        
        
    }
}

