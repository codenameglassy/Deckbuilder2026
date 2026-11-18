// How rare a card is, as in the reference build: reward rolls pick Common, Rare or Epic cards by weight, and Divine
// cards (Brahmastra and the other special astras) are never rolled. None marks a card whose rarity has not been set;
// a rarity roll never matches it, so such a card is offered only when the rolled rarity has no cards left.
namespace JourneyToLanka.Combat.Cards
{
    public enum CardRarity
    {
        None = 0,
        Common = 1,
        Rare = 2,
        Epic = 3,
        Divine = 4,
    }
}
