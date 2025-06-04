using UnityEngine;
using XCharts;
using XCharts.Runtime;

public class CreatureGraph : MonoBehaviour
{

     public LineChart chart;
    [SerializeField] private float updateInterval = 0.1f;
    private float maxSeconds = 30f;
	private float timer = 0f;
	private Serie A_lineSerie;
	private Serie K_lineSerie;
	private XAxis xAxis;
	private YAxis yAxis;
	[SerializeField] private GameObject AlbertPrefab;
	[SerializeField] private Color albertColor;
	[SerializeField] private GameObject KaiPrefab;
	[SerializeField] private Color kaiColor;
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

		// Add a new line series with area fill
		A_lineSerie = chart.AddSerie<Line>("Albert");
		A_lineSerie.EnsureComponent<AreaStyle>().show = true;
		A_lineSerie.symbol.show = false;
		A_lineSerie.lineStyle.color = new Color(1, 0.5f, 0);
		A_lineSerie.areaStyle.color = new Color(1, 0.5f, 0);
		A_lineSerie.areaStyle.opacity = 0.3f;
		
		K_lineSerie = chart.AddSerie<Line>("Kai");
		K_lineSerie.EnsureComponent<AreaStyle>().show = true;
		K_lineSerie.symbol.show = false;
		K_lineSerie.areaStyle.color = new Color(0,0.5f,1,0.5f);
		K_lineSerie.lineStyle.color = new Color(0, 0.5f, 1, 0.5f);
		K_lineSerie.areaStyle.opacity = 0.3f;
		//for (int i = 0; i < maxSeconds; i++)
		//{
		//	chart.AddXAxisData($"{i}s");
		//}

		chart.RefreshChart(); // Initial draw


	}

	void Update()
	{
		timer += Time.deltaTime;
		if (timer >= updateInterval)
		{
			timer = 0f;

			UpdateKaiGraph();
			UpdateAlbertGraph();
			timeCounter++;
		}
	}
	void UpdateAlbertGraph()
	{
		// Count creatures by tag
		Transform parentTransform = ParenthoodManager.GetParent(AlbertPrefab);
		if (parentTransform == null) return;
		int creatureCount = parentTransform.childCount;
		// Keep X-axis and Y-data within time window
		if (A_lineSerie.dataCount >= maxSeconds )
		{
			A_lineSerie.data.RemoveAt(0);
			xAxis.data.RemoveAt(0);
		}

		// Add new data
		chart.AddXAxisData($"{timeCounter}s");
		chart.AddData(0, creatureCount, "Albert");

		// Auto-scale Y-axis to peak value
		float max = Mathf.Max(A_lineSerie.maxCache, K_lineSerie.maxCache, 2f);
		yAxis.max = Mathf.Ceil(max * 1.0f + 10); // Add 10% padding

		chart.RefreshChart(); // Trigger render
	}

	void UpdateKaiGraph()
	{
		// Count creatures by tag
		Transform parentTransform = ParenthoodManager.GetParent(KaiPrefab);
		if (parentTransform == null) return;
		int creatureCount = parentTransform.childCount;
		// Keep X-axis and Y-data within time window
		if (K_lineSerie.dataCount >= maxSeconds)
		{
			K_lineSerie.data.RemoveAt(0);
			xAxis.data.RemoveAt(0);
		}

		// Add new data
		chart.AddXAxisData($"{timeCounter}s");
		chart.AddData(1, creatureCount, "Kai");
		

		// Auto-scale Y-axis to peak value
		float max = Mathf.Max(A_lineSerie.maxCache, K_lineSerie.maxCache, 2f);
		yAxis.max = Mathf.Ceil(max * 1.0f); // Add 10% padding

		chart.RefreshChart(); // Trigger render
	}



}
