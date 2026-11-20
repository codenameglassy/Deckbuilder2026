// A keyword ready to decorate text with: the word in the current language and the rich-text tag that opens its colour.
// Built by CardKeywordSet whenever a word or colour changes, read by CardKeywordDecorator.
namespace JourneyToLanka.CombatUI
{
    public readonly struct CardKeyword
    {
        public readonly string Word;
        public readonly string OpenTag;

        public CardKeyword(string word, string openTag)
        {
            Word = word;
            OpenTag = openTag;
        }
    }
}
