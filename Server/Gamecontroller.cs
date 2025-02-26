using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public static class CollectionExtension
    {
        private static Random random = new Random();
        public static type Random<type>(this IList<type> list)
        {
            return list[random.Next(list.Count)];
        }
    }
    class Gamecontroller
    {
        public Deck deck = new();
        public Table table = new();
        public List<player> players = new();

        Gamecontroller(String p1, String p2, String p3, String p4) {
            players.Add(new player(p1));
            players.Add(new player(p2));
            players.Add(new player(p3));
            players.Add(new player(p4));
        }

        void Start_round()
        {
            foreach (player p in players)
            {
                p.hand.card_1 = deck.deck.Random<Card>();
                p.hand.card_2 = deck.deck.Random<Card>();
            }

        }

        void flop_round()
        {

        }

        void turn_round() 
        {

        }

        void river_round()
        {

        }
    }

    internal class player
    {
        internal String playername;
        internal int chips_amount;
        internal Hand hand = new();

        internal player()
        {
            playername = "No name";
            chips_amount = 0;
        }
        internal player(String name)
        {
            playername = name;
            chips_amount = 2000;
        }
        internal player(String name, int start_chips)
        {
            playername = name;
            chips_amount = start_chips;
        }


    }

    internal class Card
    {
        internal String value;
        internal String suit;
        internal Card()
        {
            value = "empty";
            suit = "empty";
        }
        internal Card(String Value, String Suit)
        {
            value = Value;
            suit = Suit;
        }
    }

    internal class Hand 
    {
        internal Card card_1;
        internal Card card_2;
        internal Hand() { }
    }

    internal class Table
    {
        internal List<Card> flop = new List<Card>();
        internal Card turn = new();
        internal Card river = new();

        internal Table() { }

        void SetTable(Card flop1, Card flop2, Card flop3, Card turn1, Card river1)
        {
            flop.Clear();
            flop.Add(flop1);
            flop.Add(flop2);
            flop.Add(flop3);
            turn = turn1;
            river = river1;
        }
    }

    internal class Deck
    {
        internal List<Card> deck = new();
        internal List<String> value = new();
        internal List<String> suit = new();

        internal Deck()
        {
            value = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King", "Ace"];
            suit = ["Heart", "Diamond", "Spade", "club"];
            generateDeck();
        }

        public void generateDeck() 
        {
            foreach (var S in suit)
            {
                foreach (var V in value)
                {
                    deck.Add(new Card(V, S));
                }
            }
        }
    }

    internal class PokerHandEvaluator
    {
        private static readonly string[] CardValues = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };

        public static string EvaluateHand(List<Card> flop, Card turn, Card river, Card hand1, Card hand2)
        {
            List<Card> allCards = new List<Card>(flop) { turn, river, hand1, hand2 };
            allCards = allCards.OrderBy(card => Array.IndexOf(CardValues, card.value)).ToList();

            if (IsRoyalFlush(allCards)) return "Royal Flush";
            if (IsStraightFlush(allCards)) return "Straight Flush";
            if (IsFourOfAKind(allCards)) return "Four of a Kind";
            if (IsFullHouse(allCards)) return "Full House";
            if (IsFlush(allCards)) return "Flush";
            if (IsStraight(allCards)) return "Straight";
            if (IsThreeOfAKind(allCards)) return "Three of a Kind";
            if (IsTwoPair(allCards)) return "Two Pair";
            if (IsOnePair(allCards)) return "One Pair";

            return "High Card: " + allCards.Last().value;
        }

        private static bool IsFlush(List<Card> cards)
        {
            return cards.GroupBy(c => c.suit).Any(g => g.Count() >= 5);
        }

        private static bool IsStraight(List<Card> cards)
        {
            var distinctValues = cards.Select(c => Array.IndexOf(CardValues, c.value)).Distinct().OrderBy(v => v).ToList();
            for (int i = 0; i <= distinctValues.Count - 5; i++)
            {
                if (distinctValues[i + 4] - distinctValues[i] == 4)
                    return true;
            }
            return false;
        }

        private static bool IsStraightFlush(List<Card> cards)
        {
            return cards.GroupBy(c => c.suit).Any(g => IsStraight(g.ToList()));
        }

        private static bool IsRoyalFlush(List<Card> cards)
        {
            var royalValues = new HashSet<string> { "10", "J", "Q", "K", "A" };
            return cards.GroupBy(c => c.suit).Any(g => royalValues.All(v => g.Any(c => c.value == v)));
        }

        private static bool IsFourOfAKind(List<Card> cards)
        {
            return cards.GroupBy(c => c.value).Any(g => g.Count() == 4);
        }

        private static bool IsFullHouse(List<Card> cards)
        {
            return cards.GroupBy(c => c.value).Count(g => g.Count() == 3) > 0 && cards.GroupBy(c => c.value).Count(g => g.Count() == 2) > 0;
        }

        private static bool IsThreeOfAKind(List<Card> cards)
        {
            return cards.GroupBy(c => c.value).Any(g => g.Count() == 3);
        }

        private static bool IsTwoPair(List<Card> cards)
        {
            return cards.GroupBy(c => c.value).Count(g => g.Count() == 2) >= 2;
        }

        private static bool IsOnePair(List<Card> cards)
        {
            return cards.GroupBy(c => c.value).Any(g => g.Count() == 2);
        }
    }


}
