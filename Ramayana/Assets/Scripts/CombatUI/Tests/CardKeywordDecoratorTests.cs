// EditMode tests for CardKeywordDecorator: a keyword is wrapped in its colour tag; only whole words match, and only
// with the same case; placeholders and existing rich-text tags are copied untouched and never matched inside, while
// escaped braces stay escaped and the literal text between them is ordinary text; the first matching keyword wins, so a
// longer keyword listed first beats its prefix; combining marks count as part of a word; and empty input or no keywords
// leave the pattern as it is. The decorated pattern still formats with CardTextFormatter.
using System.Collections.Generic;
using System.Text;
using JourneyToLanka.Combat.Effects;
using NUnit.Framework;

namespace JourneyToLanka.CombatUI.Tests
{
    public sealed class CardKeywordDecoratorTests
    {
        private const string Blue = "<color=#0000FFFF>";
        private const string Red = "<color=#FF0000FF>";
        private const string Close = CardKeywordDecorator.CloseTag;

        private readonly StringBuilder _output = new StringBuilder();

        private string Decorate(string pattern, params CardKeyword[] keywords)
        {
            CardKeywordDecorator.Decorate(pattern, keywords, _output);
            return _output.ToString();
        }

        [Test]
        public void Keyword_IsWrappedInItsColour()
        {
            string result = Decorate("Gain {0} Kavach.", new CardKeyword("Kavach", Blue));

            Assert.AreEqual("Gain {0} " + Blue + "Kavach" + Close + ".", result);
        }

        [Test]
        public void EveryOccurrence_IsWrapped()
        {
            string result = Decorate("Kavach, then Kavach", new CardKeyword("Kavach", Blue));

            Assert.AreEqual(Blue + "Kavach" + Close + ", then " + Blue + "Kavach" + Close, result);
        }

        [Test]
        public void PartOfALongerWord_IsNotWrapped()
        {
            var keyword = new CardKeyword("Kavach", Blue);

            Assert.AreEqual("Kavaches and SuperKavach", Decorate("Kavaches and SuperKavach", keyword));
        }

        [Test]
        public void DifferentCase_IsNotWrapped()
        {
            Assert.AreEqual("kavach", Decorate("kavach", new CardKeyword("Kavach", Blue)));
        }

        [Test]
        public void Placeholders_AreUntouched_AndEscapedBracesStayEscaped()
        {
            var keyword = new CardKeyword("Focus", Red);

            Assert.AreEqual("{Focus} {0}", Decorate("{Focus} {0}", keyword));
            Assert.AreEqual("{{" + Red + "Focus" + Close + "}}", Decorate("{{Focus}}", keyword));
        }

        [Test]
        public void ExistingTags_AreNeverMatchedInside()
        {
            string result = Decorate("<link=Bhaya>x</link> Bhaya", new CardKeyword("Bhaya", Red));

            Assert.AreEqual("<link=Bhaya>x</link> " + Red + "Bhaya" + Close, result);
        }

        [Test]
        public void FirstMatchingKeyword_Wins()
        {
            string result = Decorate("Prana Fire", new CardKeyword("Prana Fire", Red), new CardKeyword("Prana", Blue));

            Assert.AreEqual(Red + "Prana Fire" + Close, result);
        }

        [Test]
        public void CombiningMark_KeepsTheWordTogether()
        {
            const string Word = "\u0915\u0935\u091A\u093E";
            var keyword = new CardKeyword("\u0915\u0935\u091A", Blue);

            Assert.AreEqual(Word, Decorate(Word, keyword));
        }

        [Test]
        public void EmptyInput_OrNoKeywords_LeaveThePattern()
        {
            Assert.AreEqual(string.Empty, Decorate(string.Empty, new CardKeyword("Kavach", Blue)));
            CardKeywordDecorator.Decorate("Gain Kavach", null, _output);
            Assert.AreEqual("Gain Kavach", _output.ToString());
        }

        [Test]
        public void DecoratedPattern_StillFormats()
        {
            string decorated = Decorate("Gain {0} Kavach.", new CardKeyword("Kavach", Blue));
            var values = new List<CardValue> { new CardValue(5, 5) };
            var formatted = new StringBuilder();

            CardTextFormatter.Format(decorated, values, null, null, formatted);

            Assert.AreEqual("Gain 5 " + Blue + "Kavach" + Close + ".", formatted.ToString());
        }
    }
}
