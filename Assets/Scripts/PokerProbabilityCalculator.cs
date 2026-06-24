using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum HandType
{
    HighCard, //高牌
    OnePair, //一对
    TwoPair, //两对
    ThreeOfAKind, //三条
    Straight, //顺子
    Flush, //同花
    FullHouse, //葫芦
    FourOfAKind, //四条
    StraightFlush //同花顺
}

public static class PokerProbabilityCalculator
{
    // 只关心你指定的7种强牌型
    private static readonly HashSet<HandType> TargetTypes = new HashSet<HandType>
    {
        HandType.TwoPair, HandType.ThreeOfAKind, HandType.Straight,
        HandType.Flush, HandType.FullHouse, HandType.FourOfAKind, HandType.StraightFlush
    };

    /// <summary>
    /// 计算最终能组成指定强牌型的概率
    /// </summary>
    public static Dictionary<HandType, float> GetFinalHandProbabilities(
        List<Card> playerHand,
        List<Card> communityCards)
    {
        // ---- 1. 构建剩余牌堆 ----
        Card[] allCards = GetAllCardsArray();
        bool[] used = new bool[52];
        MarkUsed(used, playerHand);
        MarkUsed(used, communityCards);

        Card[] remaining = new Card[52];
        int remainCount = 0;
        for (int i = 0; i < allCards.Length; i++)
        {
            if (!used[i])
                remaining[remainCount++] = allCards[i];
        }

        int slotsToFill = 5 - communityCards.Count;
        int totalCombos = 0;
        int[] hitCounts = new int[9]; // 索引对应枚举值

        Card[] finalHand = new Card[7];
        finalHand[0] = playerHand[0];
        finalHand[1] = playerHand[1];
        for (int i = 0; i < communityCards.Count; i++)
            finalHand[2 + i] = communityCards[i];

        // ---- 2. 根据剩余牌数量选择枚举方式 ----
        if (slotsToFill == 2) // 最常见：翻牌圈
        {
            for (int i = 0; i < remainCount; i++)
            {
                finalHand[2 + communityCards.Count] = remaining[i];
                for (int j = i + 1; j < remainCount; j++)
                {
                    finalHand[3 + communityCards.Count] = remaining[j];
                    HandType type = EvaluateBestHand(finalHand);
                    if ((int)type >= 2 && (int)type <= 8)
                        hitCounts[(int)type]++;
                    totalCombos++;
                }
            }
        }
        else if (slotsToFill == 5) // 翻牌前
        {
            int[] indices = new int[5];
            GenerateCombos(remaining, remainCount, finalHand, communityCards.Count,
                           0, 0, indices, hitCounts, ref totalCombos);
        }
        else if (slotsToFill == 1) // 河牌前
        {
            for (int i = 0; i < remainCount; i++)
            {
                finalHand[2 + communityCards.Count] = remaining[i];
                HandType type = EvaluateBestHand(finalHand);
                if ((int)type >= 2 && (int)type <= 8) hitCounts[(int)type]++;
                totalCombos++;
            }
        }
        else // 公共牌已满
        {
            HandType type = EvaluateBestHand(finalHand);
            if ((int)type >= 2 && (int)type <= 8) hitCounts[(int)type] = 1;
            totalCombos = 1;
        }

        // ---- 3. 返回结果 ----
        var result = new Dictionary<HandType, float>();
        float total = totalCombos;
        foreach (var type in TargetTypes)
        {
            result[type] = hitCounts[(int)type] / total;
        }
        return result;
    }

    // 递归组合生成
    private static void GenerateCombos(Card[] remaining, int remainCount, Card[] finalHand,
                                       int fixedCount, int start, int depth, int[] indices,
                                       int[] hitCounts, ref int totalCombos)
    {
        if (depth == 5)
        {
            for (int i = 0; i < 5; i++)
                finalHand[2 + fixedCount + i] = remaining[indices[i]];

            HandType type = EvaluateBestHand(finalHand);
            if ((int)type >= 2 && (int)type <= 8) hitCounts[(int)type]++;
            totalCombos++;
            return;
        }

        for (int i = start; i < remainCount; i++)
        {
            indices[depth] = i;
            GenerateCombos(remaining, remainCount, finalHand, fixedCount,
                           i + 1, depth + 1, indices, hitCounts, ref totalCombos);
        }
    }

    // ---- 辅助方法 ----
    private static void MarkUsed(bool[] used, List<Card> cards)
    {
        foreach (var c in cards)
        {
            int index = (int)c.suit * 13 + ((int)c.rank - 1);
            used[index] = true;
        }
    }

