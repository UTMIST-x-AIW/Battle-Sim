using UnityEngine;
using TMPro;

public class NodeTooltip : MonoBehaviour
{
    public TextMeshProUGUI tooltipText;
    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 offset = new Vector2(15, 15);

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        
        if (tooltipText == null)
            tooltipText = GetComponentInChildren<TextMeshProUGUI>();

        // Set proper anchoring for the tooltip
        rectTransform.pivot = new Vector2(0, 1);        // Pivot at top-left
        rectTransform.anchorMin = Vector2.zero;         // Anchor to bottom-left
        rectTransform.anchorMax = Vector2.zero;
    }

    public void SetTooltipText(string text)
    {
        if (tooltipText != null)
        {
            tooltipText.text = text;
        }
    }

    void Update()
    {
        if (canvas == null) return;

        // Get mouse position and convert to canvas space
        Vector2 mousePos = Input.mousePosition;
        Vector2 position = mousePos + offset;

        // Keep tooltip on screen
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        // Adjust position if too close to screen edges
        if (position.x + width > Screen.width)
        {
            position.x = mousePos.x - width - offset.x;
        }
        if (position.y + height > Screen.height)
        {
            position.y = mousePos.y - height - offset.y;
        }

        // Set position directly in screen space
        rectTransform.position = position;
    }
} 