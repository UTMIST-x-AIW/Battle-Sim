using System.Collections.Generic;
using UnityEngine;
using XCharts;
using XCharts.Runtime;

public class PopulationGraphing : MonoBehaviour
{

    public LineChart chart;
    [SerializeField][Range(0,1)]
	private float updateInterval = 0.1f;
    public float maxSeconds = 30f;
	private float timer = 0f;

	[SerializeField] private List<GraphInfo> graphs = new();

	private XAxis xAxis;
	private YAxis yAxis;
	private int timeCounter = 0;

	void Start()

    {
		chart.RemoveData();
		xAxis = chart.EnsureChartComponent<XAxis>();
		xAxis.type = Axis.AxisType.Category;
		xAxis.boundaryGap = false;

		yAxis = chart.EnsureChartComponent<YAxis>();
		yAxis.type = Axis.AxisType.Value;
		yAxis.min = 0;

		foreach (GraphInfo graph in graphs){
			graph.serie = chart.AddSerie<Line>(graph.Name);
			graph.serie.EnsureComponent<AreaStyle>().show = true;
			graph.serie.symbol.show = false;
		}
		
		chart.RefreshChart();

	}

	void Update()
	{
		timer += Time.deltaTime;
		if (timer >= updateInterval)
		{
			timer = 0f;
			for (int i =0; i < graphs.Count; i++) 
			{
				UpdateGraph(graphs[i], i, graphs[i].color);
			}

			timeCounter++;
		}
	}

	void UpdateGraph(GraphInfo info, int index, Color color){
		// Count creatures by tag
		var creatureCount = GetChildCount(info);
		// Keep X-axis and Y-data within time window
		if (info.serie.dataCount >= maxSeconds)
		{
			info.serie.data.RemoveAt(0);
			xAxis.data.RemoveAt(0);
		}
		Color color1 = color;

		info.serie.lineStyle.color = new Color(color.r,color.g,color.b);
		info.serie.areaStyle.color = new Color(color.r, color.g, color.b);
		info.serie.areaStyle.opacity = 0.5f;

		// Add new data
		chart.AddXAxisData($"{timeCounter}s");
		chart.AddData(index, creatureCount, info.Name);

		// Auto-scale Y-axis to peak value
		float max = Mathf.Max(info.serie.maxCache, 2f);
		yAxis.max = Mathf.Ceil(max * 1.0f + 10); // Add 10% padding

		chart.RefreshChart(); // Trigger render
	}

	private static int GetChildCount(GraphInfo info)
	{
		Transform parentTransform = ParenthoodManager.GetParent(info.prefab);
		if (parentTransform == null) return 0;
		int creatureCount = parentTransform.childCount;
		return creatureCount;
	}
}