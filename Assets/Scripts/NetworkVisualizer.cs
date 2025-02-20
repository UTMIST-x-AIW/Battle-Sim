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
    private List<RectTransform> connectionObjects = new List<RectTransform>();
    private Creature selectedCreature;
    private CameraController cameraController;
    
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
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
            
            if (hit.collider != null)
            {
                Creature creature = hit.collider.GetComponent<Creature>();
                if (creature != null)
                {
                    Debug.Log($"Selected creature of type: {creature.type}");
                    SelectCreature(creature);
                }
            }
            else
            {
                // Click on empty space, hide the panel and reset camera
                HideNetwork();
                if (cameraController != null)
                {
                    cameraController.ResetCamera();
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
        
        // Group nodes by type
        var inputNodes = new List<NodeGene>();
        var hiddenNodes = new List<NodeGene>();
        var outputNodes = new List<NodeGene>();
        
        foreach (var node in nodes.Values)
        {
            switch (node.Type)
            {
                case NodeType.Input: inputNodes.Add(node); break;
                case NodeType.Hidden: hiddenNodes.Add(node); break;
                case NodeType.Output: outputNodes.Add(node); break;
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
        
        // Position hidden nodes in the middle
        if (hiddenNodes.Count > 0)
        {
            float hiddenSpacing = verticalSpace / (hiddenNodes.Count + 1);
            for (int i = 0; i < hiddenNodes.Count; i++)
            {
                var node = hiddenNodes[i];
                nodePositions[node.Key] = new Vector2(
                    panelWidth / 2,
                    -edgeMargin - hiddenSpacing * (i + 1)
                );
            }
        }
    }
    
    void DrawNode(NodeGene node)
    {
        if (!nodePositions.ContainsKey(node.Key) || nodePrefab == null) return;
        
        GameObject nodeObj = Instantiate(nodePrefab, networkPanel);
        RectTransform rect = nodeObj.GetComponent<RectTransform>();
        rect.anchoredPosition = nodePositions[node.Key];
        rect.sizeDelta = new Vector2(nodeSize, nodeSize);  // Set node size
        
        // Set node color based on type
        Image image = nodeObj.GetComponent<Image>();
        if (image != null)
        {
            switch (node.Type)
            {
                case NodeType.Input:
                    image.color = new Color(0.7f, 0.9f, 1f);
                    break;
                case NodeType.Output:
                    image.color = new Color(0.7f, 1f, 0.7f);
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
            text.fontSize = Mathf.RoundToInt(nodeSize * 0.4f);  // Scale font size with node
        }
        
        Debug.Log($"Drew node {node.Key} of type {node.Type} at position {rect.anchoredPosition}");
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
        connectionObjects.Clear();
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