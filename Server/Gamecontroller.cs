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

        // Extension method for IList<type> to retrieve and remove a random element.
        public static type Random<type>(this IList<type> list)
        {
            // Choose a random element from the list.
            type temp = list[random.Next(list.Count)];
            // Remove the selected element to avoid reusing it.
            list.Remove(temp);
            return temp;
        }
    }

    // Gamecontroller class manages the overall poker game logic.
    class Gamecontroller
    {
        public Deck deck = new();               // The deck of cards.
        public Table table = new();             // The table with community cards.
        public List<Player> players_round = new();  // Players still active in the current round.
        public List<Player> players = new();        // All players in the game.
        public List<Player> players_fold = new();     // Players that have folded.
        public int chips_poll = 0;              // Total chips bet in the current round.

        // Constructor: Initializes the game with four players.
        Gamecontroller(String p1, String p2, String p3, String p4)
        {
            players.Add(new Player(p1));
            players.Add(new Player(p2));
            players.Add(new Player(p3));
            players.Add(new Player(p4));
        }

        // Starts a new round: deals two cards to each player and processes initial betting.
        void Start_round()
        {
            // Copy the full players list to the round's active players.
            players_round = new List<Player>(players);

            // Loop through each player in the round.
            foreach (Player p in players_round)
            {
                // Deal two cards to the player from the deck.
                p.hand.card_1 = deck.deck.Random<Card>();
                p.hand.card_2 = deck.deck.Random<Card>();

                // Display the player's cards.
                Console.WriteLine(p.playername + ": " + p.hand.card_1.suit + "_" + p.hand.card_1.value + ", " + p.hand.card_2.suit + "_" + p.hand.card_2.value);
                Console.WriteLine("if you want to bet type b, if you want to fold type f.");

                // Get the player's decision.
                String bet = Console.ReadLine().Trim();
                // Validate input until a valid option is entered.
                while (!bet.Equals("b") && !bet.Equals("f"))
                {
                    Console.WriteLine("You can only write b or f, try again");
                    bet = Console.ReadLine();
                    bet = bet.Trim();
                }
                // Process bet if the player chooses to bet.
                if (bet.Equals("b"))
                {
                    Console.WriteLine("type the amount you want to bet you have " + p.chips_amount + " left. (write only number)");
                    int bet_val = int.Parse(Console.ReadLine().Trim());
                    // Ensure the bet does not exceed the player's chips.
                    while (bet_val > p.chips_amount)
                    {
                        Console.WriteLine("Can't bet more than you have, write new amount, remember only number");
                        bet_val = int.Parse(Console.ReadLine().Trim());
                    }
                    // Deduct the bet amount and add it to the pool.
                    p.chips_amount -= bet_val;
                    chips_poll += bet_val;
                }
                // If player folds, add to the folded players list.
                else
                {
                    players_fold.Add(p);
                }
            }
            // Remove all folded players from the active round list.
            foreach (Player p in players_fold)
            {
                players_round.Remove(p);
            }
        }

        // Processes the flop round: reveals three community cards and processes betting.
        void flop_round()
        {
            Console.WriteLine("Flop: ");
            // Reveal 3 community cards.
            for (int i = 0; i < 3; i++)
            {
                table.flop.Add(deck.deck.Random<Card>());
                Console.WriteLine(table.flop[i].suit + "_" + table.flop[i].value);
            }
            // Process betting for each active player.
            foreach (Player p in players_round)
            {
                Console.WriteLine("It's player " + p.playername + " turn.");
                Console.WriteLine("if you want to bet type b, if you want to fold type f.");
                String bet = Console.ReadLine().Trim();
                while (!bet.Equals("b") && !bet.Equals("f"))
                {
                    Console.WriteLine("You can only write b or f, try again");
                    bet = Console.ReadLine();
                    bet = bet.Trim();
                }
                if (bet.Equals("b"))
                {
                    Console.WriteLine("type the amount you want to bet you have " + p.chips_amount + " left. (write only number)");
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
            // Remove folded players from the active round.
            foreach (Player p in players_fold)
            {
                players_round.Remove(p);
            }
        }

        // Processes the turn round: reveals the turn card and processes betting.
        void turn_round()
        {
            // Reveal the turn card.
            table.turn = deck.deck.Random<Card>();
            Console.WriteLine("Turn: " + table.turn.suit + "_" + table.turn.value);

            // Process betting for each active player.
            foreach (Player p in players_round)
            {
                Console.WriteLine("It's player " + p.playername + " turn.");
                Console.WriteLine("if you want to bet type b, if you want to fold type f.");
                String bet = Console.ReadLine().Trim();
                while (!bet.Equals("b") && !bet.Equals("f"))
                {
                    Console.WriteLine("You can only write b or f, try again");
                    bet = Console.ReadLine();
                    bet = bet.Trim();
                }
                if (bet.Equals("b"))
                {
                    Console.WriteLine("type the amount you want to bet you have " + p.chips_amount + " left. (write only number)");
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
            // Remove players who have folded.
            foreach (Player p in players_fold)
            {
                players_round.Remove(p);
            }
        }

        // Processes the river round: reveals the river card and processes betting.
        void river_round()
        {
            // Reveal the river card.
            table.river = deck.deck.Random<Card>();
            Console.WriteLine("River: " + table.river.suit + "_" + table.river.value);

            // Process betting for each active player.
            foreach (Player p in players_round)
            {
                Console.WriteLine("It's player " + p.playername + " turn.");
                Console.WriteLine("if you want to bet type b, if you want to fold type f.");
                String bet = Console.ReadLine().Trim();
                while (!bet.Equals("b") && !bet.Equals("f"))
                {
                    Console.WriteLine("You can only write b or f, try again");
                    bet = Console.ReadLine();
                    bet = bet.Trim();
                }
                if (bet.Equals("b"))
                {
                    Console.WriteLine("type the amount you want to bet you have " + p.chips_amount + " left. (write only number)");
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
            // Remove players who have folded.
            foreach (Player p in players_fold)
            {
                players_round.Remove(p);
            }
        }

        // Ends the round: evaluates hands, determines winners, awards chips, and resets round variables.
        void end_round()
        {
            // Evaluate each active player's hand and return cards to the deck.
            foreach (Player p in players_round)
            {
                p.playerhandtype = PokerHandEvaluator.EvaluateHand(table.flop, table.turn, table.river, p.hand.card_1, p.hand.card_2);
                // Return player's cards to the deck.
                deck.deck.Add(p.hand.card_1);
                deck.deck.Add(p.hand.card_2);
            }
            // Return all community cards to the deck.
            deck.deck.AddRange(table.flop);
            deck.deck.Add(table.turn);
            deck.deck.Add(table.river);

            // Determine the winning players based on hand evaluation.
            List<Player> winners = PokerHandComparer.ComparePlayers(players_round);

            // Display the result.
            if (winners.Count == 1)
                Console.WriteLine("Winning player: " + winners[0].playername);
            else
                Console.WriteLine("It's a tie between: " + string.Join(", ", winners.Select(p => p.playername)));

            // Calculate each winner's share of the pot.
            int splitAmount = chips_poll / winners.Count;

            // Award each winner with their share of chips.
            foreach (Player winner in winners)
            {
                winner.chips_amount += splitAmount;
            }

            // Reset the chips pool for the next round.
            chips_poll = 0;

            // Clear the table and folded players list for the next round.
            table.flop.Clear();
            players_fold.Clear();
        }

        // Plays a full round of the game by executing all rounds in order.
        void play_round()
        {
            Start_round();
            flop_round();
            turn_round();
            river_round();
            end_round();
        }

        // Main method: entry point of the game.
        /*public static void Main()
        {
            // Initialize a new game with four players.
            Gamecontroller Game = new("p1", "p2", "p3", "p4");

            // Continue playing rounds until only one player remains.
            while (Game.players.Count > 1)
            {
                Game.play_round();
                // After each round, remove all players who have 0 chips.
                Game.players.RemoveAll(p => p.chips_amount == 0);
            }

            // Declare the final winner.
            Console.WriteLine("The winner of the room is: " + Game.players.First().playername);
        }*/ // game works
    }

    // Player class represents each participant in the game.
    internal class Player
    {
        internal String playername = "No name";   // The player's name.
        internal String playerhandtype = "";      // The evaluated hand type (e.g., "Flush", "Full House").
        internal int chips_amount = 0;            // The number of chips the player currently has.
        internal Hand hand = new();               // The player's hand (two cards).

        // Default constructor.
        internal Player() { }

        // Constructor that initializes a player with a name and a default chip amount.
        internal Player(String name)
        {
            playername = name;
            chips_amount = 2000;
        }

        // Constructor that allows setting a custom starting chip amount.
        internal Player(String name, int start_chips)
        {
            playername = name;
            chips_amount = start_chips;
        }
    }

    // Card class represents an individual playing card.
    internal class Card
    {
        internal String value;   // Card value (e.g., "2", "J", "A").
        internal String suit;    // Card suit (e.g., "Heart", "Diamond").

        // Default constructor initializes an "empty" card.
        internal Card()
        {
            value = "empty";
            suit = "empty";
        }

        // Constructor that sets the card's value and suit.
        internal Card(String Value, String Suit)
        {
            value = Value;
            suit = Suit;
        }
    }

    // Hand class represents the two cards held by a player.
    internal class Hand
    {
        internal Card card_1 = new();  // First card.
        internal Card card_2 = new();  // Second card.
        internal Hand() { }
    }

    // Table class represents the community cards on the table.
    internal class Table
    {
        internal List<Card> flop = new();  // The three flop cards.
        internal Card turn = new();        // The turn card.
        internal Card river = new();       // The river card.
        internal Table() { }
    }

    // Deck class builds and manages the deck of cards.
    internal class Deck
    {
        internal List<Card> deck = new();          // List holding the deck's cards.
        internal List<String> value = new();         // Possible card values.
        internal List<String> suit = new();          // Possible card suits.

        // Constructor initializes the card values and suits, and generates the deck.
        internal Deck()
        {
            // Define the card values.
            value = new List<string> { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            // Define the card suits.
            suit = new List<string> { "Heart", "Diamond", "Spade", "club" };
            generateDeck();
        }

        // Generates a standard 52-card deck.
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

    // PokerHandEvaluator class is responsible for evaluating the strength of a poker hand.
    internal class PokerHandEvaluator
    {
        // Standard order of card values used for evaluation.
        private static readonly string[] CardValues = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };

        // Evaluates a hand given the community cards and a player's two cards.
        internal static string EvaluateHand(List<Card> flop, Card turn, Card river, Card hand1, Card hand2)
        {
            // Combine all available cards into one list.
            List<Card> allCards = new List<Card>(flop) { turn, river, hand1, hand2 };
            // Order cards based on their value using the standard order.
            allCards = allCards.OrderBy(card => Array.IndexOf(CardValues, card.value)).ToList();

            // Check for various hand types from best to worst.
            if (IsRoyalFlush(allCards)) return "Royal Flush";
            if (IsStraightFlush(allCards)) return "Straight Flush";
            if (IsFourOfAKind(allCards)) return "Four of a Kind";
            if (IsFullHouse(allCards)) return "Full House";
            if (IsFlush(allCards)) return "Flush";
            if (IsStraight(allCards)) return "Straight";
            if (IsThreeOfAKind(allCards)) return "Three of a Kind";
            if (IsTwoPair(allCards)) return "Two Pair";
            if (IsOnePair(allCards)) return "One Pair";

            // If no other hand is made, return the highest card.
            return "High Card: " + allCards.Last().value;
        }

        // Checks if the cards form a Flush (five or more cards of the same suit).
        private static bool IsFlush(List<Card> cards)
        {
            return cards.GroupBy(c => c.suit).Any(g => g.Count() >= 5);
        }

        // Checks if the cards form a Straight (five consecutive card values).
        private static bool IsStraight(List<Card> cards)
        {
            // Get distinct card values as their index positions.
            var distinctValues = cards.Select(c => Array.IndexOf(CardValues, c.value)).Distinct().OrderBy(v => v).ToList();
            // Check for a sequence of 5 consecutive values.
            for (int i = 0; i <= distinctValues.Count - 5; i++)
            {
                if (distinctValues[i + 4] - distinctValues[i] == 4)
                    return true;
            }
            return false;
        }

        // Checks if the cards form a Straight Flush.
        private static bool IsStraightFlush(List<Card> cards)
        {
            // Group cards by suit and check if any group has a straight.
            return cards.GroupBy(c => c.suit).Any(g => IsStraight(g.ToList()));
        }

        // Checks if the cards form a Royal Flush.
        private static bool IsRoyalFlush(List<Card> cards)
        {
            // Define the required values for a Royal Flush.
            var royalValues = new HashSet<string> { "10", "J", "Q", "K", "A" };
            // Check each suit group to see if it contains all royal values.
            return cards.GroupBy(c => c.suit).Any(g => royalValues.All(v => g.Any(c => c.value == v)));
        }

        // Checks if the cards form Four of a Kind.
        private static bool IsFourOfAKind(List<Card> cards)
        {
            return cards.GroupBy(c => c.value).Any(g => g.Count() == 4);
        }

        // Checks if the cards form a Full House.
        private static bool IsFullHouse(List<Card> cards)
        {
            return cards.GroupBy(c => c.value).Count(g => g.Count() == 3) > 0 &&
                   cards.GroupBy(c => c.value).Count(g => g.Count() == 2) > 0;
        }

        // Checks if the cards form Three of a Kind.
        private static bool IsThreeOfAKind(List<Card> cards)
        {
            return cards.GroupBy(c => c.value).Any(g => g.Count() == 3);
        }

        // Checks if the cards form Two Pair.
        private static bool IsTwoPair(List<Card> cards)
        {
            return cards.GroupBy(c => c.value).Count(g => g.Count() == 2) >= 2;
        }

        // Checks if the cards form One Pair.
        private static bool IsOnePair(List<Card> cards)
        {
            return cards.GroupBy(c => c.value).Any(g => g.Count() == 2);
        }
    }

    // PokerHandComparer class compares the evaluated hands of players and determines the winner(s).
    internal class PokerHandComparer
    {
        // Defines the ranking of hands from lowest to highest.
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

        // Compares players' hands and returns the list of winning players.
        internal static List<Player> ComparePlayers(List<Player> players)
        {
            // If only one player remains, they are the winner.
            if (players.Count == 1)
                return new List<Player> { players[0] };

            // Initialize the best player as the first player.
            Player bestPlayer = players[0];
            List<Player> winningPlayers = new List<Player> { bestPlayer };

            // Compare each player's hand with the best found so far.
            foreach (Player player in players.Skip(1))
            {
                int currentRank = GetHandRank(player.playerhandtype);
                int bestRank = GetHandRank(bestPlayer.playerhandtype);
                if (currentRank > bestRank)
                {
                    // Found a better hand, update bestPlayer and reset winners.
                    bestPlayer = player;
                    winningPlayers.Clear();
                    winningPlayers.Add(player);
                }
                else if (currentRank == bestRank)
                {
                    // If both players have "High Card", compare the highest card values.
                    if (currentRank == 0)
                    {
                        string bestHighCard = GetHighCardValue(bestPlayer.playerhandtype);
                        string currentHighCard = GetHighCardValue(player.playerhandtype);
                        if (CompareCardValues(currentHighCard, bestHighCard) > 0)
                        {
                            bestPlayer = player;
                            winningPlayers.Clear();
                            winningPlayers.Add(player);
                        }
                        else if (CompareCardValues(currentHighCard, bestHighCard) == 0)
                        {
                            // If high cards are equal, add player to tie.
                            winningPlayers.Add(player);
                        }
                    }
                    else
                    {
                        // For other hand types with same rank, add player as a winner.
                        winningPlayers.Add(player);
                    }
                }
            }
            return winningPlayers;
        }

        // Returns the ranking index of the given hand.
        private static int GetHandRank(string hand)
        {
            // "High Card" is handled separately.
            if (hand.StartsWith("High Card: "))
                return 0;
            return Handrankings.IndexOf(hand);
        }

        // Extracts the high card value from a "High Card" hand string.
        private static string GetHighCardValue(string hand)
        {
            return hand.Replace("High Card: ", "");
        }

        // Compares two card values based on their order.
        private static int CompareCardValues(string card1, string card2)
        {
            string[] cardOrder = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            return Array.IndexOf(cardOrder, card1).CompareTo(Array.IndexOf(cardOrder, card2));
        }
    }
}
