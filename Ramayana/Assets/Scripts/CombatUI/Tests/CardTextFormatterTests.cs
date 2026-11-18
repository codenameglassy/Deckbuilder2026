// EditMode tests for CardTextFormatter: placeholders become current values, buffed and weakened values get their
// colour tags while unchanged ones stay plain, escaped braces, and every malformed or out-of-range placeholder is
// written as it stands instead of throwing.
using System.Collections.Generic;
using System.Text;
using JourneyToLanka.Combat.Effects;
using NUnit.Framework;

namespace JourneyToLanka.CombatUI.Tests
{
    public sealed class CardTextFormatterTests
    {
        private const string Buffed = "<color=#00FF00FF>";
        private const string Weakened = "<color=#FF0000FF>";

        private readonly StringBuilder _output = new StringBuilder();

        private string Format(string pattern, params CardValue[] values)
        {
            CardTextFormatter.Format(pattern, values, Buffed, Weakened, _output);
            return _output.ToString();
        }

        [Test]
        public void Placeholders_BecomeCurrentValues_InAnyOrder()
        {
            Assert.AreEqual("Deal 3 damage 4 times.", Format("Deal {0} damage {1} times.", CardValue.Fixed(3), CardValue.Fixed(4)));
            Assert.AreEqual("4 then 3", Format("{1} then {0}", CardValue.Fixed(3), CardValue.Fixed(4)));
        }

        [Test]
        public void BuffedAndWeakenedValues_AreColoured_UnchangedValuesArePlain()
        {
            Assert.AreEqual("Deal <color=#00FF00FF>9</color> damage.", Format("Deal {0} damage.", new CardValue(6, 9)));
            Assert.AreEqual("Deal <color=#FF0000FF>4</color> damage.", Format("Deal {0} damage.", new CardValue(6, 4)));
            Assert.AreEqual("Deal 6 damage.", Format("Deal {0} damage.", new CardValue(6, 6)));
        }

        [Test]
        public void MissingColourTags_FallBackToPlainNumbers()
        {
            var values = new List<CardValue> { new CardValue(6, 9) };

            CardTextFormatter.Format("Deal {0}.", values, null, string.Empty, _output);

            Assert.AreEqual("Deal 9.", _output.ToString());
        }

        [Test]
        public void DoubledBraces_WriteLiteralBraces()
        {
            Assert.AreEqual("{0} is 5 }", Format("{{0}} is {0} }}", CardValue.Fixed(5)));
        }

        [Test]
        public void MalformedOrOutOfRangePlaceholders_AreWrittenAsTheyStand()
        {
            Assert.AreEqual("Deal {1} damage.", Format("Deal {1} damage.", CardValue.Fixed(5)));
            Assert.AreEqual("{x} {} {0", Format("{x} {} {0", CardValue.Fixed(5)));
            Assert.AreEqual("{1234}", Format("{1234}", CardValue.Fixed(5)));
            Assert.AreEqual("Lone } brace", Format("Lone } brace"));
        }

        [Test]
        public void EmptyPattern_ClearsTheOutput()
        {
            _output.Append("stale");

            CardTextFormatter.Format(null, null, Buffed, Weakened, _output);

            Assert.AreEqual(string.Empty, _output.ToString());
        }

        [Test]
        public void NullValues_LeavePlaceholdersAsTheyStand()
        {
            CardTextFormatter.Format("Draw {0}.", null, Buffed, Weakened, _output);

            Assert.AreEqual("Draw {0}.", _output.ToString());
        }
    }
}
