using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 牌型枚举
/// </summary>
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

/// <summary>
/// 辅助数据结构
/// </summary>
public class PlayerOdds
{
    public int playerIndex;
    public float winRate;
    public float tieRate;
    public float loseRate;
    public List<Card[]> winningBoards; // 赢牌时的公共牌组合（最多存储指定数量）

    public override string ToString()
    {
        int count = winningBoards?.Count ?? 0;
        return $"玩家{playerIndex + 1}: 胜{winRate:P2} 平{tieRate:P2} 负{loseRate:P2} (展示前{count}种赢牌组合)";
    }
}

// ====================== 主评估器 ======================
public static class PokerHandEvaluator
{
    // ---------- 1. 从7张牌中选出最佳5张 ----------
    public static Card[] GetBestFiveCards(Card[] seven)
    {
        Card[] best = null;
        HandStrength bestStrength = null;

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

    /// <summary>
    /// 判定赢家（支持平局）
    /// </summary>
    /// <param name="playerHands">所有玩家手牌组</param>
    /// <param name="communityCards">公共池牌组</param>
    /// <returns>返回赢家玩家index</returns>
    /// <exception cref="ArgumentException"></exception>
    public static List<int> DetermineWinners(List<List<Card>> playerHands, List<Card> communityCards)
    {
        if (communityCards.Count != 5)
            throw new ArgumentException("公共牌必须恰好5张");

        List<Card[]> bestHands = new List<Card[]>();
        foreach (var hand in playerHands)
        {
            if (hand.Count != 2)
                throw new ArgumentException("每手牌必须恰好2张");

            Card[] seven = new Card[7];
            seven[0] = hand[0];
            seven[1] = hand[1];
            for (int i = 0; i < 5; i++)
                seven[2 + i] = communityCards[i];

            bestHands.Add(GetBestFiveCards(seven));
        }

        int bestIdx = 0;
        List<int> winners = new List<int> { 0 };
        for (int i = 1; i < bestHands.Count; i++)
        {
            int cmp = CompareFiveCardHands(bestHands[i], bestHands[bestIdx]);
            if (cmp > 0)
            {
                bestIdx = i;
                winners.Clear();
                winners.Add(i);
            }
            else if (cmp == 0)
            {
                winners.Add(i);
            }
        }
        return winners;
    }

    /// <summary>
    /// 最终组成指定强牌型概率
    /// </summary>
    /// <param name="playerHand">玩家手牌</param>
    /// <param name="communityCards">公共池牌组</param>
    /// <returns></returns>
    public static Dictionary<HandType, float> GetFinalHandProbabilities(
        List<Card> playerHand,
        List<Card> communityCards)
    {
        HashSet<HandType> targetTypes = new HashSet<HandType>
        {
            HandType.TwoPair, HandType.ThreeOfAKind, HandType.Straight,
            HandType.Flush, HandType.FullHouse, HandType.FourOfAKind, HandType.StraightFlush
        };

        Card[] allCards = GetAllCardsArray();
        bool[] used = new bool[52];
        MarkUsed(used, playerHand);
        MarkUsed(used, communityCards);

        List<Card> remaining = new List<Card>();
        for (int i = 0; i < allCards.Length; i++)
            if (!used[i]) remaining.Add(allCards[i]);

        int slots = 5 - communityCards.Count;
        int total = 0;
        int[] counts = new int[9];

        Card[] finalSeven = new Card[7];
        finalSeven[0] = playerHand[0];
        finalSeven[1] = playerHand[1];
        for (int i = 0; i < communityCards.Count; i++)
            finalSeven[2 + i] = communityCards[i];

        if (slots == 0)
        {
            HandType t = EvaluateBestHand(finalSeven);
            if (targetTypes.Contains(t)) counts[(int)t] = 1;
            total = 1;
        }
        else if (slots == 1)
        {
            foreach (var c in remaining)
            {
                finalSeven[2 + communityCards.Count] = c;
                HandType t = EvaluateBestHand(finalSeven);
                if (targetTypes.Contains(t)) counts[(int)t]++;
                total++;
            }
        }
        else if (slots == 2)
        {
            for (int i = 0; i < remaining.Count; i++)
            {
                finalSeven[2 + communityCards.Count] = remaining[i];
                for (int j = i + 1; j < remaining.Count; j++)
                {
                    finalSeven[3 + communityCards.Count] = remaining[j];
                    HandType t = EvaluateBestHand(finalSeven);
                    if (targetTypes.Contains(t)) counts[(int)t]++;
                    total++;
                }
            }
        }
        else // slots == 5 (翻牌前)
        {
            int[] indices = new int[5];
            GenerateCombos(remaining, slots, 0, 0, indices, (combo) =>
            {
                for (int i = 0; i < 5; i++)
                    finalSeven[2 + i] = combo[i];
                HandType t = EvaluateBestHand(finalSeven);
                if (targetTypes.Contains(t)) counts[(int)t]++;
                total++;
            });
        }

        Dictionary<HandType, float> result = new Dictionary<HandType, float>();
        foreach (var t in targetTypes)
            result[t] = (float)counts[(int)t] / total;
        return result;
    }

    /// <summary>
    /// 下一张补牌概率
    /// </summary>
    /// <param name="playerHand">玩家手牌</param>
    /// <param name="communityCards">公共池牌组</param>
    /// <param name="targetRank"></param>
    /// <param name="targetSuit"></param>
    /// <returns></returns>
    public static float GetNextCardOutsProbability(
        List<Card> playerHand,
        List<Card> communityCards,
        Card.Rank? targetRank = null,
        Card.Suit? targetSuit = null)
    {
        Card[] allCards = GetAllCardsArray();
        bool[] used = new bool[52];
        MarkUsed(used, playerHand);
        MarkUsed(used, communityCards);

        int hit = 0, total = 0;
        for (int i = 0; i < allCards.Length; i++)
        {
            if (used[i]) 
                continue;
            total++;
            var c = allCards[i];
            bool rankMatch = targetRank == null || c.rank == targetRank.Value;
            bool suitMatch = targetSuit == null || c.suit == targetSuit.Value;
            if (rankMatch && suitMatch) 
                hit++;
        }
        return total == 0 ? 0f : (float)hit / total;
    }

    /// <summary>
    /// All-in 跑马胜率（同步版本，精确枚举，含赢牌组合存储）
    /// </summary>
    /// <param name="playerHands"></param>
    /// <param name="communityCards"></param>
    /// <param name="maxWinningCombosToStore"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static List<PlayerOdds> CalculateAllInOdds(
        List<List<Card>> playerHands,
        List<Card> communityCards,
        int maxWinningCombosToStore = 100)
    {
        int numPlayers = playerHands.Count;
        if (numPlayers < 2) throw new Exception("至少需要2名玩家");

        Card[] allCards = GetAllCardsArray();
        bool[] used = new bool[52];
        foreach (var hand in playerHands)
            if (hand.Count != 2) throw new Exception("手牌必须2张");
        foreach (var hand in playerHands) MarkUsed(used, hand);
        MarkUsed(used, communityCards);

        List<Card> remaining = new List<Card>();
        for (int i = 0; i < allCards.Length; i++)
            if (!used[i]) remaining.Add(allCards[i]);

        int slots = 5 - communityCards.Count;
        int[] wins = new int[numPlayers];
        int[] ties = new int[numPlayers];
        List<Card[]>[] winningBoards = new List<Card[]>[numPlayers];
        for (int i = 0; i < numPlayers; i++)
            winningBoards[i] = new List<Card[]>();

        int totalCombos = 0;

        if (slots == 0)
        {
            totalCombos = 1;
            var winners = DetermineWinners(playerHands, communityCards);
            if (winners.Count == 1)
            {
                wins[winners[0]]++;
                if (maxWinningCombosToStore > 0 && winningBoards[winners[0]].Count < maxWinningCombosToStore)
                    winningBoards[winners[0]].Add(communityCards.ToArray());
            }
            else
            {
                foreach (int idx in winners)
                {
                    ties[idx]++;
                    if (maxWinningCombosToStore > 0 && winningBoards[idx].Count < maxWinningCombosToStore)
                        winningBoards[idx].Add(communityCards.ToArray());
                }
            }
        }
        else
        {
            int[] indices = new int[slots];
            GenerateCombos(remaining, slots, 0, 0, indices, (combo) =>
            {
                totalCombos++;
                List<Card> fullCommunity = new List<Card>(communityCards);
                fullCommunity.AddRange(combo);
                var winners = DetermineWinners(playerHands, fullCommunity);

                if (winners.Count == 1)
                {
                    int idx = winners[0];
                    wins[idx]++;
                    if (maxWinningCombosToStore > 0 && winningBoards[idx].Count < maxWinningCombosToStore)
                        winningBoards[idx].Add(fullCommunity.ToArray());
                }
                else
                {
                    foreach (int idx in winners)
                    {
                        ties[idx]++;
                        if (maxWinningCombosToStore > 0 && winningBoards[idx].Count < maxWinningCombosToStore)
                            winningBoards[idx].Add(fullCommunity.ToArray());
                    }
                }
            });
        }

        List<PlayerOdds> results = new List<PlayerOdds>();
        for (int i = 0; i < numPlayers; i++)
        {
            float win = (float)wins[i] / totalCombos;
            float tie = (float)ties[i] / totalCombos;
            results.Add(new PlayerOdds
            {
                playerIndex = i,
                winRate = win,
                tieRate = tie,
                loseRate = 1f - win - tie,
                winningBoards = winningBoards[i]
            });
        }
        return results;
    }

