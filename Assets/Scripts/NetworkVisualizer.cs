using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using NEAT.Genes;
using TMPro;

public class NetworkVisualizer : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform networkPanel;  // Panel to hold the network visualization
    public GameObject nodePrefab;       // Prefab for network nodes (should be a UI element)
    public GameObject connectionPrefab;  // Prefab for connections (should be a UI line element)
    public CreatureStats creatureStatsPanel; // Reference to the creature stats panel
    
    [Header("Layout Settings")]
    public Vector2 panelOffset = new Vector2(20, 20);  // Offset from top-left corner
    public Vector2 panelSize = new Vector2(300, 200);  // Size of the panel
    public float nodeSize = 30f;        // Size of node circles
    public float edgeMargin = 40f;      // Margin from panel edges
    
    [Header("Visualization Settings")]
    public float outlineThickness = 3f;  // Thickness of the node outline for bias visualization
    
    [Header("Tooltip Settings")]
    public GameObject tooltipPrefab;    // Prefab for the tooltip (can be a UI Text element with background)
    
    private Dictionary<int, Vector2> nodePositions = new Dictionary<int, Vector2>();
    private Dictionary<int, Image> nodeImages = new Dictionary<int, Image>();
    private Dictionary<int, Outline> nodeOutlines = new Dictionary<int, Outline>();
    private List<RectTransform> connectionObjects = new List<RectTransform>();
    public Creature selectedCreature;
    private CameraController cameraController;
    private Dictionary<int, double> lastNodeValues = new Dictionary<int, double>();
    private GameObject activeTooltip;    // Reference to the currently displayed tooltip
    
    void Start()
    {
        // Find camera controller if available
        cameraController = FindObjectOfType<CameraController>();
        
        // Ensure network panel starts hidden
        if (networkPanel != null)
        {
            networkPanel.gameObject.SetActive(false);
        }
        
        // Setup layout - restore original anchoring
        if (networkPanel != null)
        {
            networkPanel.anchorMin = new Vector2(0, 1);  // Anchor to top-left
            networkPanel.anchorMax = new Vector2(0, 1);
            networkPanel.pivot = new Vector2(0, 1);      // Pivot at top-left
            networkPanel.sizeDelta = panelSize;
            networkPanel.anchoredPosition = panelOffset;
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
        {
            HideNetwork();
            if (cameraController != null)
            {
                cameraController.ResetCamera();
            }
        }
        
        // Check for 'S' key to save selected creature
        if (Input.GetKeyDown(KeyCode.S) && selectedCreature != null)
        {
            CreatureSaver.SaveCreature(selectedCreature);
        }
        
        // Update visualization if we have a selected creature
        if (selectedCreature != null && networkPanel != null && networkPanel.gameObject.activeSelf)
        {
            UpdateNodeColors();
        }
    }
    
    void UpdateNodeColors()
    {
        var brain = selectedCreature.GetBrain();
        if (brain == null) return;
        
        // Get the current node values using reflection
        var nodeValuesField = brain.GetType().GetField("_nodeValues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (nodeValuesField == null) return;
        
        var nodeValues = nodeValuesField.GetValue(brain) as Dictionary<int, double>;
        if (nodeValues == null) return;
        
        // Update the color of each node based on its value
        foreach (var kvp in nodeValues)
        {
            int nodeId = kvp.Key;
            double value = kvp.Value;
            
            if (nodeImages.TryGetValue(nodeId, out Image image))
            {
                // Store the value for debugging
                lastNodeValues[nodeId] = value;
                
                // Convert value to color (green for positive, red for negative, white for zero)
                float absValue = Mathf.Abs((float)value);
                if (value > 0)
                {
                    image.color = Color.Lerp(Color.white, Color.green, Mathf.Min(1, absValue));
                }
                else if (value < 0)
                {
                    image.color = Color.Lerp(Color.white, Color.red, Mathf.Min(1, absValue));
                }
                else
                {
                    image.color = Color.white;
                }
                
                // Update tooltip if it's showing this node
                if (activeTooltip != null)
                {
                    // Get the node being displayed in tooltip
                    var eventTrigger = image.GetComponent<EventTrigger>();
                    if (eventTrigger != null && eventTrigger.triggers.Count > 0)
                    {
                        var rect = image.GetComponent<RectTransform>();
                        if (rect != null)
                        {
                            // Check if mouse is over this node
                            Vector2 mousePos = Input.mousePosition;
                            if (RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos))
                            {
                                ShowNodeTooltip(nodeId, rect);
                            }
                        }
                    }
                }
            }
        }
    }
    
    void SelectCreature(Creature creature)
    {
        selectedCreature = creature;
        ShowNetwork();
        
        // Set camera to follow the selected creature
        if (cameraController != null)
        {
            cameraController.SetTarget(creature.transform);
        }
        
        // Show creature stats panel if available
        if (creatureStatsPanel != null)
        {
            creatureStatsPanel.ShowStats(creature);
        }
    }
    
    void ShowNetwork()
    {
        if (selectedCreature == null || networkPanel == null) return;
        
        // Clear existing visualization
        ClearVisualization();
        
        // Show the panel
        networkPanel.gameObject.SetActive(true);
        
        // Get the network structure using reflection
        var brain = selectedCreature.GetBrain();
        if (brain == null)
        {
            Debug.LogWarning("Selected creature has no brain!");
            return;
        }
        
        var nodes = brain.GetType().GetField("_nodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(brain) as Dictionary<int, NodeGene>;
        var connections = brain.GetType().GetField("_connections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(brain) as Dictionary<int, ConnectionGene>;
        
        if (nodes == null || connections == null)
        {
            Debug.LogWarning("Could not get nodes or connections from brain!");
            return;
        }
        
        // Calculate layout
        CalculateNodePositions(nodes, connections);
        
        // Draw connections first (so they're behind nodes)
        foreach (var conn in connections.Values)
        {
            if (!conn.Enabled) continue;
            DrawConnection(conn);
        }
        
        // Draw nodes
        foreach (var node in nodes.Values)
        {
            DrawNode(node);
        }
    }
    
    void CalculateNodePositions(Dictionary<int, NodeGene> nodes, Dictionary<int, ConnectionGene> connections)
    {
        if (nodes == null || nodes.Count == 0) return;
        
        nodePositions.Clear();
        
        float panelWidth = networkPanel.sizeDelta.x - edgeMargin * 2;
        float panelHeight = networkPanel.sizeDelta.y;
        
        List<NodeGene> inputNodes = new List<NodeGene>();
        List<NodeGene> outputNodes = new List<NodeGene>();
        Dictionary<int, List<NodeGene>> hiddenNodes = new Dictionary<int, List<NodeGene>>();
        int maxHiddenLayer = 0;
        
        foreach (var node in nodes.Values)
        {
            switch (node.Type)
            {
                case NodeType.Input:
                    inputNodes.Add(node);
                    break;
                case NodeType.Hidden:
                    if (!hiddenNodes.ContainsKey(node.Layer))
                        hiddenNodes[node.Layer] = new List<NodeGene>();
                    hiddenNodes[node.Layer].Add(node);
                    maxHiddenLayer = Mathf.Max(maxHiddenLayer, node.Layer);
                    break;
                case NodeType.Output:
                    outputNodes.Add(node);
                    break;
            }
        }
        
        // Calculate vertical spacing
        float verticalSpace = panelHeight - edgeMargin;  // Only subtract top margin
        
        // Position input nodes on the left
        float inputSpacing = verticalSpace / (inputNodes.Count + 1);
        for (int i = 0; i < inputNodes.Count; i++)
        {
            var node = inputNodes[i];
            nodePositions[node.Key] = new Vector2(
                nodeSize/2,  // Half node size from left edge
                -edgeMargin - inputSpacing * (i + 1)
            );
        }
        
        // Position output nodes on the right
        float outputSpacing = verticalSpace / (outputNodes.Count + 1);
        for (int i = 0; i < outputNodes.Count; i++)
        {
            var node = outputNodes[i];
            nodePositions[node.Key] = new Vector2(
                panelWidth - nodeSize/2,  // Half node size from right edge
                -edgeMargin - outputSpacing * (i + 1)
            );
        }
        
        // Position hidden nodes in layers
        if (maxHiddenLayer > 0)
        {
            float layerSpacing = (panelWidth - nodeSize) / (maxHiddenLayer + 1);
            
            // For each layer
            for (int layer = 1; layer <= maxHiddenLayer; layer++)
            {
                float layerX = nodeSize/2 + layer * layerSpacing;
                
                // Position hidden nodes in this layer
                var layerNodes = hiddenNodes.ContainsKey(layer) ? hiddenNodes[layer] : new List<NodeGene>();
                float nodeSpacing = verticalSpace / (layerNodes.Count + 1);
                for (int i = 0; i < layerNodes.Count; i++)
                {
                    var node = layerNodes[i];
                    nodePositions[node.Key] = new Vector2(
                        layerX,
                        -edgeMargin - nodeSpacing * (i + 1)
                    );
                }
            }
        }
    }
    
    void DrawNode(NodeGene node)
    {
        if (!nodePositions.ContainsKey(node.Key) || nodePrefab == null) return;
        
        GameObject nodeObj = Instantiate(nodePrefab, networkPanel);
        RectTransform rect = nodeObj.GetComponent<RectTransform>();
        rect.anchoredPosition = nodePositions[node.Key];
        rect.sizeDelta = new Vector2(nodeSize, nodeSize);
        
        // Store the image component for later updates
        Image image = nodeObj.GetComponent<Image>();
        if (image != null)
        {
            nodeImages[node.Key] = image;
            
            // Set initial color based on node type
            switch (node.Type)
            {
                case NodeType.Input:
                    image.color = Color.cyan;
                    break;
                case NodeType.Output:
                    image.color = Color.yellow;
                    break;
                default:
                    image.color = Color.white;
                    break;
            }
        }
        
        // Add node ID text
        Text text = nodeObj.GetComponentInChildren<Text>();
        if (text != null)
        {
            // For output nodes, add a descriptive label
            if (node.Type == NodeType.Output)
            {
                string label = node.Key.ToString();
                
                // Assuming the output nodes are numbered 17, 18, 19, 20, 21 based on the Creature.cs code
                // Adjust these numbers if your network uses different output node IDs
                switch (label) //TODO: this seems like remnant code, examine and if so remove it
                {
                    case "17": label = "17: X Move"; break;
                    case "18": label = "18: Y Move"; break;
                    case "19": label = "19: Chop"; break;
                    case "20": label = "20: Attack"; break;
                    case "21": label = "21: Reproduce"; break;
                }
                
                text.text = label;
                text.fontSize = Mathf.RoundToInt(nodeSize * 0.3f); // Smaller font for longer text
            }
            else
            {
                text.text = node.Key.ToString();
                text.fontSize = Mathf.RoundToInt(nodeSize * 0.4f);
            }
        }
        
        // Add or get outline component for bias visualization
        Outline outline = nodeObj.GetComponent<Outline>();
        if (outline == null)
        {
            outline = nodeObj.AddComponent<Outline>();
        }
        
        nodeOutlines[node.Key] = outline;
        
        // Configure outline
        outline.effectColor = GetBiasColor((float)node.Bias);
        outline.effectDistance = new Vector2(outlineThickness, outlineThickness);
        outline.useGraphicAlpha = false;
        
        // Add event trigger component for hover tooltips
        EventTrigger eventTrigger = nodeObj.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = nodeObj.AddComponent<EventTrigger>();
        }
        
        // Add pointer enter event (hover)
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((eventData) => { ShowNodeTooltip(node.Key, rect); });
        eventTrigger.triggers.Add(enterEntry);
        
        // Add pointer exit event (hover end)
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((eventData) => { HideTooltip(); });
        eventTrigger.triggers.Add(exitEntry);
    }
    
    // Helper method to convert bias value to color
    Color GetBiasColor(float bias)
    {
        // Green for positive bias, red for negative, white for zero
        if (bias > 0)
        {
            return Color.Lerp(Color.white, Color.green, Mathf.Min(1, Mathf.Abs(bias)));
        }
        else if (bias < 0)
        {
            return Color.Lerp(Color.white, Color.red, Mathf.Min(1, Mathf.Abs(bias)));
        }
        else
        {
            return Color.white;
        }
    }
    
    void DrawConnection(ConnectionGene connection)
    {
        if (!nodePositions.ContainsKey(connection.InputKey) || 
            !nodePositions.ContainsKey(connection.OutputKey) || 
            connectionPrefab == null) return;
        
        GameObject connObj = Instantiate(connectionPrefab, networkPanel);
        RectTransform rect = connObj.GetComponent<RectTransform>();
        connectionObjects.Add(rect);
        
        // Get start and end positions
        Vector2 startPos = nodePositions[connection.InputKey];
        Vector2 endPos = nodePositions[connection.OutputKey];
        
        // Position and rotate line to connect nodes
        rect.anchoredPosition = startPos;
        Vector2 direction = endPos - startPos;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rect.rotation = Quaternion.Euler(0, 0, angle);
        rect.sizeDelta = new Vector2(direction.magnitude, rect.sizeDelta.y);
        
        // Set connection color based on weight
        Image image = connObj.GetComponent<Image>();
        if (image != null)
        {
            float weight = (float)connection.Weight;
            if (weight > 0)
                image.color = new Color(0, 1, 0, Mathf.Min(1, weight));
            else
                image.color = new Color(1, 0, 0, Mathf.Min(1, -weight));
        }
    }
    
    void ShowNodeTooltip(int nodeId, RectTransform nodeRect)
    {
        // Create tooltip if it doesn't exist
        if (activeTooltip == null && tooltipPrefab != null)
        {
            // Create the tooltip under the canvas
            Canvas canvas = networkPanel.GetComponentInParent<Canvas>();
            activeTooltip = Instantiate(tooltipPrefab, canvas.transform);
        }

        if (activeTooltip != null)
        {
            // Get the node gene to access bias
            var brain = selectedCreature.GetBrain();
            if (brain == null) return;
            
            var nodesField = brain.GetType().GetField("_nodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (nodesField == null) return;
            
            var nodes = nodesField.GetValue(brain) as Dictionary<int, NodeGene>;
            if (nodes == null || !nodes.ContainsKey(nodeId)) return;
            
            NodeGene node = nodes[nodeId];
            
            // Get the current node value (if available) and round to 2 decimal places
            string valueText = "No value";
            if (lastNodeValues.TryGetValue(nodeId, out double value))
            {
                float roundedValue = Mathf.Round((float)value * 100f) / 100f;
                valueText = $"Value: {roundedValue:F2}";
            }
            
            // Set the tooltip text with NodeTooltip component
            NodeTooltip tooltipComponent = activeTooltip.GetComponent<NodeTooltip>();
            if (tooltipComponent != null)
            {
                string nodeType = node.Type.ToString();
                float roundedBias = Mathf.Round((float)node.Bias * 100f) / 100f;
                string biasText = $"Bias: {roundedBias:F2}";
                tooltipComponent.SetTooltipText($"Node {nodeId} ({nodeType})\n{valueText}\n{biasText}");
            }
            else
            {
                // Fallback to standard Text component if NodeTooltip not found
                TextMeshProUGUI tooltipText = activeTooltip.GetComponentInChildren<TextMeshProUGUI>();
                if (tooltipText != null)
                {
                    string nodeType = node.Type.ToString();
                    float roundedBias = Mathf.Round((float)node.Bias * 100f) / 100f;
                    string biasText = $"Bias: {roundedBias:F2}";
                    tooltipText.text = $"Node {nodeId} ({nodeType})\n{valueText}\n{biasText}";
                }
            }
        }
    }
    
    void HideTooltip()
    {
        if (activeTooltip != null)
        {
            Destroy(activeTooltip);
            activeTooltip = null;
        }
    }
    
    void ClearVisualization()
    {
        // Clear all node and connection objects
        foreach (Transform child in networkPanel)
        {
            Destroy(child.gameObject);
        }
        
        nodePositions.Clear();
        nodeImages.Clear();
        nodeOutlines.Clear();
        connectionObjects.Clear();
        lastNodeValues.Clear();
        
        // Ensure tooltip is destroyed
        HideTooltip();
    }
    
    void HideNetwork()
    {
        if (networkPanel != null)
        {
            networkPanel.gameObject.SetActive(false);
            selectedCreature = null;
            ClearVisualization();
            
            // Hide creature stats panel if available
            if (creatureStatsPanel != null)
            {
                creatureStatsPanel.HideStats();
            }
        }
    }
} 