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

    internal class Hand 
    {
        Card card_1;
        Card card_2;
        internal Hand() { }
    }

    internal class Table
    {
        List<Card> flop;
        Card turn;
        Card river;

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
        List<Card> deck;
        List<String> value;
        List<String> suit;

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
                    deck.Add(new Servercard(V, S));
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
