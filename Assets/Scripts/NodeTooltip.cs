using UnityEngine;
using TMPro;

public class NodeTooltip : MonoBehaviour
{
    public TextMeshProUGUI tooltipText;
    private RectTransform rectTransform;
    private Camera mainCamera;
    private Canvas canvas;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;
        canvas = GetComponentInParent<Canvas>();
        
        if (tooltipText == null)
            tooltipText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void SetTooltipText(string text)
    {
        if (tooltipText != null)
        {
            tooltipText.text = text;
        }
    }

    void LateUpdate()
    {
        if (canvas == null || mainCamera == null) return;

        // Convert screen position to canvas position
        Vector2 screenPos = mainCamera.WorldToScreenPoint(transform.position);
        Vector2 canvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPos,
            canvas.worldCamera,
            out canvasPos);

        // Keep tooltip within screen bounds
        Vector2 size = rectTransform.sizeDelta;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        
        // Adjust position to keep tooltip on screen
        float rightEdge = canvasPos.x + size.x;
        float topEdge = canvasPos.y + size.y;
        
        if (rightEdge > screenSize.x)
            canvasPos.x = screenSize.x - size.x;
        if (topEdge > screenSize.y)
            canvasPos.y = screenSize.y - size.y;
        if (canvasPos.x < 0)
            canvasPos.x = 0;
        if (canvasPos.y < 0)
            canvasPos.y = 0;

        rectTransform.anchoredPosition = canvasPos;
    }
} 