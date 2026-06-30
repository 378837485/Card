using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.GPUSort;

public class Test : MonoBehaviour
{
    public GameObject card;
    public List<GameObject> cards = new List<GameObject>();

    public int round;

    public Text pT;
    public Text n1T;
    public Text n2T;
    public Text n3T;
    public Text n4T;

    public Text combo;
    public Text win;

    public Transform btn;

    int pM;
    int n1M;
    int n2M;
    int n3M;
    int n4M;

    List<Card> player;
    List<Card> npc1;
    List<Card> npc2;
    List<Card> npc3;
    List<Card> npc4;

    List<Card> community;

    void Start()
    {
        StartCoroutine(Game());
        //TestCard();
    }

    public void Check() 
    {
        btn.gameObject.SetActive(false);
        StartCoroutine(NextRound());
    }

    IEnumerator NextRound() 
    {
        if (round == 0)
        {
            cards[10].GetComponent<CardData>().Show(community[0]);
            cards[11].GetComponent<CardData>().Show(community[1]);
            cards[12].GetComponent<CardData>().Show(community[2]);
            yield return new WaitForSeconds(1f);
            //显示组合
            btn.gameObject.SetActive(true);
            round = 3;
            UpdateCombo();
        }
        else if (round < 5)
        {
            cards[10 + round].GetComponent<CardData>().Show(community[round]);
            round++;
            yield return new WaitForSeconds(1f);
            //显示组合
            btn.gameObject.SetActive(true);
            UpdateCombo();
        }
        else 
        {
            combo.text = "";

            //开牌
            cards[2].GetComponent<CardData>().Show(npc1[0]);
            cards[3].GetComponent<CardData>().Show(npc1[1]);
            cards[4].GetComponent<CardData>().Show(npc2[0]);
            cards[5].GetComponent<CardData>().Show(npc2[1]);
            cards[6].GetComponent<CardData>().Show(npc3[0]);
            cards[7].GetComponent<CardData>().Show(npc3[1]);
            cards[8].GetComponent<CardData>().Show(npc4[0]);
            cards[9].GetComponent<CardData>().Show(npc4[1]);
            yield return new WaitForSeconds(1f);

            List<int> index = Win();

            for (int i = 0; i < index.Count; i++)
            {
                cards[index[i] * 2].transform.DOScale(1.5f, 0.2f).SetEase(Ease.Linear);
                cards[index[i] * 2 + 1].transform.DOScale(1.5f, 0.2f).SetEase(Ease.Linear);
            }

            if (index.Count > 1)
                win.text = "玩家与电脑平局";
            else 
            {
                if (index[0] == 0)
                    win.text = "玩家赢";
                else
                    win.text = "电脑赢";
            }

            yield return new WaitForSeconds(2f);
            StartCoroutine(GameOver());
        }
    }

    public List<int> Win()
    {
        List<List<Card>> playerHands = new List<List<Card>>() { player, npc1, npc2, npc3, npc4 };
        return PokerHandEvaluator.DetermineWinners(playerHands, community);
    }

    public void UpdateCombo() 
    {
        combo.text = "";
        List<Card> showC = new List<Card>();

        for (int i = 0; i < round; i++)
        {
            showC.Add(community[i]);
        }

        var probs = PokerHandEvaluator.GetFinalHandProbabilities(player, showC);

        foreach (var kv in probs)
            if (kv.Value > 0)
                combo.text += ($"{kv.Key}: {kv.Value:P2}\n");
    }

    public void Fold()
    {
        btn.gameObject.SetActive(false);
        StopAllCoroutines();
        StartCoroutine(GameOver());
    }

    IEnumerator GameOver() 
    {
        combo.text = "";
        win.text = "";
        for (int i = cards.Count; i > 0; i--)
        {
            Destroy(cards[i - 1]);
        }
        cards = new List<GameObject>();
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(Game());
    }

