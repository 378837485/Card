using System;
using UnityEngine;

public class PokerDeck
{
    private Card[] cards;
    private int dealIndex;
    private System.Random rng;

    public PokerDeck()
    {
        rng = new System.Random();
        cards = new Card[52];
        InitDeck();
        Shuffle();
    }

    private void InitDeck()
    {
        int index = 0;
        foreach (Card.Suit suit in Enum.GetValues(typeof(Card.Suit)))
        {
            foreach (Card.Rank rank in Enum.GetValues(typeof(Card.Rank)))
            {
                cards[index++] = new Card(suit, rank);
            }
        }
    }

    public void Shuffle()
    {
        int n = cards.Length;
        for (int i = n - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            Card temp = cards[i];
            cards[i] = cards[j];
            cards[j] = temp;
        }
        dealIndex = 0;
    }

    public Card[] DealCards(int count)
    {
        if (dealIndex + count > cards.Length)
        {
            Debug.LogError("牌堆不够，请重新洗牌");
            return null;
        }
        Card[] dealt = new Card[count];
        Array.Copy(cards, dealIndex, dealt, 0, count);
        dealIndex += count;
        return dealt;
    }

    public int RemainingCount => cards.Length - dealIndex;
}