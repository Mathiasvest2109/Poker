using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public static class CollectionExtension
    {
        private static Random random = new Random();
        public static type Random<type>(this IList<type> list)
        {
            type temp = list[random.Next(list.Count)];
            list.Remove(temp);
            return temp;
        }
    }
    class Gamecontroller
    {
        public Deck deck = new();
        public Table table = new();
        public List<Player> players_round = new();
        public List<Player> players = new();
        Gamecontroller(String p1, String p2, String p3, String p4) {
            players.Add(new Player(p1));
            players.Add(new Player(p2));
            players.Add(new Player(p3));
            players.Add(new Player(p4));
        }
        void Start_round()
        {
            players_round = players;
            foreach (Player p in players_round)
            {
                p.hand.card_1 = deck.deck.Random<Card>();
                p.hand.card_2 = deck.deck.Random<Card>();
                Console.WriteLine(p.playername+": "+p.hand.card_1.suit+"_"+p.hand.card_1.value+", "+p.hand.card_2.suit+"_"+p.hand.card_2.value);
            }
            //betting and folding handling
            //if player fold remove from player_round list
        }
        void flop_round()
        {
            Console.WriteLine("Flop: ");
            for (int i = 0; i < 3; i++)
            {
                table.flop.Add(deck.deck.Random<Card>());
                Console.WriteLine(table.flop[i].suit + "_" + table.flop[i].value);
            }
            //betting and folding handling
            //if player fold remove from player_round list
        }
        void turn_round() 
        {
            table.turn = deck.deck.Random<Card>();
            Console.WriteLine("Turn: "+table.turn.suit + "_" + table.turn.value);
            //betting and folding handling
            //if player fold remove from player_round list
        }
        void river_round()
        {
            table.river = deck.deck.Random<Card>();
            Console.WriteLine("River: " + table.river.suit + "_" + table.river.value);
            //betting and folding handling
            //if player fold remove from player_round list
        }
        void end_round()
        {
            foreach (Player p in players_round)
            {
                p.playerhandtype = PokerHandEvaluator.EvaluateHand(table.flop,table.turn,table.river,p.hand.card_1,p.hand.card_2);
                deck.deck.Add(p.hand.card_1);
                deck.deck.Add(p.hand.card_2);
            }
            deck.deck.AddRange(table.flop);
            deck.deck.Add(table.turn);
            deck.deck.Add(table.river);
            String winner = PokerHandComparer.ComparePlayers(players_round);
            //winner or winners need to get the pot
            Console.WriteLine(winner);
            table.flop.Clear();
        }
        /*public static void Main()
        {
            Gamecontroller Game = new("p1","p2","p3","p4");
            Game.Start_round();
            Game.flop_round();
            Game.turn_round();
            Game.river_round();
            Game.end_round();

        }*/
    }

    internal class Player
    {
        internal String playername;
        internal String playerhandtype;
        internal int chips_amount;
        internal Hand hand = new();
        internal Player()
        {
            playername = "No name";
            chips_amount = 0;
        }
        internal Player(String name)
        {
            playername = name;
            chips_amount = 2000;
        }
        internal Player(String name, int start_chips)
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
        internal Card card_1 = new();
        internal Card card_2 = new();
        internal Hand() { }
    }

    internal class Table
    {
        internal List<Card> flop = new();
        internal Card turn = new();
        internal Card river = new();
        internal Table() { }
    }

    internal class Deck
    {
        internal List<Card> deck = new();
        internal List<String> value = new();
        internal List<String> suit = new();
        internal Deck()
        {
            value = ["2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A"];
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
        private static readonly string[] CardValues = {"2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
        internal static string EvaluateHand(List<Card> flop, Card turn, Card river, Card hand1, Card hand2)
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

    internal class PokerHandComparer
    {
        private static readonly List<string> Handrankings = new List<string>
    {
        "High Card",
        "One Pair",
        "Two Pair",
        "Three of a Kind",
        "Straight",
        "Flush",
        "Full House",
        "Four of a Kind",
        "Straight Flush",
        "Royal Flush"
        };
        internal static string ComparePlayers(List<Player> Players)
        {
            if (Players.Count == 1)
                return "Winning Player: " + Players[0].playername;
            Player bestplayer = Players[0];
            List<Player> winningplayers = new List<Player> { bestplayer };
            foreach (Player player in Players.Skip(1))
            {
                int currentrank = GetHandrank(player.playerhandtype);
                int bestrank = GetHandrank(bestplayer.playerhandtype);
                if (currentrank > bestrank)
                {
                    bestplayer = player;
                    winningplayers.Clear();
                    winningplayers.Add(player);
                }
                else if (currentrank == bestrank)
                {
                    if (currentrank == 0)
                    {
                        string bestHighCard = GetHighCardValue(bestplayer.playerhandtype);
                        string currentHighCard = GetHighCardValue(player.playerhandtype);
                        if (CompareCardValues(currentHighCard, bestHighCard) > 0)
                        {
                            bestplayer = player;
                            winningplayers.Clear();
                            winningplayers.Add(player);
                        }
                        else if (CompareCardValues(currentHighCard, bestHighCard) == 0)
                        {
                            winningplayers.Add(player);
                        }
                    }
                    else
                    {
                        winningplayers.Add(player);
                    }
                }
            }
            if (winningplayers.Count == 1)
                return "Winning player: " + bestplayer.playername;
            else
                return "It's a Tie between: " + string.Join(", ", winningplayers.Select(p => p.playername));
        }
        private static int GetHandrank(string hand)
        {
            if (hand.StartsWith("High Card: "))
                return 0;
            return Handrankings.IndexOf(hand);
        }
        private static string GetHighCardValue(string hand)
        {
            return hand.Replace("High Card: ", "");
        }
        private static int CompareCardValues(string card1, string card2)
        {
            string[] cardOrder = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            return Array.IndexOf(cardOrder, card1).CompareTo(Array.IndexOf(cardOrder, card2));
        }
    }
}
