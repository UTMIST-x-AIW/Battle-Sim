using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class NeuralNetworkVisualizer : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset neuralNetworkUXML;
    [SerializeField] private StyleSheet neuralNetworkUSS;

    private UIDocument uiDocument;
    private VisualElement networkGraph;
    private Dictionary<int, VisualElement> nodes = new Dictionary<int, VisualElement>();
    private List<VisualElement> edges = new List<VisualElement>();

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            uiDocument = gameObject.AddComponent<UIDocument>();
        }

        // Initialize UI
        InitializeUI();
    }

    private void InitializeUI()
    {
        uiDocument.visualTreeAsset = neuralNetworkUXML;
        uiDocument.panelSettings = Resources.Load<PanelSettings>("DefaultPanelSettings");

        var root = uiDocument.rootVisualElement;
        root.styleSheets.Add(neuralNetworkUSS);

        networkGraph = root.Q<VisualElement>("NetworkGraph");
    }

    public void UpdateNetworkVisualization(NeuralNetworkData networkData)
    {
        // Clear previous visualization
        networkGraph.Clear();
        nodes.Clear();
        edges.Clear();

        // Calculate layout parameters
        int inputCount = networkData.InputNodes.Length;
        int hiddenCount = networkData.HiddenLayers.Length;
        int outputCount = networkData.OutputNodes.Length;

        float totalHeight = networkGraph.resolvedStyle.height;
        float totalWidth = networkGraph.resolvedStyle.width;

        float layerSpacing = totalWidth / 4f;
        float verticalPadding = 20f;

        // Draw input layer
        DrawLayer(networkData.InputNodes, layerSpacing, totalHeight, verticalPadding, "input");

        // Draw hidden layers
        for (int i = 0; i < networkData.HiddenLayers.Length; i++)
        {
            DrawLayer(networkData.HiddenLayers[i], (i + 1) * layerSpacing, totalHeight, verticalPadding, "hidden");
        }

        // Draw output layer
        DrawLayer(networkData.OutputNodes, (networkData.HiddenLayers.Length + 1) * layerSpacing, totalHeight, verticalPadding, "output");

        // Draw connections
        foreach (var connection in networkData.Connections)
        {
            DrawConnection(connection.FromId, connection.ToId, connection.Weight);
        }
    }

    private void DrawLayer(NeuralNode[] nodes, float xPosition, float totalHeight, float verticalPadding, string layerType)
    {
        float availableHeight = totalHeight - (2 * verticalPadding);
        float spacing = availableHeight / (nodes.Length + 1);

        for (int i = 0; i < nodes.Length; i++)
        {
            float yPosition = verticalPadding + (i + 1) * spacing;
            DrawNode(nodes[i], xPosition, yPosition, layerType);
        }
    }

    private void DrawNode(NeuralNode node, float x, float y, string layerType)
    {
        var nodeElement = new VisualElement();
        nodeElement.AddToClassList("node");

        // Adjust color based on layer type
        switch (layerType)
        {
            case "input":
                nodeElement.style.backgroundColor = new Color(0.2f, 0.6f, 1f); // Blue
                break;
            case "hidden":
                nodeElement.style.backgroundColor = new Color(0.9f, 0.7f, 0.1f); // Yellow
                break;
            case "output":
                nodeElement.style.backgroundColor = new Color(1f, 0.3f, 0.3f); // Red
                break;
        }

        // Position the node
        nodeElement.style.left = x - 15; // Center the node
        nodeElement.style.top = y - 15; // Center the node

        // Add node value as text if desired
        var label = new Label(node.Value.ToString("F2"));
        nodeElement.Add(label);

        networkGraph.Add(nodeElement);
        nodes.Add(node.Id, nodeElement);
    }

    private void DrawConnection(int fromId, int toId, float weight)
    {
        if (!nodes.ContainsKey(fromId) || !nodes.ContainsKey(toId)) return;

        var fromNode = nodes[fromId];
        var toNode = nodes[toId];

        float x1 = fromNode.worldBound.center.x;
        float y1 = fromNode.worldBound.center.y;
        float x2 = toNode.worldBound.center.x;
        float y2 = toNode.worldBound.center.y;

        float length = Mathf.Sqrt(Mathf.Pow(x2 - x1, 2) + Mathf.Pow(y2 - y1, 2));
        float angle = Mathf.Atan2(y2 - y1, x2 - x1) * Mathf.Rad2Deg;

        var edge = new VisualElement();
        edge.AddToClassList("edge");

        // Set edge properties based on weight
        edge.style.width = length;
        edge.style.height = Mathf.Clamp(Mathf.Abs(weight) * 2f, 0.5f, 4f);
        edge.style.left = x1;
        edge.style.top = y1;
        edge.style.rotate = new Rotate(angle);

        // Color based on weight sign
        edge.style.backgroundColor = weight > 0 ?
            new Color(0.2f, 0.8f, 0.2f, 0.7f) : // Green for positive
            new Color(0.8f, 0.2f, 0.2f, 0.7f);   // Red for negative

        networkGraph.Add(edge);
        edges.Add(edge);
    }
}

// Example data structure - replace with your actual neural network implementation
public class NeuralNetworkData
{
    public NeuralNode[] InputNodes;
    public NeuralNode[][] HiddenLayers;
    public NeuralNode[] OutputNodes;
    public NeuralConnection[] Connections;
}

public class NeuralNode
{
    public int Id;
    public float Value;
    // Add other node properties as needed
}

public class NeuralConnection
{
    public int FromId;
    public int ToId;
    public float Weight;
}