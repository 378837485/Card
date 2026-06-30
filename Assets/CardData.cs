using DG.Tweening;
using UnityEngine;

public class CardData : MonoBehaviour
{
    public Card data;

    public bool isShow = false;

    public SpriteRenderer back;
    public SpriteRenderer card;

    private void OnEnable()
    {
        card.sortingOrder = 0;
        isShow = false;
    }

    public void Show(Card _data) 
    {
        if (isShow)
            return;

        isShow = true;

        data = _data;

        card.sprite = Resources.Load<Sprite>(data.ToString());

        transform.DOScaleX(0, 0.2f).SetEase(Ease.Linear).OnComplete(() => {
            card.sortingOrder = 2;
            transform.DOScaleX(1, 0.2f).SetEase(Ease.Linear);
        });
    }

    private void OnDisable()
    {
        card.sortingOrder = 0;
    }
}