    /// <summary>
    /// All-in 跑马胜率（协程版本，分帧计算，含赢牌组合存储）
    /// </summary>
    /// <param name="playerHands"></param>
    /// <param name="communityCards"></param>
    /// <param name="onComplete"></param>
    /// <param name="onProgress"></param>
    /// <param name="maxWinningCombosToStore"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static IEnumerator CalculateAllInOddsCoroutine(
        List<List<Card>> playerHands,
        List<Card> communityCards,
        Action<List<PlayerOdds>> onComplete,
        Action<float> onProgress = null,
        int maxWinningCombosToStore = 100)
    {
        int numPlayers = playerHands.Count;
        if (numPlayers < 2) throw new Exception("至少需要2名玩家");

        Card[] allCards = GetAllCardsArray();
        bool[] used = new bool[52];
        foreach (var hand in playerHands)
            if (hand.Count != 2) throw new Exception("手牌必须2张");
        foreach (var hand in playerHands) MarkUsed(used, hand);
        MarkUsed(used, communityCards);

        List<Card> remaining = new List<Card>();
        for (int i = 0; i < allCards.Length; i++)
            if (!used[i]) remaining.Add(allCards[i]);

        int slots = 5 - communityCards.Count;
        int[] wins = new int[numPlayers];
        int[] ties = new int[numPlayers];
        List<Card[]>[] winningBoards = new List<Card[]>[numPlayers];
        for (int i = 0; i < numPlayers; i++)
            winningBoards[i] = new List<Card[]>();

        long totalCombos = CombinationCount(remaining.Count, slots);
        if (totalCombos == 0)
        {
            var winners = DetermineWinners(playerHands, communityCards);
            if (winners.Count == 1)
            {
                wins[winners[0]] = 1;
                if (maxWinningCombosToStore > 0 && winningBoards[winners[0]].Count < maxWinningCombosToStore)
                    winningBoards[winners[0]].Add(communityCards.ToArray());
            }
            else
            {
                foreach (int idx in winners)
                {
                    ties[idx] = 1;
                    if (maxWinningCombosToStore > 0 && winningBoards[idx].Count < maxWinningCombosToStore)
                        winningBoards[idx].Add(communityCards.ToArray());
                }
            }
            totalCombos = 1;
        }
        else
        {
            int[] indices = new int[slots];
            for (int i = 0; i < slots; i++) indices[i] = i;
            long processed = 0;
            int batchSize = 50;
            List<Card> combo = new List<Card>(slots);

            while (true)
            {
                combo.Clear();
                for (int i = 0; i < slots; i++) combo.Add(remaining[indices[i]]);

                List<Card> fullCommunity = new List<Card>(communityCards);
                fullCommunity.AddRange(combo);
                var winners = DetermineWinners(playerHands, fullCommunity);

                if (winners.Count == 1)
                {
                    int idx = winners[0];
                    wins[idx]++;
                    if (maxWinningCombosToStore > 0 && winningBoards[idx].Count < maxWinningCombosToStore)
                        winningBoards[idx].Add(fullCommunity.ToArray());
                }
                else
                {
                    foreach (int idx in winners)
                    {
                        ties[idx]++;
                        if (maxWinningCombosToStore > 0 && winningBoards[idx].Count < maxWinningCombosToStore)
                            winningBoards[idx].Add(fullCommunity.ToArray());
                    }
                }

                processed++;
                if (processed % batchSize == 0)
                {
                    onProgress?.Invoke((float)processed / totalCombos);
                    yield return null;
                }

                // 生成下一个组合 (next_combination)
                int iIdx = slots - 1;
                while (iIdx >= 0 && indices[iIdx] == remaining.Count - slots + iIdx)
                    iIdx--;
                if (iIdx < 0) break;
                indices[iIdx]++;
                for (int j = iIdx + 1; j < slots; j++)
                    indices[j] = indices[j - 1] + 1;
            }
            onProgress?.Invoke(1f);
        }

        // 组装结果
        List<PlayerOdds> results = new List<PlayerOdds>();
        for (int i = 0; i < numPlayers; i++)
        {
            float win = (float)wins[i] / totalCombos;
            float tie = (float)ties[i] / totalCombos;
            results.Add(new PlayerOdds
            {
                playerIndex = i,
                winRate = win,
                tieRate = tie,
                loseRate = 1f - win - tie,
                winningBoards = winningBoards[i]
            });
        }
        onComplete?.Invoke(results);
        yield return null;
    }

    // ==================== 内部辅助方法 ====================

    private static long CombinationCount(int n, int k)
    {
        if (k < 0 || k > n) return 0;
        if (k > n - k) k = n - k;
        long result = 1;
        for (int i = 1; i <= k; i++)
        {
            result *= (n - i + 1);
            result /= i;
        }
        return result;
    }

    private static void GenerateCombos(List<Card> source, int k, int start, int depth, int[] indices, Action<List<Card>> callback)
    {
        if (depth == k)
        {
            List<Card> combo = new List<Card>();
            for (int i = 0; i < k; i++) combo.Add(source[indices[i]]);
            callback(combo);
            return;
        }
        for (int i = start; i < source.Count; i++)
        {
            indices[depth] = i;
            GenerateCombos(source, k, i + 1, depth + 1, indices, callback);
        }
    }

    private static void MarkUsed(bool[] used, List<Card> cards)
    {
        foreach (var c in cards)
        {
            int idx = (int)c.suit * 13 + ((int)c.rank - 1);
            used[idx] = true;
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

    public static HandType EvaluateBestHand(Card[] seven)
    {
        HandType best = HandType.HighCard;
        for (int a = 0; a < 3; a++)
            for (int b = a + 1; b < 4; b++)
                for (int c = b + 1; c < 5; c++)
                    for (int d = c + 1; d < 6; d++)
                        for (int e = d + 1; e < 7; e++)
                        {
                            Card[] five = new Card[5] { seven[a], seven[b], seven[c], seven[d], seven[e] };
                            HandType t = EvaluateFiveCards(five);
                            if (t > best) best = t;
                        }
        return best;
    }

    private static HandType EvaluateFiveCards(Card[] five)
    {
        int[] values = five.Select(c => GetCardValue(c.rank)).ToArray();
        Array.Sort(values);

        bool flush = five.All(c => c.suit == five[0].suit);
        bool straight = false;
        bool distinct = values.Distinct().Count() == 5;
        if (distinct)
        {
            if (values[4] - values[0] == 4) straight = true;
            if (values[0] == 2 && values[1] == 3 && values[2] == 4 && values[3] == 5 && values[4] == 14)
                straight = true;
        }

        var groups = five.GroupBy(c => GetCardValue(c.rank))
                         .OrderByDescending(g => g.Count())
                         .ThenByDescending(g => g.Key)
                         .ToList();
        int[] counts = groups.Select(g => g.Count()).ToArray();
        int[] ranks = groups.Select(g => g.Key).ToArray();

        if (flush && straight) return HandType.StraightFlush;
        if (counts[0] == 4) return HandType.FourOfAKind;
        if (counts[0] == 3 && counts.Length > 1 && counts[1] == 2) return HandType.FullHouse;
        if (flush) return HandType.Flush;
        if (straight) return HandType.Straight;
        if (counts[0] == 3) return HandType.ThreeOfAKind;
        if (counts[0] == 2 && counts.Length > 1 && counts[1] == 2) return HandType.TwoPair;
        if (counts[0] == 2) return HandType.OnePair;
        return HandType.HighCard;
    }

    // ---------- 比较相关 ----------
    private class HandStrength
    {
        public HandType type;
        public int[] tieBreakers;
        public HandStrength(HandType type, int[] breakers)
        {
            this.type = type;
            tieBreakers = breakers;
        }
    }

    private static HandStrength GetHandStrength(Card[] five)
    {
        int[] values = five.Select(c => GetCardValue(c.rank)).ToArray();
        Array.Sort(values);
        bool flush = five.All(c => c.suit == five[0].suit);
        bool straight = false;
        bool distinct = values.Distinct().Count() == 5;
        if (distinct)
        {
            if (values[4] - values[0] == 4) straight = true;
            if (values[0] == 2 && values[1] == 3 && values[2] == 4 && values[3] == 5 && values[4] == 14)
                straight = true;
        }

        var groups = five.GroupBy(c => GetCardValue(c.rank))
                         .OrderByDescending(g => g.Count())
                         .ThenByDescending(g => g.Key)
                         .ToList();
        int[] counts = groups.Select(g => g.Count()).ToArray();
        int[] ranks = groups.Select(g => g.Key).ToArray();

        HandType type;
        int[] breakers;

        if (flush && straight)
        {
            type = HandType.StraightFlush;
            int high = (values[4] == 14 && values[0] == 2) ? 5 : values[4];
            breakers = new int[] { high };
        }
        else if (counts[0] == 4)
        {
            type = HandType.FourOfAKind;
            breakers = new int[] { ranks[0], ranks[1] };
        }
        else if (counts[0] == 3 && counts.Length > 1 && counts[1] == 2)
        {
            type = HandType.FullHouse;
            breakers = new int[] { ranks[0], ranks[1] };
        }
        else if (flush)
        {
            type = HandType.Flush;
            breakers = values.Reverse().ToArray();
        }
        else if (straight)
        {
            type = HandType.Straight;
            int high = (values[4] == 14 && values[0] == 2) ? 5 : values[4];
            breakers = new int[] { high };
        }
        else if (counts[0] == 3)
        {
            type = HandType.ThreeOfAKind;
            var kickers = ranks.Skip(1).OrderByDescending(x => x).ToArray();
            breakers = new int[] { ranks[0] }.Concat(kickers).ToArray();
        }
        else if (counts[0] == 2 && counts.Length > 1 && counts[1] == 2)
        {
            type = HandType.TwoPair;
            int highPair = ranks[0];
            int lowPair = ranks[1];
            int kicker = ranks[2];
            breakers = new int[] { highPair, lowPair, kicker };
        }
        else if (counts[0] == 2)
        {
            type = HandType.OnePair;
            var kickers = ranks.Skip(1).OrderByDescending(x => x).ToArray();
            breakers = new int[] { ranks[0] }.Concat(kickers).ToArray();
        }
        else
        {
            type = HandType.HighCard;
            breakers = values.Reverse().ToArray();
        }

        return new HandStrength(type, breakers);
    }

    private static int CompareStrength(HandStrength a, HandStrength b)
    {
        if (a.type != b.type)
            return ((int)a.type).CompareTo((int)b.type);
        for (int i = 0; i < Math.Min(a.tieBreakers.Length, b.tieBreakers.Length); i++)
        {
            if (a.tieBreakers[i] != b.tieBreakers[i])
                return a.tieBreakers[i].CompareTo(b.tieBreakers[i]);
        }
        return 0;
    }

    private static int CompareFiveCardHands(Card[] handA, Card[] handB)
    {
        return CompareStrength(GetHandStrength(handA), GetHandStrength(handB));
    }

    private static int GetCardValue(Card.Rank rank)
    {
        return rank == Card.Rank.Ace ? 14 : (int)rank;
    }
}