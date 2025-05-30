using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using NEAT.Genes;
using TMPro;
using Unity.VisualScripting;

/// <summary>
/// Network Visualizer: uses object pooling and straight connections,
/// with immediate color updates (no external tween library required).
/// </summary>
public class NetworkVisualizer : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform networkPanel;
    public GameObject nodePrefab;
    public GameObject connectionPrefab;
    public CreatureStats creatureStatsPanel;
    public Sprite gradientBackground;

    [Header("Layout Settings")]
    public Vector2 panelOffset = new Vector2(20, 20);
    public Vector2 panelSize = new Vector2(300, 200);
    public float nodeSize = 32f;
    public float edgeMargin = 40f;

    [Header("Visualization Settings")]
    [Range(0f, 10f)] public float outlineThickness = 3f;

    [Header("Tooltip Settings")]
    public GameObject tooltipPrefab;

    // Internal state
    private Dictionary<int, Vector2> nodePositions = new Dictionary<int, Vector2>();
    private Dictionary<int, Image> nodeImages = new Dictionary<int, Image>();
    private List<GameObject> allNodes = new List<GameObject>();
    private List<GameObject> allConnections = new List<GameObject>();
    private GameObject activeTooltip;
    public Creature selectedCreature;
    private CameraController cameraController;
    private Dictionary<int, double> lastNodeValues = new Dictionary<int, double>();

    // Simple pools
    private Queue<GameObject> nodePool = new Queue<GameObject>();
    private Queue<GameObject> connectionPool = new Queue<GameObject>();

    void Start()
    {
        cameraController = FindObjectOfType<CameraController>();

        // Hide & setup panel
        if (networkPanel == null) return;
        networkPanel.gameObject.SetActive(false);
        networkPanel.anchorMin = new Vector2(0, 1);
        networkPanel.anchorMax = new Vector2(0, 1);
        networkPanel.pivot = new Vector2(0, 1);
        networkPanel.sizeDelta = panelSize;
        networkPanel.anchoredPosition = panelOffset;

        // Optional gradient backdrop
        if (gradientBackground != null)
        {
            var bg = new GameObject("Background", typeof(Image));
            bg.transform.SetParent(networkPanel, false);
            var img = bg.GetComponent<Image>();
            img.sprite = gradientBackground;
            img.type = Image.Type.Sliced;
            img.color = Color.white * 0.8f;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    void Update()
    {
        // Check for mouse clicks to select creatures
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            
            // Cast a ray and check for creatures
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
            if (hit.collider != null)
            {
                Creature creature = hit.collider.GetComponent<Creature>();
                // If no Creature on the hit object, try the parent (for child colliders)
                if (creature == null)
                {
                    creature = hit.collider.GetComponentInParent<Creature>();
                }
                
                if (creature != null)
                {
                    SelectCreature(creature);
                }
                else
                {
                    // Clicked on something else - reset camera
                    HideNetwork();
                    if (cameraController != null)
                    {
                        cameraController.ResetCamera();
                    }
                }
            }
            else
            {
                // Clicked on empty space - reset camera
                HideNetwork();
                if (cameraController != null)
                {
                    cameraController.ResetCamera();
                }
            }
        }
        
        // Check for escape key to hide network
        if (Input.GetKeyDown(KeyCode.Escape))
            HideNetwork();
            if (cameraController != null)
            {
                cameraController.ResetCamera();
            }
        }
        
        // Check for 'S' key to save selected creature
        if (Input.GetKeyDown(KeyCode.X) && selectedCreature != null)
        {
            CreatureSaver.SaveCreature(selectedCreature);
    }

    void SelectCreature(Creature c)
    {
        selectedCreature = c;
        ShowNetwork();
        cameraController?.SetTarget(c.transform);
        creatureStatsPanel?.ShowStats(c);
    }

    void ShowNetwork()
    {
        ClearVisualization();
        networkPanel.gameObject.SetActive(true);

        var brain = selectedCreature.GetBrain();
        var nodes = (Dictionary<int, NodeGene>)brain.GetType()
            .GetField("_nodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(brain);
        var conns = (Dictionary<int, ConnectionGene>)brain.GetType()
            .GetField("_connections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(brain);

        CalculateNodePositions(nodes);

        foreach (var conn in conns.Values)
            if (conn.Enabled) DrawConnection(conn);

        foreach (var node in nodes.Values)
            DrawNode(node);
    }

    void CalculateNodePositions(Dictionary<int, NodeGene> nodes)
    {
        nodePositions.Clear();
        float w = networkPanel.sizeDelta.x - edgeMargin * 2;
        float h = networkPanel.sizeDelta.y - edgeMargin;

        var inputs = new List<NodeGene>();
        var outputs = new List<NodeGene>();
        var hidden = new Dictionary<int, List<NodeGene>>();
        int maxLayer = 0;

        foreach (var n in nodes.Values)
        {
            switch (n.Type)
            {
                case NodeType.Input: inputs.Add(n); break;
                case NodeType.Output: outputs.Add(n); break;
                case NodeType.Hidden:
                    if (!hidden.ContainsKey(n.Layer)) hidden[n.Layer] = new List<NodeGene>();
                    hidden[n.Layer].Add(n);
                    maxLayer = Mathf.Max(maxLayer, n.Layer);
                    break;
            }
        }

        float vs = h / (inputs.Count + 1);
        for (int i = 0; i < inputs.Count; i++)
            nodePositions[inputs[i].Key] = new Vector2(nodeSize / 2, -edgeMargin - vs * (i + 1));

        vs = h / (outputs.Count + 1);
        for (int i = 0; i < outputs.Count; i++)
            nodePositions[outputs[i].Key] = new Vector2(w - nodeSize / 2, -edgeMargin - vs * (i + 1));

        if (maxLayer > 0)
        {
            float layerSpace = (w - nodeSize) / (maxLayer + 1);
            for (int layer = 1; layer <= maxLayer; layer++)
            {
                var list = hidden.ContainsKey(layer) ? hidden[layer] : new List<NodeGene>();
                float hs = h / (list.Count + 1);
                for (int i = 0; i < list.Count; i++)
                    nodePositions[list[i].Key] = new Vector2(nodeSize / 2 + layer * layerSpace, -edgeMargin - hs * (i + 1));
            }
        }
    }

    void DrawNode(NodeGene node)
    {
        var go = GetPooled(ref nodePool, nodePrefab);
        go.transform.SetParent(networkPanel, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = nodePositions[node.Key];
        rt.sizeDelta = Vector2.one * nodeSize;
        allNodes.Add(go);

        var img = go.GetComponent<Image>();
        nodeImages[node.Key] = img;
        img.color = node.Type == NodeType.Input ? Color.cyan :
                    node.Type == NodeType.Output ? Color.yellow : Color.white;

        var outline = go.GetOrAddComponent<Outline>();
        outline.effectColor = GetBiasColor((float)node.Bias);
        outline.effectDistance = Vector2.one * outlineThickness;

        var label = go.GetComponentInChildren<TextMeshProUGUI>();
        if (label)
        {
            label.text = node.Key.ToString();
            label.fontSize = Mathf.RoundToInt(nodeSize * 0.35f);
        }

        SetupTooltip(go, node.Key);
    }

    void DrawConnection(ConnectionGene conn)
    {
        var go = GetPooled(ref connectionPool, connectionPrefab);
        go.transform.SetParent(networkPanel, false);
        allConnections.Add(go);

        var rt = go.GetComponent<RectTransform>();
        Vector2 start = nodePositions[conn.InputKey];
        Vector2 end = nodePositions[conn.OutputKey];
        rt.anchoredPosition = start;
        Vector2 dir = end - start;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.rotation = Quaternion.Euler(0, 0, angle);
        rt.sizeDelta = new Vector2(dir.magnitude, rt.sizeDelta.y);

        var img = go.GetComponent<Image>();
        float w = Mathf.Clamp01(Mathf.Abs((float)conn.Weight));
        img.color = conn.Weight > 0 ? new Color(0, 1, 0, w) : new Color(1, 0, 0, w);
    }

    void RefreshNodeValues()
    {
        var brain = selectedCreature.GetBrain();
        var vals = (Dictionary<int, double>)brain.GetType()
            .GetField("_nodeValues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(brain);
        if (vals == null) return;

        foreach (var kv in vals)
        {
            if (!nodeImages.TryGetValue(kv.Key, out var img)) continue;
            lastNodeValues[kv.Key] = kv.Value;

            img.color = kv.Value > 0 ? Color.Lerp(Color.white, Color.green, Mathf.Clamp01((float)kv.Value)) :
                        kv.Value < 0 ? Color.Lerp(Color.white, Color.red, Mathf.Clamp01((float)-kv.Value)) :
                        Color.white;
        }
    }

    void SetupTooltip(GameObject go, int id)
    {
        var trigger = go.GetOrAddComponent<EventTrigger>();
        trigger.triggers.Clear();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => ShowNodeTooltip(id));
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => HideTooltip());
        trigger.triggers.Add(exit);
    }

    void ShowNodeTooltip(int id)
    {
        if (activeTooltip) return;
        activeTooltip = Instantiate(tooltipPrefab, networkPanel.parent);
        activeTooltip.transform.SetAsLastSibling();

        var brain = selectedCreature.GetBrain();
        var nodes = (Dictionary<int, NodeGene>)brain.GetType()
            .GetField("_nodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(brain);
        if (!nodes.ContainsKey(id)) return;

        var gene = nodes[id];
        double val = lastNodeValues.ContainsKey(id) ? lastNodeValues[id] : 0;
        string txt = $"#{id} ({gene.Type})\nVal: {val:F2}\nBias: {gene.Bias:F2}";

        var tipText = activeTooltip.GetComponentInChildren<TextMeshProUGUI>();
        if (tipText) tipText.text = txt;

        var tipRT = activeTooltip.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            networkPanel.parent as RectTransform,
            Input.mousePosition,
            null,
            out Vector2 local);
        tipRT.anchoredPosition = local + Vector2.up * (nodeSize + 10);
    }

    void HideTooltip()
    {
        if (activeTooltip) Destroy(activeTooltip);
        activeTooltip = null;
    }

    void ClearVisualization()
    {
        foreach (var go in allNodes) ReturnToPool(go, nodePool);
        foreach (var go in allConnections) ReturnToPool(go, connectionPool);
        allNodes.Clear(); allConnections.Clear();
        nodeImages.Clear(); nodePositions.Clear(); lastNodeValues.Clear();
        HideTooltip();
    }

    void HideNetwork()
    {
        networkPanel.gameObject.SetActive(false);
        selectedCreature = null;
        ClearVisualization();
        creatureStatsPanel?.HideStats();
        cameraController?.ResetCamera();
    }

    GameObject GetPooled(ref Queue<GameObject> pool, GameObject prefab)
    {
        if (pool.Count == 0) return Instantiate(prefab);
        var go = pool.Dequeue();
        go.SetActive(true);
        return go;
    }

    void ReturnToPool(GameObject go, Queue<GameObject> pool)
    {
        go.SetActive(false);
        pool.Enqueue(go);
    }

    Color GetBiasColor(float bias)
    {
        if (bias > 0) return Color.Lerp(Color.white, Color.green, Mathf.Clamp01(bias));
        if (bias < 0) return Color.Lerp(Color.white, Color.red, Mathf.Clamp01(-bias));
        return Color.white;
    }
}
