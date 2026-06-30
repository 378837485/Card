using System;

[Serializable]
public struct Card
{
    [Serializable]
    public enum Suit { c, h, s, d }

    [Serializable]
    public enum Rank
    {
        Ace = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13
    }

    public Suit suit;
    public Rank rank;

    public Card(Suit s, Rank r)
    {
        suit = s;
        rank = r;
    }

    public override string ToString()
    {
        return $"{suit}" + (int)rank;
    }
}