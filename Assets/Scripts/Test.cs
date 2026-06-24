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
        List<Card> myHand = new List<Card>{
            new Card(Card.Suit.∫ÏÃ“, Card.Rank.Ace),
            new Card(Card.Suit.∫ÏÃ“, Card.Rank.King)
        };

        List<Card> community = new List<Card>{
            new Card(Card.Suit.∫ÏÃ“, Card.Rank.Queen),
            new Card(Card.Suit.∫ÏÃ“, Card.Rank.Jack),
            new Card(Card.Suit.∫⁄Ã“, Card.Rank.Two)
        };

        var probs = PokerProbabilityCalculator.GetFinalHandProbabilities(myHand, community);
        foreach (var kv in probs.OrderByDescending(kv => kv.Value))
            Debug.Log($"{kv.Key}: {kv.Value:P2}");
    }
}
