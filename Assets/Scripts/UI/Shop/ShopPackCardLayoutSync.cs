using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class ShopPackCardLayoutSync : MonoBehaviour
{
    private RectTransform cardRect;
    private LayoutElement cardLayout;
    private RectTransform cardsContainer;
    private LayoutElement carouselLayout;
    private float paginationSpace;

    private void OnEnable()
    {
        cardRect = (RectTransform)transform;
        cardLayout = GetComponent<LayoutElement>();
        cardsContainer = transform.parent as RectTransform;
        carouselLayout = cardsContainer.parent.GetComponent<LayoutElement>();
        paginationSpace = carouselLayout.preferredHeight - cardsContainer.rect.height;
        SyncLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (cardsContainer != null) SyncLayout();
    }

    private void SyncLayout()
    {
        if (!Mathf.Approximately(cardLayout.preferredWidth, cardRect.rect.width))
            cardLayout.preferredWidth = cardRect.rect.width;
        if (!Mathf.Approximately(cardLayout.preferredHeight, cardRect.rect.height))
            cardLayout.preferredHeight = cardRect.rect.height;

        float cardHeight = 0f;
        foreach (RectTransform card in cardsContainer)
            cardHeight = Mathf.Max(cardHeight, card.rect.height);

        if (!Mathf.Approximately(cardsContainer.rect.height, cardHeight))
            cardsContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cardHeight);

        float preferredHeight = cardHeight + paginationSpace;
        if (!Mathf.Approximately(carouselLayout.preferredHeight, preferredHeight))
            carouselLayout.preferredHeight = preferredHeight;
    }
}
