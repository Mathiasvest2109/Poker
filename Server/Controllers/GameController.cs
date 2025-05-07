using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Server.Hubs;

namespace Server.Services
{
    // Extension method for picking and removing a random element from a list (used for dealing cards)
    public static class CollectionExtension
    {
        // Random instance to generate random numbers.
        private static readonly Random random = new Random();
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

    // Represents a playing card
    public class Card
    {
        public string suit;
        public string value;
    }

    // Represents a deck of cards
    public class Deck
    {
        public List<Card> d = new();

        public Deck()
        {
            // Initialize a standard 52-card deck
            string[] suits = { "Hearts", "Diamonds", "Clubs", "Spades" };
            string[] values = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            foreach (var suit in suits)
                foreach (var value in values)
                    d.Add(new Card { suit = suit, value = value });
        }
    }

    // Represents a player in the game
    public class Player
    {
        public string playername;
        public string ConnectionId;
        public Hand hand;
        public int chips = 1000; // Starting chips
        internal string handtype;
        internal List<Card> besthand;
    }

    // Represents a player's hand (two cards)
    public class Hand
    {
        public Card card_1;
        public Card card_2;
    }

    // Represents the community cards on the table
    public class Table
    {
        public Card flop1, flop2, flop3, turn, river;
    }

    // Main game controller for a single table
    public class Gamecontroller
    {
        private readonly IHubContext<PokerHub> _hubContext;
        private readonly string _tableId;
        public List<Player> players = new();
        public List<Player> players_round = new();
        public int currentPlayerIndex = 0;
        public List<Player> players_fold = new();
        public Deck deck = new();
        public Table table = new();
        private int bettingRound = 0; // 0 = pre-flop, 1 = flop, 2 = turn, 3 = river
        private HashSet<string> actedThisRound = new();
        private int currentBet = 0;
        private Dictionary<Player, int> playerBets = new();
        private int dealerIndex = 0;
        private int smallBlind = 10;
        private int bigBlind = 20;
        private int pot = 0;

        private static readonly Dictionary<string, int> CardRank = new()
        {
            ["2"] = 2, ["3"] = 3, ["4"] = 4, ["5"] = 5, ["6"] = 6, ["7"] = 7, ["8"] = 8, ["9"] = 9,
            ["10"] = 10, ["J"] = 11, ["Q"] = 12, ["K"] = 13, ["A"] = 14
        };

        // Constructor: initializes players and stores SignalR context and table id
        public Gamecontroller(List<TablePlayer> tablePlayers, IHubContext<PokerHub> hubContext, string tableId)
        {
            _hubContext = hubContext;
            _tableId = tableId;
            foreach (var tp in tablePlayers)
                players.Add(new Player { playername = tp.Name, ConnectionId = tp.ConnectionId });
            players_round = new List<Player>(players);
        }

        // Starts a new round: deals cards, notifies players, and begins betting
        public async Task PlayRoundAsync()
        {
            deck = new Deck(); // Reset deck with 52 cards
            players_round = new List<Player>(players);
            foreach (var p in players_round)
            {
                p.hand = new Hand
                {
                    card_1 = CollectionExtension.Random<Card>(deck.d),
                    card_2 = CollectionExtension.Random<Card>(deck.d)
                };
                playerBets[p] = 0;
            }

            await _hubContext.Clients.Group(_tableId).SendAsync("ReceiveTableMessage", "System", "The game has started!", DateTime.UtcNow);

            foreach (var player in players)
            {
                await _hubContext.Clients.Group(_tableId).SendAsync(
                    "ReceiveTableMessage",
                    "System",
                    $"{player.playername} was dealt {player.hand.card_1.value} of {player.hand.card_1.suit} and {player.hand.card_2.value} of {player.hand.card_2.suit}",
                    DateTime.UtcNow
                );
            }

            // Determine small and big blind positions
            dealerIndex = (dealerIndex + 1) % players.Count;
            int smallBlindIndex = (dealerIndex + 1) % players_round.Count;
            int bigBlindIndex = (dealerIndex + 2) % players_round.Count;

            Player smallBlindPlayer = players_round[smallBlindIndex];
            Player bigBlindPlayer = players_round[bigBlindIndex];

            // Deduct blinds from chips and set initial bets
            smallBlindPlayer.chips -= smallBlind;
            bigBlindPlayer.chips -= bigBlind;
            playerBets[smallBlindPlayer] = smallBlind;
            playerBets[bigBlindPlayer] = bigBlind;
            currentBet = bigBlind;
            pot += smallBlind + bigBlind;

            // Announce blinds in chat
            await _hubContext.Clients.Group(_tableId).SendAsync(
                "ReceiveTableMessage", "System",
                $"{smallBlindPlayer.playername} posts small blind ({smallBlind}), {bigBlindPlayer.playername} posts big blind ({bigBlind})",
                DateTime.UtcNow
            );

            bettingRound = 0;
            actedThisRound.Clear();
            currentPlayerIndex = 0;
            await ProcessBettingAsync(true);

        }

