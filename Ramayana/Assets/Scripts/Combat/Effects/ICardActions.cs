// Card-pile actions an effect may perform, such as drawing. Kept apart from ICombatActions so enemy moves and
// test doubles that never touch a deck do not have to implement it; effects check for it on the actions they get.
namespace JourneyToLanka.Combat.Effects
{
    public interface ICardActions
    {
        int DrawCards(int count);
    }
}
