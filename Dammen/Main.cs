using System;
namespace Dammen
{
	class MainClass
	{
		public static void Main (string[] args)
		{
            Board b = new Board ();

            Player player1 = null;
            foreach (var a in args)
            {
                if (a.Contains("human"))
                    player1 = new HumanPlayer(b);
            }

            if (player1 == null)
                player1 = new AI(b, 10000);
            Player player2 = new AI(b, 10000);

            Player currentPlayer = player1;
            while (true)
            {
                b.PrintBoard();
                Move m = currentPlayer.GetMove();
                if (m == null)
                    break;
                m.Apply(b);
                b.currentColor = (b.currentColor == Color.White ? Color.Black : Color.White);
                currentPlayer = (currentPlayer == player1 ? player2 : player1);
            }
		}
	}
}
