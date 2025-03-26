using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    // Extension method class to add extra functionality to collections.
    public static class CollectionExtension
    {
        // Random instance to generate random numbers.
        private static Random random = new Random();

        // Extension method for IList<T> to retrieve and remove a random element.
        public static T Random<T>(this IList<T> list)
        {
            // Choose a random element from the list.
            int index = random.Next(list.Count);
            T temp = list[index];
            // Remove the selected element to avoid reusing it.
            list.RemoveAt(index);
            return temp;
        }
    }

    // Gamecontroller class manages the overall poker game logic.
    class Gamecontroller
    {
        public Deck deck = new();                   // The deck of cards.
        public Table table = new();                 // The table with community cards.
        public List<Player> players_round = new();  // Players still active in the current round.
        public List<Player> players = new();        // All players in the game.
        public List<Player> players_fold = new();   // Players that have folded.
        public int chips_poll = 0;                  // Total chips bet in the current round.

        // Constructor: Initializes the game with four players.
        Gamecontroller(string p1, string p2, string p3, string p4)
        {
            players.Add(new Player(p1));
            players.Add(new Player(p2));
            players.Add(new Player(p3));
            players.Add(new Player(p4));
        }

        // Starts a new round: deals two cards to each player and processes initial betting.
        void Start_round()
        {
            players_round = new List<Player>(players);
            deck.generateDeck();

            // Loop through each player in the round
            foreach (Player p in players_round)
            {
                // Deal two cards to each player from the deck
                p.hand.card_1 = deck.deck.Random<Card>();
                p.hand.card_2 = deck.deck.Random<Card>();

                // Show cards to user
                Console.WriteLine($"{p.playername}: {p.hand.card_1.suit}_{p.hand.card_1.value}, {p.hand.card_2.suit}_{p.hand.card_2.value}");
                Console.WriteLine("if you want to bet type b, if you want to fold type f.");

                // Get user input
                string bet = Console.ReadLine().Trim();
                while (!bet.Equals("b") && !bet.Equals("f"))
                {
                    Console.WriteLine("You can only write b or f, try again");
                    bet = Console.ReadLine().Trim();
                }

                if (bet.Equals("b"))
                {
                    Console.WriteLine($"type the amount you want to bet you have {p.chips_amount} left. (write only number)");
                    int bet_val = int.Parse(Console.ReadLine().Trim());
                    while (bet_val > p.chips_amount)
                    {
                        Console.WriteLine("Can't bet more than you have, write new amount, remember only number");
                        bet_val = int.Parse(Console.ReadLine().Trim());
                    }
                    // Update chip values
                    p.chips_amount -= bet_val;
                    chips_poll += bet_val;
                }
                else
                {
                    // Player folds
                    players_fold.Add(p);
                }
            }

            // Remove folded players from active round
            foreach (Player p in players_fold)
            {
                players_round.Remove(p);
            }
        }

        // Handles flop (first 3 community cards)
        void flop_round()
        {
            Console.WriteLine("Flop: ");
            for (int i = 0; i < 3; i++)
            {
                table.flop.Add(deck.deck.Random<Card>());
                Console.WriteLine($"{table.flop[i].suit}_{table.flop[i].value}");
            }

            ProcessBetting();
        }

        // Handles turn (fourth community card)
        void turn_round()
        {
            table.turn = deck.deck.Random<Card>();
            Console.WriteLine($"Turn: {table.turn.suit}_{table.turn.value}");
            ProcessBetting();
        }

        // Handles river (fifth and final community card)
        void river_round()
        {
            table.river = deck.deck.Random<Card>();
            Console.WriteLine($"River: {table.river.suit}_{table.river.value}");
            ProcessBetting();
        }

        // Shared method for handling betting interactions
        void ProcessBetting()
        {
            foreach (Player p in players_round)
            {
                Console.WriteLine($"It's player {p.playername} turn.");
                Console.WriteLine("if you want to bet type b, if you want to fold type f.");
                string bet = Console.ReadLine().Trim();
                while (!bet.Equals("b") && !bet.Equals("f"))
                {
                    Console.WriteLine("You can only write b or f, try again");
                    bet = Console.ReadLine().Trim();
                }

                if (bet.Equals("b"))
                {
                    Console.WriteLine($"type the amount you want to bet you have {p.chips_amount} left. (write only number)");
                    int bet_val = int.Parse(Console.ReadLine().Trim());
                    while (bet_val > p.chips_amount)
                    {
                        Console.WriteLine("Can't bet more than you have, write new amount, remember only number");
                        bet_val = int.Parse(Console.ReadLine().Trim());
                    }
                    p.chips_amount -= bet_val;
                    chips_poll += bet_val;
                }
                else
                {
                    players_fold.Add(p);
                }
            }

            // Remove folded players from current round
            foreach (Player p in players_fold)
            {
                players_round.Remove(p);
            }
        }

        // Ends the round: evaluates hands, determines winner(s), distributes chips
        void end_round()
        {
            // Evaluate hands and get best 5-card hand
            foreach (Player p in players_round)
            {
                var allCards = new List<Card>(table.flop) { table.turn, table.river, p.hand.card_1, p.hand.card_2 };
                p.bestHand = PokerHandEvaluator.GetBestFiveCardHand(allCards);
                p.playerhandtype = PokerHandEvaluator.EvaluateHand(p.bestHand);

                // Return player cards to deck
                deck.deck.Add(p.hand.card_1);
                deck.deck.Add(p.hand.card_2);
            }

            // Return community cards
            deck.deck.AddRange(table.flop);
            deck.deck.Add(table.turn);
            deck.deck.Add(table.river);

            // Determine winner(s)
            var winners = PokerHandComparer.ComparePlayers(players_round);

            if (winners.Count == 1)
                Console.WriteLine("Winning player: " + winners[0].playername);
            else
                Console.WriteLine("It's a tie between: " + string.Join(", ", winners.Select(p => p.playername)));

            int split = chips_poll / winners.Count;
            foreach (Player winner in winners)
            {
                winner.chips_amount += split;
            }

            // Reset for next round
            chips_poll = 0;
            table.flop.Clear();
            players_fold.Clear();
        }

        // Executes a complete round
        void play_round()
        {
            Start_round();
            flop_round();
            turn_round();
            river_round();
            end_round();
        }

        // Main method: game testing
        /*public static void Main()
        {
            Gamecontroller game = new("p1", "p2", "p3", "p4");

            while (game.players.Count > 1)
            {
                game.play_round();
                game.players.RemoveAll(p => p.chips_amount == 0);
            }

            Console.WriteLine("The winner of the room is: " + game.players.First().playername);
        }*/
    }

    // Player class represents each participant in the game.
    internal class Player
    {
        internal string playername = "No name";   // The player's name.
        internal string playerhandtype = "";      // The evaluated hand type (e.g., "Flush", "Full House").
        internal int chips_amount = 0;            // The number of chips the player currently has.
        internal Hand hand = new();               // The player's hand (two private cards).
        internal List<Card> bestHand = new();     // The best 5-card hand for this player after evaluation.

        // Default constructor.
        internal Player() { }

        // Constructor that initializes a player with a name and a default chip amount.
        internal Player(string name)
        {
            playername = name;
            chips_amount = 2000;
        }

        // Constructor that allows setting a custom starting chip amount.
        internal Player(string name, int start_chips)
        {
            playername = name;
            chips_amount = start_chips;
        }
    }

    // Card class represents an individual playing card.
    internal class Card
    {
        internal string value;   // Card value (e.g., "2", "J", "A").
        internal string suit;    // Card suit (e.g., "Heart", "Diamond").

        // Default constructor initializes an "empty" card.
        internal Card()
        {
            value = "empty";
            suit = "empty";
        }

        // Constructor that sets the card's value and suit.
        internal Card(string Value, string Suit)
        {
            value = Value;
            suit = Suit;
        }
    }

    // Hand class represents the two private cards held by a player.
    internal class Hand
    {
        internal Card card_1 = new();  // First card.
        internal Card card_2 = new();  // Second card.
        internal Hand() { }
    }

    // Table class represents the shared community cards on the table.
    internal class Table
    {
        internal List<Card> flop = new();  // The three flop cards.
        internal Card turn = new();        // The turn card.
        internal Card river = new();       // The river card.
        internal Table() { }
    }

    // Deck class builds and manages the full 52-card deck.
    internal class Deck
    {
        internal List<Card> deck = new();          // The list of cards currently in the deck.
        internal List<string> value = new() { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
        internal List<string> suit = new() { "Heart", "Diamond", "Spade", "club" };

        // Constructor automatically builds the deck.
        internal Deck() => generateDeck();

        // Generates a standard 52-card deck.
        public void generateDeck()
        {
            deck.Clear();
            foreach (var s in suit)
                foreach (var v in value)
                    deck.Add(new Card(v, s));
        }
    }

    // PokerHandEvaluator class is responsible for evaluating the strength of poker hands.
    internal static class PokerHandEvaluator
    {
        // Standard order of card values used to determine hand strength.
        internal static readonly string[] CardValues = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };

        // Evaluates a 5-card hand and returns its ranking as a string.
        internal static string EvaluateHand(List<Card> cards)
        {
            var sorted = cards.OrderBy(card => Array.IndexOf(CardValues, card.value)).ToList();

            if (IsRoyalFlush(sorted)) return "Royal Flush";
            if (IsStraightFlush(sorted)) return "Straight Flush";
            if (IsFourOfAKind(sorted)) return "Four of a Kind";
            if (IsFullHouse(sorted)) return "Full House";
            if (IsFlush(sorted)) return "Flush";
            if (IsStraight(sorted)) return "Straight";
            if (IsThreeOfAKind(sorted)) return "Three of a Kind";
            if (IsTwoPair(sorted)) return "Two Pair";
            if (IsOnePair(sorted)) return "One Pair";

            return "High Card";
        }

        // Returns the best 5-card hand out of a list of 7 cards.
        internal static List<Card> GetBestFiveCardHand(List<Card> allCards)
        {
            var combinations = GetCombinations(allCards, 5);
            List<Card> best = new();
            string bestRank = "";

            foreach (var combo in combinations)
            {
                string currentRank = EvaluateHand(combo);
                if (best == null || PokerHandComparer.IsStronger(currentRank, combo, bestRank, best))
                {
                    best = combo;
                    bestRank = currentRank;
                }
            }
            return best;
        }

        // Generates all combinations of a certain size from a list of cards.
        private static IEnumerable<List<Card>> GetCombinations(List<Card> cards, int length)
        {
            if (length == 0) yield return new List<Card>();
            else
            {
                for (int i = 0; i <= cards.Count - length; i++)
                {
                    foreach (var tail in GetCombinations(cards.Skip(i + 1).ToList(), length - 1))
                    {
                        var result = new List<Card> { cards[i] };
                        result.AddRange(tail);
                        yield return result;
                    }
                }
            }
        }

        // Hand type checkers below:
        private static bool IsFlush(List<Card> cards) =>
            cards.GroupBy(c => c.suit).Any(g => g.Count() >= 5);

        private static bool IsStraight(List<Card> cards)
        {
            var indices = cards.Select(c => Array.IndexOf(CardValues, c.value)).Distinct().OrderBy(i => i).ToList();
            for (int i = 0; i <= indices.Count - 5; i++)
                if (indices[i + 4] - indices[i] == 4) return true;
            return false;
        }

        private static bool IsStraightFlush(List<Card> cards) =>
            cards.GroupBy(c => c.suit).Any(g => IsStraight(g.ToList()));

        private static bool IsRoyalFlush(List<Card> cards)
        {
            var royalValues = new HashSet<string> { "10", "J", "Q", "K", "A" };
            return cards.GroupBy(c => c.suit).Any(g => royalValues.All(v => g.Any(c => c.value == v)));
        }

        private static bool IsFourOfAKind(List<Card> cards) =>
            cards.GroupBy(c => c.value).Any(g => g.Count() == 4);

        private static bool IsFullHouse(List<Card> cards)
        {
            var groups = cards.GroupBy(c => c.value).ToList();
            return groups.Any(g => g.Count() == 3) && groups.Count(g => g.Count() >= 2) > 1;
        }

        private static bool IsThreeOfAKind(List<Card> cards) =>
            cards.GroupBy(c => c.value).Any(g => g.Count() == 3);

        private static bool IsTwoPair(List<Card> cards) =>
            cards.GroupBy(c => c.value).Count(g => g.Count() == 2) >= 2;

        private static bool IsOnePair(List<Card> cards) =>
            cards.GroupBy(c => c.value).Any(g => g.Count() == 2);
    }

    // PokerHandComparer class compares the evaluated hands of players and determines the winner(s).
    internal class PokerHandComparer
    {
        // Defines the hierarchy of hand strengths from weakest to strongest.
        private static readonly List<string> HandRankings = new()
        {
            "High Card", "One Pair", "Two Pair", "Three of a Kind",
            "Straight", "Flush", "Full House", "Four of a Kind",
            "Straight Flush", "Royal Flush"
        };

        // Compares players' hands and returns a list of the player(s) with the best hand.
        internal static List<Player> ComparePlayers(List<Player> players)
        {
            List<Player> winners = new();
            string bestRank = "";
            List<Card> bestHand = new();

            foreach (var player in players)
            {
                var rank = player.playerhandtype;
                var hand = player.bestHand;

                if (winners.Count == 0 || IsStronger(rank, hand, bestRank, bestHand))
                {
                    winners = new List<Player> { player };
                    bestRank = rank;
                    bestHand = hand;
                }
                else if (rank == bestRank && CompareHighCards(hand, bestHand) == 0)
                {
                    winners.Add(player);
                }
            }
            return winners;
        }

        // Determines whether the first hand is stronger than the second, considering hand type and kickers.
        internal static bool IsStronger(string rank1, List<Card> hand1, string rank2, List<Card> hand2)
        {
            int r1 = HandRankings.IndexOf(rank1);
            int r2 = HandRankings.IndexOf(rank2);
            if (r1 != r2) return r1 > r2;
            return CompareHighCards(hand1, hand2) > 0;
        }

        // Compares two hands with the same rank based on their highest cards.
        private static int CompareHighCards(List<Card> hand1, List<Card> hand2)
        {
            var sorted1 = hand1.OrderByDescending(c => Array.IndexOf(PokerHandEvaluator.CardValues, c.value)).ToList();
            var sorted2 = hand2.OrderByDescending(c => Array.IndexOf(PokerHandEvaluator.CardValues, c.value)).ToList();
            for (int i = 0; i < sorted1.Count; i++)
            {
                int cmp = Array.IndexOf(PokerHandEvaluator.CardValues, sorted1[i].value)
                    .CompareTo(Array.IndexOf(PokerHandEvaluator.CardValues, sorted2[i].value));
                if (cmp != 0) return cmp;
            }
            return 0;
        }
    }
}