using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Test : MonoBehaviour
{
    void Start()
    {
        TestCard();
    }

    void TestCard()
    {
        // ========== 1. 洗牌发牌（先发5张公共牌，再每人2张） ==========
        PokerDeck deck = new PokerDeck();
        Card[] community = deck.DealCards(3);
        Card[] hand1 = deck.DealCards(2);
        Card[] hand2 = deck.DealCards(2);

        Debug.Log("公共牌: " + string.Join(", ", community));
        Debug.Log("玩家1手牌: " + string.Join(", ", hand1));
        Debug.Log("玩家2手牌: " + string.Join(", ", hand2));

        List<Card> myHand = new List<Card> { hand1[0], hand1[1] };
        List<Card> commList = new List<Card>(community);

        // ========== 2. 最终牌型概率（强牌型） ==========
        var probs = PokerHandEvaluator.GetFinalHandProbabilities(myHand, commList);
        Debug.Log("== 强牌型概率 ==");
        foreach (var kv in probs)
            if (kv.Value > 0) Debug.Log($"{kv.Key}: {kv.Value:P2}");

        // ========== 3. 下一张补牌概率（红桃） ==========
        //float flushProb = PokerHandEvaluator.GetNextCardOutsProbability(
        //    myHand, commList, targetSuit: Card.Suit.红桃);
        //Debug.Log($"下一张是红桃的概率: {flushProb:P2}");

        // ========== 4. All-in 跑马（2人示例，带赢牌组合） ==========
        List<Card> playerA = new List<Card>
        {
            new Card(Card.Suit.黑桃, Card.Rank.Queen),
            new Card(Card.Suit.方块, Card.Rank.Jack)
        };
        List<Card> playerB = new List<Card>
        {
            new Card(Card.Suit.梅花, Card.Rank.Five),
            new Card(Card.Suit.梅花, Card.Rank.Six)
        };
        List<List<Card>> players = new List<List<Card>> { playerA, playerB };
        List<Card> board = new List<Card>
        {
            new Card(Card.Suit.红桃, Card.Rank.Ten),
            new Card(Card.Suit.梅花, Card.Rank.Seven),
            new Card(Card.Suit.红桃, Card.Rank.Three),
            new Card(Card.Suit.梅花, Card.Rank.Three)
        };

        // 用协程计算（避免卡顿）
        StartCoroutine(PokerHandEvaluator.CalculateAllInOddsCoroutine(
            players,
            board,
            onComplete: (odds) =>
            {
                Debug.Log("== All-in 跑马结果 ==");
                foreach (var od in odds)
                {
                    Debug.Log(od.ToString());
                    // 打印前3种赢牌组合（如果有）
                    if (od.winningBoards != null && od.winningBoards.Count > 0)
                    {
                        int show = Mathf.Min(od.winningBoards.Count, 3);
                        for (int i = 0; i < show; i++)
                        {
                            string boardStr = string.Join(", ", od.winningBoards[i].Select(c => c.ToString()));
                            Debug.Log($"  赢牌组合{i + 1}: {boardStr}");
                        }
                        if (od.winningBoards.Count > 3)
                            Debug.Log($"  ... 还有 {od.winningBoards.Count - 3} 种");
                    }
                }
            },
            onProgress: (p) => Debug.Log($"计算进度: {p:P2}"),
            maxWinningCombosToStore: 50
        ));
    }

    public void Win(List<List<Card>> playerHands, List<Card> community) 
    {
        foreach (var item in PokerHandEvaluator.DetermineWinners(playerHands, community))
        {

        }
    }
}
