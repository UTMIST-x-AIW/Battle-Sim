using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using NEAT.Genes;

public class NetworkVisualizer : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform networkPanel;  // Panel to hold the network visualization
    public GameObject nodePrefab;       // Prefab for network nodes (should be a UI element)
    public GameObject connectionPrefab;  // Prefab for connections (should be a UI line element)
    
    [Header("Layout Settings")]
    public Vector2 panelOffset = new Vector2(20, 20);  // Offset from top-left corner
    public Vector2 panelSize = new Vector2(300, 200);  // Size of the panel
    public float nodeSize = 30f;        // Size of node circles
    public float edgeMargin = 40f;      // Margin from panel edges
    
    private Dictionary<int, Vector2> nodePositions = new Dictionary<int, Vector2>();
    private Dictionary<int, Image> nodeImages = new Dictionary<int, Image>();
    private List<RectTransform> connectionObjects = new List<RectTransform>();
    private Creature selectedCreature;
    private CameraController cameraController;
    private Dictionary<int, double> lastNodeValues = new Dictionary<int, double>();
    
    void Start()
    {
        // Hide panel initially
        if (networkPanel != null)
        {
            networkPanel.gameObject.SetActive(false);
            
            // Set panel position and size
            networkPanel.anchorMin = new Vector2(0, 1);  // Anchor to top-left
            networkPanel.anchorMax = new Vector2(0, 1);
            networkPanel.pivot = new Vector2(0, 1);      // Pivot at top-left
            networkPanel.anchoredPosition = panelOffset;
            networkPanel.sizeDelta = panelSize;
        }
        
        // Get camera controller
        cameraController = Camera.main.GetComponent<CameraController>();
        if (cameraController == null)
        {
            cameraController = Camera.main.gameObject.AddComponent<CameraController>();
        }
    }
    
    void Update()
    {
        // Check for mouse click
        if (Input.GetMouseButtonDown(0))
        {
            // Cast a ray from the camera to the mouse position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction);
            
            bool foundCreature = false;
            foreach (var hit in hits)
            {
                Creature creature = hit.collider.GetComponent<Creature>();
                if (creature != null)
                {
                    Debug.Log($"Selected creature of type: {creature.type}");
                    SelectCreature(creature);
                    foundCreature = true;
                    break;
                }
            }
            
            // If we didn't click on a creature, hide the network and reset camera
            if (!foundCreature)
            {
                HideNetwork();
                if (cameraController != null)
                {
                    cameraController.ResetCamera();
                }
            }
        }
        
        // Update node colors if we have a selected creature
        if (selectedCreature != null && networkPanel.gameObject.activeSelf)
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
        
        Debug.Log($"Visualizing network with {nodes.Count} nodes and {connections.Count} connections");
        
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
        float panelWidth = panelSize.x;
        float panelHeight = panelSize.y;
        
        // Group nodes by type and layer
        var inputNodes = new List<NodeGene>();
        var hiddenNodes = new Dictionary<int, List<NodeGene>>();  // Layer -> nodes
        var biasNodes = new Dictionary<int, NodeGene>();  // Layer -> bias node
        var outputNodes = new List<NodeGene>();
        
        // Find max layer for hidden nodes
        int maxHiddenLayer = 1;
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
                case NodeType.Bias:
                    biasNodes[node.Layer] = node;
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
        
        // Position hidden and bias nodes in layers
        if (maxHiddenLayer > 0)
        {
            float layerSpacing = (panelWidth - nodeSize) / (maxHiddenLayer + 1);
            
            // For each layer
            for (int layer = 1; layer <= maxHiddenLayer; layer++)
            {
                float layerX = nodeSize/2 + layer * layerSpacing;
                
                // Position hidden nodes in this layer
                var layerNodes = hiddenNodes.ContainsKey(layer) ? hiddenNodes[layer] : new List<NodeGene>();
                float nodeSpacing = verticalSpace / (layerNodes.Count + 2);  // +2 to leave space for bias node
                for (int i = 0; i < layerNodes.Count; i++)
                {
                    var node = layerNodes[i];
                    nodePositions[node.Key] = new Vector2(
                        layerX,
                        -edgeMargin - nodeSpacing * (i + 1)
                    );
                }
                
                // Position bias node at the top of the layer if it exists
                if (biasNodes.ContainsKey(layer))
                {
                    var biasNode = biasNodes[layer];
                    nodePositions[biasNode.Key] = new Vector2(
                        layerX,
                        -edgeMargin  // At the top of the layer
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
                case NodeType.Bias:
                    image.color = new Color(1f, 0.7f, 0.7f);  // Light pink for bias nodes
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
            text.text = node.Key.ToString();
            text.fontSize = Mathf.RoundToInt(nodeSize * 0.4f);
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
        
        Debug.Log($"Drew connection from node {connection.InputKey} to {connection.OutputKey} with weight {connection.Weight}");
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
        connectionObjects.Clear();
        lastNodeValues.Clear();
    }
    
    void HideNetwork()
    {
        if (networkPanel != null)
        {
            networkPanel.gameObject.SetActive(false);
            selectedCreature = null;
            ClearVisualization();
        }
    }
} 