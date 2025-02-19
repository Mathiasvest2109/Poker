using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker.Server
{
    internal class GameController
    {

    }

    internal class Servercard : Card {


        protected internal Servercard(String Value, String Suit): base()
        {
            value = Value;
            suit = Suit;
        }
    }
    internal class Deck 
    {
        List<Card> deck;
        List<String> value = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King", "Ace"];
        List<String> suit = ["Heart", "Diamond", "Spade", "club"];

        Deck() {
            foreach (var S in suit){
                foreach (var V in value) {
                    deck.Add(new Servercard(V,S));
                }
            }
        }
        
    }
}