        // Prompts the current player for an action (call/fold/raise)
        public async Task ProcessBettingAsync(bool isPreFlop)
        {
            // End hand if only one player remains
            if (players_round.Count - players_fold.Count == 1)
            {
                var winner = players_round.First(p => !players_fold.Contains(p));
                await _hubContext.Clients.Group(_tableId).SendAsync("ReceiveTableMessage", "System", $"{winner.playername} wins the hand!", DateTime.UtcNow);
                return;
            }

            // End betting round if all non-folded players have acted
            if (actedThisRound.Count >= players_round.Count - players_fold.Count)
            {
                await NextBettingRound();
                return;
            }

            // Find the next player who hasn't folded and hasn't acted
            Player p = null;
            int startIdx = currentPlayerIndex;
            do
            {
                p = players_round[currentPlayerIndex];
                if (!players_fold.Contains(p) && !actedThisRound.Contains(p.playername))
                    break;
                currentPlayerIndex = (currentPlayerIndex + 1) % players_round.Count;
            } while (currentPlayerIndex != startIdx);

            // Notify all clients whose turn it is
            await _hubContext.Clients.Group(_tableId).SendAsync("CurrentPlayer", p.playername);

            // Prompt the current player for their action
            int toCall = currentBet - playerBets[p];
            await _hubContext.Clients.Client(p.ConnectionId)
                .SendAsync("PromptPlayerAction", p.playername, toCall, currentBet);
        }

        // Handles a player's action and advances the betting round
        public async Task HandlePlayerActionAsync(string playerName, string action, int raiseAmount)
        {
            var p = players_round.Find(x => x.playername == playerName);

            // Prevent duplicate folds
            if (action == "fold" && !players_fold.Contains(p))
                players_fold.Add(p);

            // Handle call/raise (very basic demo logic)
            if (action == "call")
            {
                int toCall = currentBet - playerBets[p];
                if (toCall > p.chips) toCall = p.chips; // All-in protection
                p.chips -= toCall;
                pot += toCall;
                playerBets[p] += toCall;
            }
            else if (action == "raise")
            {
                int toCall = currentBet - playerBets[p];
                int totalBet = toCall + raiseAmount;
                if (totalBet > p.chips) totalBet = p.chips; // All-in protection
                p.chips -= totalBet;
                pot += totalBet;
                playerBets[p] += totalBet;
                currentBet += raiseAmount;
            }

            await _hubContext.Clients.Group(_tableId).SendAsync(
                "ReceiveTableMessage",
                "System",
                $"Pot is now {pot} chips.",
                DateTime.UtcNow
            );

            actedThisRound.Add(playerName);

            // Advance to next player
            currentPlayerIndex = (currentPlayerIndex + 1) % players_round.Count;

            await ProcessBettingAsync(bettingRound == 0);
        }

