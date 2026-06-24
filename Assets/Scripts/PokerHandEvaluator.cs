using System;
using System.Collections.Generic;
using System.Linq;

public static class PokerHandEvaluator
{
    /// <summary>
    /// 传入每个玩家的手牌（2张）和公共牌（5张），返回赢家的索引列表（可能有多个，平局）。
    /// </summary>
    public static List<int> DetermineWinners(List<List<Card>> playerHands, List<Card> communityCards)
    {
        if (communityCards.Count != 5)
            throw new ArgumentException("公共牌必须恰好5张");

        // 存储每个玩家的最佳5张牌组合
        List<Card[]> bestHands = new List<Card[]>();
        foreach (var hand in playerHands)
        {
            if (hand.Count != 2)
                throw new ArgumentException("每个玩家手牌必须恰好2张");

            // 合并7张牌
            Card[] seven = new Card[7];
            seven[0] = hand[0];
            seven[1] = hand[1];
            for (int i = 0; i < 5; i++)
                seven[2 + i] = communityCards[i];

            // 选出最佳5张
            Card[] best = GetBestFiveCards(seven);
            bestHands.Add(best);
        }

        // 找出最强的手牌
        int bestIndex = 0;
        List<int> winners = new List<int> { 0 };
        for (int i = 1; i < bestHands.Count; i++)
        {
            int cmp = CompareFiveCardHands(bestHands[i], bestHands[bestIndex]);
            if (cmp > 0) // 当前玩家更强
            {
                bestIndex = i;
                winners.Clear();
                winners.Add(i);
            }
            else if (cmp == 0) // 平局
            {
                winners.Add(i);
            }
        }
        return winners;
    }

    // ---------- 从7张牌中选出最佳5张 ----------
    private static Card[] GetBestFiveCards(Card[] seven)
    {
        Card[] best = null;
        HandStrength bestStrength = null;

        // 枚举所有 C(7,5)=21 种组合
        for (int a = 0; a < 3; a++)
            for (int b = a + 1; b < 4; b++)
                for (int c = b + 1; c < 5; c++)
                    for (int d = c + 1; d < 6; d++)
                        for (int e = d + 1; e < 7; e++)
                        {
                            Card[] five = new Card[5] { seven[a], seven[b], seven[c], seven[d], seven[e] };
                            HandStrength strength = GetHandStrength(five);
                            if (bestStrength == null || CompareStrength(strength, bestStrength) > 0)
                            {
                                bestStrength = strength;
                                best = five;
                            }
                        }
        return best;
    }

    // ---------- 比较两个5张牌组合（返回1,0,-1） ----------
    private static int CompareFiveCardHands(Card[] handA, Card[] handB)
    {
        var sa = GetHandStrength(handA);
        var sb = GetHandStrength(handB);
        return CompareStrength(sa, sb);
    }

    // ---------- 手牌强度数据结构 ----------
    private class HandStrength
    {
        public HandType type;
        public int[] tieBreakers; // 从最重要到次重要排列的点数（A=14）

        public HandStrength(HandType type, int[] breakers)
        {
            this.type = type;
            tieBreakers = breakers;
        }
    }

    // ---------- 比较两个强度 ----------
    private static int CompareStrength(HandStrength a, HandStrength b)
    {
        if (a.type != b.type)
            return ((int)a.type).CompareTo((int)b.type);

        // 同类型，比较踢脚数组
        for (int i = 0; i < Math.Min(a.tieBreakers.Length, b.tieBreakers.Length); i++)
        {
            if (a.tieBreakers[i] != b.tieBreakers[i])
                return a.tieBreakers[i].CompareTo(b.tieBreakers[i]);
        }
        return 0; // 完全相等
    }

