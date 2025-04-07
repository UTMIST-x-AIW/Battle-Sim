using UnityEngine;
using TMPro;

public class NodeTooltip : MonoBehaviour
{
    public TextMeshProUGUI tooltipText;
    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 offset = new Vector2(0, 0);

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        
        if (tooltipText == null)
            tooltipText = GetComponentInChildren<TextMeshProUGUI>();

        // Set proper anchoring for the tooltip
        // rectTransform.pivot = new Vector2(0, 1);        // Pivot at top-left
        // rectTransform.anchorMin = Vector2.zero;         // Anchor to bottom-left
        // rectTransform.anchorMax = Vector2.zero;
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
        if (canvas == null) return;

        // Get mouse position
        Vector2 mousePos = Input.mousePosition;

        // Convert to canvas space
        Vector2 canvasPos;
        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePos, cam, out canvasPos);

        // Get canvas scale factor
        float scaleFactor = canvas.scaleFactor;

        // Get tooltip size in screen space
        Vector2 tooltipSize = rectTransform.rect.size * scaleFactor;

        // Get screen bounds in canvas space
        Vector2 screenMin = Vector2.zero;
        Vector2 screenMax = new Vector2(Screen.width, Screen.height);
        Vector2 screenMinCanvas, screenMaxCanvas;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenMin, cam, out screenMinCanvas);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenMax, cam, out screenMaxCanvas);

        // Apply initial offset
        canvasPos += offset;

        // Check right edge
        if (mousePos.x + tooltipSize.x + offset.x > Screen.width)
        {
            // Place tooltip to the left of the cursor
            canvasPos.x = canvasPos.x - tooltipSize.x/scaleFactor - offset.x * 2;
        }

        // Check top edge
        if (mousePos.y + tooltipSize.y + offset.y > Screen.height)
        {
            // Place tooltip below the cursor
            canvasPos.y = canvasPos.y - tooltipSize.y/scaleFactor - offset.y * 2;
        }

        // Ensure tooltip stays within screen bounds
        // canvasPos.x = Mathf.Clamp(canvasPos.x, screenMinCanvas.x + offset.x, screenMaxCanvas.x - tooltipSize.x/scaleFactor - offset.x);
        // canvasPos.y = Mathf.Clamp(canvasPos.y, screenMinCanvas.y + tooltipSize.y/scaleFactor + offset.y, screenMaxCanvas.y - offset.y);

        // Apply position
        rectTransform.anchoredPosition = canvasPos;
    }
} 