        // Advances to the next betting round or ends the hand
        private async Task NextBettingRound()
        {
            actedThisRound.Clear();
            currentPlayerIndex = 0;
            bettingRound++;

            // Reset player bets for the new round
            foreach (var p in players_round)
                playerBets[p] = 0;
            currentBet = 0;

            if (bettingRound == 1)
            {
                // Deal the flop
                table.flop1 = CollectionExtension.Random<Card>(deck.d);
                table.flop2 = CollectionExtension.Random<Card>(deck.d);
                table.flop3 = CollectionExtension.Random<Card>(deck.d);
                await _hubContext.Clients.Group(_tableId).SendAsync(
                    "ReceiveTableMessage",
                    "System",
                    $"Flop: {table.flop1.value} of {table.flop1.suit}, {table.flop2.value} of {table.flop2.suit}, {table.flop3.value} of {table.flop3.suit}",
                    DateTime.UtcNow
                );
                await ProcessBettingAsync(false);
            }
            else if (bettingRound == 2)
            {
                // Deal the turn
                table.turn = CollectionExtension.Random<Card>(deck.d);
                await _hubContext.Clients.Group(_tableId).SendAsync(
                    "ReceiveTableMessage",
                    "System",
                    $"Turn: {table.turn.value} of {table.turn.suit}",
                    DateTime.UtcNow
                );
                await ProcessBettingAsync(false);
            }
            else if (bettingRound == 3)
            {
                // Deal the river
                table.river = CollectionExtension.Random<Card>(deck.d);
                await _hubContext.Clients.Group(_tableId).SendAsync(
                    "ReceiveTableMessage",
                    "System",
                    $"River: {table.river.value} of {table.river.suit}",
                    DateTime.UtcNow
                );
                await ProcessBettingAsync(false);
            }
            else
            {
                // Showdown: pick a winner among players who have not folded
                var contenders = players_round.Where(p => !players_fold.Contains(p)).ToList();

                foreach (var p in contenders)
                {
                    await _hubContext.Clients.Group(_tableId).SendAsync(
                        "ReceiveTableMessage",
                        "System",
                        $"{p.playername} shows {p.hand.card_1.value} of {p.hand.card_1.suit} and {p.hand.card_2.value} of {p.hand.card_2.suit}",
                        DateTime.UtcNow
                    );
                }

                if (contenders.Count == 1)
                {
                    // Only one player left (should be handled earlier, but just in case)
                    Player winner = contenders[0];
                    players.First(p => p == winner).chips += pot;
                    await _hubContext.Clients.Group(_tableId).SendAsync(
                        "ReceiveTableMessage",
                        "System",
                        $"{winner.playername} wins the pot of {pot} chips!",
                        DateTime.UtcNow
                    );
                }
                else if (contenders.Count > 1)
                {
                    // Use the simple evaluator for now
                    List<Player> winners = EvaluateBestHand(contenders);
                    if (winners.Count == 1) {
                        players.First(p => p == winners[0]).chips += pot; 

                        await _hubContext.Clients.Group(_tableId).SendAsync(
                            "ReceiveTableMessage",
                            "System",
                            $"Showdown! {winners[0].playername} wins the pot of {pot} chips.",
                            DateTime.UtcNow
                        );
                    }
                    else
                    {
                        int split = pot / winners.Count;
                        foreach (Player winner in winners)
                        {
                            players.Find(p => p == winner).chips += split;
                        }
                        String winners_text = string.Join(", ", winners.Select(p => p.playername));
                        await _hubContext.Clients.Group(_tableId).SendAsync(
                            "ReceiveTableMessage",
                            "System",
                            $"Showdown! It's a tie between: {winners_text}. Ther pot have been split equally between them, so they each get {split} chips",
                            DateTime.UtcNow
                        );
                    }
                }
                else
                {
                    await _hubContext.Clients.Group(_tableId).SendAsync(
                        "ReceiveTableMessage",
                        "System",
                        "No winner could be determined.",
                        DateTime.UtcNow
                    );
                }

                foreach (Player p in players)
                {
                    await _hubContext.Clients.Group(_tableId).SendAsync(
                            "ReceiveTableMessage",
                            "System",
                            $"{p.playername} has {p.chips} left.",
                            DateTime.UtcNow
                        );
                }
                // Reset pot for next hand
                pot = 0;

                await PlayRoundAsync();
            }
        }

        private List<Player> EvaluateBestHand(List<Player> contenders)
        {
            // For each player, find their highest card (hole + community)
            Player bestPlayer = null;
            int bestRank = -1;

            // Gather all community cards
            var community = new List<Card>();
            if (table.flop1 != null) community.Add(table.flop1);
            if (table.flop2 != null) community.Add(table.flop2);
            if (table.flop3 != null) community.Add(table.flop3);
            if (table.turn != null) community.Add(table.turn);
            if (table.river != null) community.Add(table.river);

            foreach (var player in contenders)
            {
                var allCards = new List<Card> { player.hand.card_1, player.hand.card_2 };
                allCards.AddRange(community);

                // Find the highest card rank
                player.handtype = PokerHandEvaluator.EvaluateHand(allCards);
                player.besthand = PokerHandEvaluator.GetBestFiveCardHand(allCards);

            }

            return PokerHandComparer.ComparePlayers(contenders);
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
                var rank = player.handtype;
                var hand = player.besthand;

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