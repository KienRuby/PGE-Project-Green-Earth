using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Quan ly Carousel cho muc Special Item trong Shop:
/// - Chuyen doi giua 3 goi: Welcome Package, Intermediate Pack, Advanced Pack.
/// - Dong bo 3 pagination dots.
/// - Ho tro vuot cham (drag/swipe) va bam truc tiep vao dot.
/// </summary>
public sealed class ShopCarouselUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Pages / Cards")]
    [SerializeField] private RectTransform[] pages;
    [SerializeField] private Image[] dots;

    [Header("Styling")]
    [SerializeField] private Color activeDotColor = Color.white;
    [SerializeField] private Color inactiveDotColor = new Color(0.7f, 0.85f, 0.95f, 0.45f);
    [SerializeField] private float activeDotScale = 1.3f;
    [SerializeField] private float inactiveDotScale = 1.0f;

    [Header("Auto Slide")]
    [SerializeField] private bool enableAutoSlide = true;
    [SerializeField] private float autoSlideInterval = 5f;

    private int currentPage = 0;
    private Coroutine autoSlideCoroutine;
    private Vector2 dragStartPos;
    private bool isDragging = false;

    public int CurrentPage => currentPage;
    public RectTransform[] Pages => pages;
    public Image[] Dots => dots;

    private ScrollRect parentScrollRect;
    private bool isVerticalScrolling = false;

    private void Awake()
    {
        parentScrollRect = GetComponentInParent<ScrollRect>();
    }

    private void Start()
    {
        if (parentScrollRect == null) parentScrollRect = GetComponentInParent<ScrollRect>();
        SetupDotButtons();
        ShowPage(currentPage, false);
        StartAutoSlide();
    }

    private void OnEnable()
    {
        StartAutoSlide();
    }

    private void OnDisable()
    {
        StopAutoSlide();
    }

    public void Setup(RectTransform[] pageList, Image[] dotList)
    {
        if (parentScrollRect == null) parentScrollRect = GetComponentInParent<ScrollRect>();
        pages = pageList;
        dots = dotList;
        SetupDotButtons();
        ShowPage(0, false);
    }

    private void SetupDotButtons()
    {
        if (dots == null) return;
        for (int i = 0; i < dots.Length; i++)
        {
            int pageIndex = i;
            Button btn = dots[i].GetComponent<Button>();
            if (btn == null)
            {
                btn = dots[i].gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
            }
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                SetPage(pageIndex);
                RestartAutoSlide();
            });
        }
    }

    public void SetPage(int index)
    {
        if (pages == null || pages.Length == 0) return;
        index = Mathf.Clamp(index, 0, pages.Length - 1);
        currentPage = index;
        ShowPage(currentPage, true);
    }

    public void NextPage()
    {
        if (pages == null || pages.Length == 0) return;
        int next = (currentPage + 1) % pages.Length;
        SetPage(next);
    }

    public void PreviousPage()
    {
        if (pages == null || pages.Length == 0) return;
        int prev = (currentPage - 1 + pages.Length) % pages.Length;
        SetPage(prev);
    }

    private void ShowPage(int targetIndex, bool animated)
    {
        if (pages == null) return;
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
            {
                pages[i].gameObject.SetActive(i == targetIndex);
            }
        }
        UpdateDots(targetIndex);
    }

    private void UpdateDots(int activeIndex)
    {
        if (dots == null) return;
        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] != null)
            {
                bool isActive = (i == activeIndex);
                dots[i].color = isActive ? activeDotColor : inactiveDotColor;
                dots[i].transform.localScale = Vector3.one * (isActive ? activeDotScale : inactiveDotScale);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        dragStartPos = eventData.position;
        isVerticalScrolling = false;
        StopAutoSlide();

        if (parentScrollRect != null)
        {
            parentScrollRect.OnBeginDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        float deltaX = Mathf.Abs(eventData.position.x - dragStartPos.x);
        float deltaY = Mathf.Abs(eventData.position.y - dragStartPos.y);

        if (deltaY > deltaX || isVerticalScrolling)
        {
            isVerticalScrolling = true;
            if (parentScrollRect != null)
            {
                parentScrollRect.OnDrag(eventData);
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        float deltaX = eventData.position.x - dragStartPos.x;
        float deltaY = Mathf.Abs(eventData.position.y - dragStartPos.y);
        float swipeThreshold = 50f;

        if (isVerticalScrolling && parentScrollRect != null)
        {
            parentScrollRect.OnEndDrag(eventData);
        }
        else if (Mathf.Abs(deltaX) > deltaY)
        {
            if (deltaX < -swipeThreshold)
            {
                NextPage();
            }
            else if (deltaX > swipeThreshold)
            {
                PreviousPage();
            }
        }

        isVerticalScrolling = false;
        RestartAutoSlide();
    }

    private void StartAutoSlide()
    {
        if (!enableAutoSlide) return;
        StopAutoSlide();
        if (gameObject.activeInHierarchy)
        {
            autoSlideCoroutine = StartCoroutine(AutoSlideRoutine());
        }
    }

    private void StopAutoSlide()
    {
        if (autoSlideCoroutine != null)
        {
            StopCoroutine(autoSlideCoroutine);
            autoSlideCoroutine = null;
        }
    }

    private void RestartAutoSlide()
    {
        StopAutoSlide();
        StartAutoSlide();
    }

    private IEnumerator AutoSlideRoutine()
    {
        while (enableAutoSlide)
        {
            yield return new WaitForSeconds(autoSlideInterval);
            if (!isDragging && gameObject.activeInHierarchy)
            {
                NextPage();
            }
        }
    }
}
