using System;

// 扑克牌数据结构
public struct Card
{
    public enum Suit { 黑桃, 红桃, 梅花, 方块 }

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
        return $"{rank} of {suit}";
    }
}