    // ---------- 获取5张牌的强度（类型+踢脚点数） ----------
    private static HandStrength GetHandStrength(Card[] five)
    {
        // 1. 获取点数列表（用14代表Ace，用于比较）
        int[] values = five.Select(c => GetCardValue(c.rank)).ToArray();
        Array.Sort(values); // 从小到大排序

        // 2. 检查同花
        bool isFlush = five.All(c => c.suit == five[0].suit);

        // 3. 检查顺子（注意Ace的特殊处理）
        bool isStraight = false;
        bool distinct = values.Distinct().Count() == 5;
        if (distinct)
        {
            // 常规顺子
            if (values[4] - values[0] == 4)
                isStraight = true;
            // 特殊 A-2-3-4-5  (此时values为[2,3,4,5,14] 但因为Ace=14，排序后是[2,3,4,5,14])
            // 检测：如果 values[4]==14 且 values[0]==2 且 values[1]==3 && values[2]==4 && values[3]==5
            if (values[0] == 2 && values[1] == 3 && values[2] == 4 && values[3] == 5 && values[4] == 14)
                isStraight = true;
        }

        // 4. 统计点数频率
        var groups = five.GroupBy(c => GetCardValue(c.rank))
                         .OrderByDescending(g => g.Count())
                         .ThenByDescending(g => g.Key)
                         .ToList();

        int[] counts = groups.Select(g => g.Count()).ToArray();
        int[] rankValues = groups.Select(g => g.Key).ToArray(); // 从高频到低频

        // 5. 判断牌型并构建踢脚数组
        HandType type;
        int[] breakers;

        if (isFlush && isStraight)
        {
            type = HandType.StraightFlush;
            // 顺子最大牌（如果是A-2-3-4-5，最大牌是5）
            int high = (values[4] == 14 && values[0] == 2) ? 5 : values[4];
            breakers = new int[] { high };
        }
        else if (counts[0] == 4)
        {
            type = HandType.FourOfAKind;
            // 四条点数 + 踢脚
            int fourRank = rankValues[0];
            int kicker = rankValues[1]; // 另一张牌
            breakers = new int[] { fourRank, kicker };
        }
        else if (counts[0] == 3 && counts.Length > 1 && counts[1] == 2)
        {
            type = HandType.FullHouse;
            int threeRank = rankValues[0];
            int pairRank = rankValues[1];
            breakers = new int[] { threeRank, pairRank };
        }
        else if (isFlush)
        {
            type = HandType.Flush;
            // 所有牌从大到小排列（降序）
            breakers = values.Reverse().ToArray();
        }
        else if (isStraight)
        {
            type = HandType.Straight;
            int high = (values[4] == 14 && values[0] == 2) ? 5 : values[4];
            breakers = new int[] { high };
        }
        else if (counts[0] == 3)
        {
            type = HandType.ThreeOfAKind;
            int threeRank = rankValues[0];
            // 两个踢脚（从大到小）
            int[] kickers = rankValues.Skip(1).OrderByDescending(x => x).ToArray();
            breakers = new int[] { threeRank }.Concat(kickers).ToArray();
        }
        else if (counts[0] == 2 && counts.Length >= 2 && counts[1] == 2)
        {
            type = HandType.TwoPair;
            int highPair = rankValues[0];
            int lowPair = rankValues[1];
            int kicker = rankValues[2]; // 剩下的一张
            breakers = new int[] { highPair, lowPair, kicker };
        }
        else if (counts[0] == 2)
        {
            type = HandType.OnePair;
            int pairRank = rankValues[0];
            // 三个踢脚（从大到小）
            int[] kickers = rankValues.Skip(1).OrderByDescending(x => x).ToArray();
            breakers = new int[] { pairRank }.Concat(kickers).ToArray();
        }
        else
        {
            type = HandType.HighCard;
            // 所有牌从大到小
            breakers = values.Reverse().ToArray();
        }

        return new HandStrength(type, breakers);
    }

    // ---------- 辅助：获取点数的比较值（Ace=14） ----------
    private static int GetCardValue(Card.Rank rank)
    {
        if (rank == Card.Rank.Ace)
            return 14;
        return (int)rank; // 2..13
    }
}