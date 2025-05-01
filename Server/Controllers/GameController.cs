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
        private static Random random = new Random();

        public static T Random<T>(this IList<T> list)
        {
            int index = random.Next(list.Count);
            T temp = list[index];
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
        public List<Card> deck = new();

        public Deck()
        {
            // Initialize a standard 52-card deck
            string[] suits = { "Hearts", "Diamonds", "Clubs", "Spades" };
            string[] values = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            foreach (var suit in suits)
                foreach (var value in values)
                    deck.Add(new Card { suit = suit, value = value });
        }

        public Card RandomCard()
        {
            int index = new Random().Next(deck.Count);
            Card temp = deck[index];
            deck.RemoveAt(index);
            return temp;
        }
    }

    // Represents a player in the game
    public class Player
    {
        public string playername;
        public string ConnectionId;
        public Hand hand;
        public int chips = 1000; // Starting chips
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

            foreach (var p in players_round)
            {
                p.hand = new Hand
                {
                    card_1 = deck.RandomCard(),
                    card_2 = deck.RandomCard()
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
                table.flop1 = deck.RandomCard();
                table.flop2 = deck.RandomCard();
                table.flop3 = deck.RandomCard();
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
                table.turn = deck.RandomCard();
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
                table.river = deck.RandomCard();
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
                    var winner = contenders[0];
                    winner.chips += pot;
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
                    var winner = EvaluateBestHand(contenders);
                    winner.chips += pot;
                    await _hubContext.Clients.Group(_tableId).SendAsync(
                        "ReceiveTableMessage",
                        "System",
                        $"Showdown! {winner.playername} wins the pot of {pot} chips with the highest card! (Simple evaluation)",
                        DateTime.UtcNow
                    );
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

                // Reset pot for next hand
                pot = 0;
            }
        }

        private Player EvaluateBestHand(List<Player> contenders)
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

                // Find the highest card value for this player
                int max = allCards.Max(card => CardRank[card.value]);
                if (max > bestRank)
                {
                    bestRank = max;
                    bestPlayer = player;
                }
            }
            return bestPlayer;
        }
    }
}