    private static Card[] GetAllCardsArray()
    {
        Card[] deck = new Card[52];
        int idx = 0;
        foreach (Card.Suit suit in Enum.GetValues(typeof(Card.Suit)))
            foreach (Card.Rank rank in Enum.GetValues(typeof(Card.Rank)))
                deck[idx++] = new Card(suit, rank);
        return deck;
    }

    // ---- 核心：7张牌选最佳5张（完全兼容Unity，无Span） ----
    public static HandType EvaluateBestHand(Card[] seven)
    {
        HandType best = HandType.HighCard;
        // 枚举 C(7,5)=21 种组合
        for (int a = 0; a < 3; a++)
            for (int b = a + 1; b < 4; b++)
                for (int c = b + 1; c < 5; c++)
                    for (int d = c + 1; d < 6; d++)
                        for (int e = d + 1; e < 7; e++)
                        {
                            Card[] five = new Card[5] { seven[a], seven[b], seven[c], seven[d], seven[e] };
                            HandType current = EvaluateFiveCards(five);
                            if (current > best) best = current;
                        }
        return best;
    }

    // ---- 评估5张牌（修复版） ----
    private static HandType EvaluateFiveCards(Card[] five)
    {
        // 1. 按点数排序
        int[] ranks = new int[5];
        for (int i = 0; i < 5; i++) ranks[i] = (int)five[i].rank;
        Array.Sort(ranks);

        // 2. 检查同花
        bool isFlush = true;
        for (int i = 1; i < 5; i++)
            if (five[i].suit != five[0].suit) { isFlush = false; break; }

        // 3. 检查顺子
        bool isStraight = false;
        bool distinct = true;
        for (int i = 1; i < 5; i++) if (ranks[i] == ranks[i - 1]) { distinct = false; break; }
        if (distinct && ranks[4] - ranks[0] == 4) isStraight = true;
        // 特殊：A2345 (A=14, 但这里A=1? 注意：我们的Rank枚举中Ace=1, 但排序后Ace=1, 所以A2345的ranks是[1,2,3,4,5]? 不对，因为Ace=1，如果顺子是A2345，那排序后为[1,2,3,4,5]，但此时ranks[4]-ranks[0]=4，所以条件成立。但对于10JQKA，Ace=14，但我们的Ace=1，所以10JQKA的ranks是[10,11,12,13,1]，排序后为[1,10,11,12,13]，这样不满足差值4，所以需要特殊处理。我们必须在顺子检查中处理Ace的双重角色。标准做法：如果包含Ace（1），单独检查[1,2,3,4,5]或[10,11,12,13,1]。
        // 但因为我们枚举值Ace=1，10=10, Jack=11, Queen=12, King=13，所以顺子10-J-Q-K-A的ranks是[10,11,12,13,1]，排序后是[1,10,11,12,13]。
        // 我们需要额外检查这个情况。
        // 简单方法：如果检查到Ace且其它四张是10,11,12,13，则算是顺子。
        // 更通用：我们可以在排序后，如果包含Ace(1)，我们把Ace当作14，再检查一次。
        // 重写顺子检查：

        if (distinct)
        {
            // 常规顺子（从最小到最大连续）
            if (ranks[4] - ranks[0] == 4)
                isStraight = true;
            // 特殊：A,2,3,4,5  (ranks为[1,2,3,4,5])
            if (ranks[0] == 1 && ranks[1] == 2 && ranks[2] == 3 && ranks[3] == 4 && ranks[4] == 5)
                isStraight = true;
            // 特殊：10,J,Q,K,A (ranks为[1,10,11,12,13])
            if (ranks[0] == 1 && ranks[1] == 10 && ranks[2] == 11 && ranks[3] == 12 && ranks[4] == 13)
                isStraight = true;
        }

        // 4. 统计点数频率
        int[] counts = new int[14]; // 索引1~13
        for (int i = 0; i < 5; i++) counts[(int)five[i].rank]++;

        bool hasFour = false, hasThree = false, hasTwo = false;
        int pairCount = 0;
        for (int i = 1; i <= 13; i++)
        {
            if (counts[i] == 4) hasFour = true;
            if (counts[i] == 3) hasThree = true;
            if (counts[i] == 2) { pairCount++; hasTwo = true; }
        }

        // 5. 按优先级返回
        if (isFlush && isStraight) return HandType.StraightFlush;
        if (hasFour) return HandType.FourOfAKind;
        if (hasThree && hasTwo) return HandType.FullHouse;
        if (isFlush) return HandType.Flush;
        if (isStraight) return HandType.Straight;
        if (hasThree) return HandType.ThreeOfAKind;
        if (pairCount == 2) return HandType.TwoPair;
        if (hasTwo) return HandType.OnePair;
        return HandType.HighCard;
    }
}