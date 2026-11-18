// Rama's own resources an effect may change: drawing cards and gaining Prana (the Arrow of Surya). Kept apart from
// ICombatActions so enemy moves, previews and test doubles that never touch a deck or Prana do not have to implement
// it; effects check for it on the actions they get.
namespace JourneyToLanka.Combat.Effects
{
    public interface ICardActions
    {
        int DrawCards(int count);

        void GainPrana(int amount);
    }
}
