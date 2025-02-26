using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Gamecontroller
    {
        public Deck deck = new();
    }

    internal class Servercard : Card
    {
        protected internal Servercard(String Value, String Suit) : base()
        {
            value = Value;
            suit = Suit;
        }
    }

    internal class Hand {
        Card card_1;
        Card card_2;
        internal Hand() {
            card_1 = new Card();
            card_2 = new Card();
        }
    }

    internal class Deck
    {
        List<Card> deck;
        List<String> value;
        List<String> suit;

        internal Deck()
        {
            value = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King", "Ace"];
            suit = ["Heart", "Diamond", "Spade", "club"];
            generateDeck();
        }


        public void generateDeck() {
            foreach (var S in suit)
            {
                foreach (var V in value)
                {
                    deck.Add(new Servercard(V, S));
                }
            }
        }
    }


}