    IEnumerator Game()
    {
        btn.gameObject.SetActive(false);

        pT.text = "";
        n1T.text = "";
        n2T.text = "";
        n3T.text = "";
        n4T.text = "";

        PokerDeck deck = new PokerDeck();

        GameObject h1 = Instantiate(card);
        h1.gameObject.SetActive(true);
        h1.transform.DOMove(new Vector3(-0.5f, -2.5f, 0), 0.5f).SetEase(Ease.Linear);
        GameObject h2 = Instantiate(card);
        h2.gameObject.SetActive(true);
        h2.transform.DOMove(new Vector3(0.5f, -2.5f, 0), 0.5f).SetEase(Ease.Linear);
        cards.Add(h1);
        cards.Add(h2);
        player = new List<Card>(deck.DealCards(2));

        yield return new WaitForSeconds(1f);

        GameObject p1 = Instantiate(card);
        p1.gameObject.SetActive(true);
        p1.transform.DOMove(new Vector3(-5.5f, -0.35f, 0), 0.5f).SetEase(Ease.Linear);
        GameObject p2 = Instantiate(card);
        p2.gameObject.SetActive(true);
        p2.transform.DOMove(new Vector3(-4.5f, -0.35f, 0), 0.5f).SetEase(Ease.Linear);
        cards.Add(p1);
        cards.Add(p2);
        npc1 = new List<Card>(deck.DealCards(2));

        yield return new WaitForSeconds(1f);

        GameObject p3 = Instantiate(card);
        p3.gameObject.SetActive(true);
        p3.transform.DOMove(new Vector3(-2.8f, 1.5f, 0), 0.5f).SetEase(Ease.Linear);
        GameObject p4 = Instantiate(card);
        p4.gameObject.SetActive(true);
        p4.transform.DOMove(new Vector3(-1.8f, 1.5f, 0), 0.5f).SetEase(Ease.Linear);
        cards.Add(p3);
        cards.Add(p4);
        npc2 = new List<Card>(deck.DealCards(2));

        yield return new WaitForSeconds(1f);

        GameObject p5 = Instantiate(card);
        p5.gameObject.SetActive(true);
        p5.transform.DOMove(new Vector3(1.8f, 1.5f, 0), 0.5f).SetEase(Ease.Linear);
        GameObject p6 = Instantiate(card);
        p6.gameObject.SetActive(true);
        p6.transform.DOMove(new Vector3(2.8f, 1.5f, 0), 0.5f).SetEase(Ease.Linear);
        cards.Add(p5);
        cards.Add(p6);
        npc3 = new List<Card>(deck.DealCards(2));

        yield return new WaitForSeconds(1f);

        GameObject p7 = Instantiate(card);
        p7.gameObject.SetActive(true);
        p7.transform.DOMove(new Vector3(4.5f, -0.35f, 0), 0.5f).SetEase(Ease.Linear);
        GameObject p8 = Instantiate(card);
        p8.gameObject.SetActive(true);
        p8.transform.DOMove(new Vector3(5.5f, -0.35f, 0), 0.5f).SetEase(Ease.Linear);
        cards.Add(p7);
        cards.Add(p8);
        npc4 = new List<Card>(deck.DealCards(2));

        yield return new WaitForSeconds(1f);

        GameObject c1 = Instantiate(card);
        c1.gameObject.SetActive(true);
        c1.transform.DOMove(new Vector3(-2f, 0, 0), 0.2f).SetEase(Ease.Linear);
        GameObject c2 = Instantiate(card);
        c2.gameObject.SetActive(true);
        c2.transform.DOMove(new Vector3(-1f, 0, 0), 0.2f).SetEase(Ease.Linear);
        GameObject c3 = Instantiate(card);
        c3.gameObject.SetActive(true);
        c3.transform.DOMove(new Vector3(0, 0, 0), 0.2f).SetEase(Ease.Linear);
        GameObject c4 = Instantiate(card);
        c4.gameObject.SetActive(true);
        c4.transform.DOMove(new Vector3(1f, 0, 0), 0.2f).SetEase(Ease.Linear);
        GameObject c5 = Instantiate(card);
        c5.gameObject.SetActive(true);
        c5.transform.DOMove(new Vector3(2f, 0, 0), 0.2f).SetEase(Ease.Linear);
        cards.Add(c1);
        cards.Add(c2); 
        cards.Add(c3);
        cards.Add(c4);
        cards.Add(c5);
        community = new List<Card>(deck.DealCards(5));

        yield return new WaitForSeconds(0.5f);
        //show card
        h1.GetComponent<CardData>().Show(player[0]);
        h2.GetComponent<CardData>().Show(player[1]);

        //show UI
        btn.gameObject.SetActive(true);

        round = 0;
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
            new Card(Card.Suit.c, Card.Rank.Queen),
            new Card(Card.Suit.d, Card.Rank.Jack)
        };
        List<Card> playerB = new List<Card>
        {
            new Card(Card.Suit.s, Card.Rank.Five),
            new Card(Card.Suit.s, Card.Rank.Six)
        };
        List<List<Card>> players = new List<List<Card>> { playerA, playerB };
        List<Card> board = new List<Card>
        {
            new Card(Card.Suit.h, Card.Rank.Ten),
            new Card(Card.Suit.s, Card.Rank.Seven),
            new Card(Card.Suit.h, Card.Rank.Three),
            new Card(Card.Suit.s, Card.Rank.Three)
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
}
