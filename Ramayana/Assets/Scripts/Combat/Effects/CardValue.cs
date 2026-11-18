// One number shown in a card's description: the value authored on the effect (Base) and the value it has right now
// after every modifier (Current), so card text can show the live number and mark it buffed or weakened. Effects
// append their values in order and description patterns refer to them by position ({0}, {1}, ...). Plain data.
namespace JourneyToLanka.Combat.Effects
{
    public readonly struct CardValue
    {
        public CardValue(int baseValue, int current)
        {
            Base = baseValue;
            Current = current;
        }

        public int Base { get; }

        public int Current { get; }

        public bool IsBuffed => Current > Base;

        public bool IsWeakened => Current < Base;

        public static CardValue Fixed(int value)
        {
            return new CardValue(value, value);
        }
    }
}
