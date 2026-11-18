// Everything a fight needs to know about Rama: name key, maximum and current health, the deck as a flat card list,
// and his quiver of special arrows. A run builds it from its own state so health, new cards and arrows carry between
// fights (the fight fires from the run's own quiver); FromDefinition builds a fresh one from the player definition
// for standalone fights and tests. A setup with no quiver fights with an empty one.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Combat.Cards;

namespace JourneyToLanka.Combat.Model
{
    public readonly struct PlayerSetup
    {
        public PlayerSetup(string nameKey, int maxHealth, int health, IReadOnlyList<CardDefinitionSO> deck)
            : this(nameKey, maxHealth, health, deck, null)
        {
        }

        public PlayerSetup(string nameKey, int maxHealth, int health, IReadOnlyList<CardDefinitionSO> deck, Quiver quiver)
        {
            NameKey = nameKey;
            MaxHealth = maxHealth;
            Health = health;
            Deck = deck;
            Quiver = quiver;
        }

        public string NameKey { get; }

        public int MaxHealth { get; }

        public int Health { get; }

        public IReadOnlyList<CardDefinitionSO> Deck { get; }

        public Quiver Quiver { get; }

        public static PlayerSetup FromDefinition(PlayerDefinitionSO player, int health)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (player.StartingDeck == null)
            {
                throw new ArgumentException($"{player.name} has no starting deck.", nameof(player));
            }

            var deck = new List<CardDefinitionSO>(player.StartingDeck.TotalCount);
            ExpandDeck(player.StartingDeck, deck);
            return new PlayerSetup(player.NameKey, player.MaxHealth, health, deck, player.CreateQuiver());
        }

        public static void ExpandDeck(StartingDeckSO deck, List<CardDefinitionSO> results)
        {
            if (deck == null)
            {
                throw new ArgumentNullException(nameof(deck));
            }

            var entries = deck.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                DeckEntry entry = entries[i];
                if (entry.Card == null)
                {
                    continue;
                }

                for (int copy = 0; copy < entry.Count; copy++)
                {
                    results.Add(entry.Card);
                }
            }
        }
    }
}
