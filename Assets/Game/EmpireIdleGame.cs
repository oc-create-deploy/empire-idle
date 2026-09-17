using UnityEngine;

public sealed class EmpireIdleGame : MonoBehaviour
{
    private double coins;
    private int workers = 1;
    private int factories = 1;
    private int prestige;
    private GUIStyle titleStyle;
    private GUIStyle valueStyle;
    private GUIStyle bodyStyle;
    private GUIStyle buttonStyle;

    private double IncomePerSecond => workers * (2.0 + prestige) + factories * (12.0 + prestige * 3.0);

    private void Awake()
    {
        Application.targetFrameRate = 60;
        coins = PlayerPrefs.GetFloat("coins", 25f);
        workers = PlayerPrefs.GetInt("workers", 1);
        factories = PlayerPrefs.GetInt("factories", 1);
        prestige = PlayerPrefs.GetInt("prestige", 0);
    }

    private void Update()
    {
        coins += IncomePerSecond * Time.deltaTime;
        if (Time.frameCount % 300 == 0) Save();
    }

    private void Save()
    {
        PlayerPrefs.SetFloat("coins", (float)coins);
        PlayerPrefs.SetInt("workers", workers);
        PlayerPrefs.SetInt("factories", factories);
        PlayerPrefs.SetInt("prestige", prestige);
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) Save();
    }

    private void InitStyles(float scale)
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(42 * scale), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        titleStyle.normal.textColor = new Color(1f, 0.82f, 0.22f);
        valueStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(28 * scale), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        valueStyle.normal.textColor = Color.white;
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(18 * scale), alignment = TextAnchor.MiddleCenter };
        bodyStyle.normal.textColor = new Color(0.75f, 0.86f, 1f);
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(20 * scale), fontStyle = FontStyle.Bold };
        buttonStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        float scale = Mathf.Min(Screen.width / 430f, Screen.height / 932f);
        InitStyles(scale);
        float width = 390f * scale;
        float left = (Screen.width - width) * 0.5f;

        Color old = GUI.color;
        GUI.color = new Color(0.03f, 0.08f, 0.18f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        GUI.Label(new Rect(left, 42 * scale, width, 70 * scale), "EMPIRE IDLE", titleStyle);
        GUI.Label(new Rect(left, 118 * scale, width, 52 * scale), "$" + Format(coins), valueStyle);
        GUI.Label(new Rect(left, 166 * scale, width, 34 * scale), Format(IncomePerSecond) + " per second", bodyStyle);

        DrawCard(left, 225 * scale, width, 155 * scale, "WORKFORCE", workers, WorkerCost(), "Hire worker", () =>
        {
            double cost = WorkerCost();
            if (coins >= cost) { coins -= cost; workers++; }
        });

        DrawCard(left, 405 * scale, width, 155 * scale, "FACTORIES", factories, FactoryCost(), "Build factory", () =>
        {
            double cost = FactoryCost();
            if (coins >= cost) { coins -= cost; factories++; }
        });

        if (GUI.Button(new Rect(left, 590 * scale, width, 92 * scale), "COLLECT  +" + Format(10 + factories * 4), buttonStyle))
            coins += 10 + factories * 4;

        double prestigeCost = 2500.0 * (prestige + 1) * (prestige + 1);
        GUI.Label(new Rect(left, 710 * scale, width, 40 * scale), "Prestige " + prestige + "  •  next at $" + Format(prestigeCost), bodyStyle);
        GUI.enabled = coins >= prestigeCost;
        if (GUI.Button(new Rect(left, 760 * scale, width, 70 * scale), "EXPAND THE EMPIRE", buttonStyle))
        {
            prestige++;
            coins = 100;
            workers = 1;
            factories = 1;
            Save();
        }
        GUI.enabled = true;
    }

    private void DrawCard(float x, float y, float width, float height, string label, int count, double cost, string action, System.Action onClick)
    {
        GUI.Box(new Rect(x, y, width, height), string.Empty);
        GUI.Label(new Rect(x + 16, y + 10, width - 32, 38), label + "  " + count, valueStyle);
        GUI.Label(new Rect(x + 16, y + 52, width - 32, 28), "Upgrade cost: $" + Format(cost), bodyStyle);
        GUI.enabled = coins >= cost;
        if (GUI.Button(new Rect(x + 28, y + 91, width - 56, 50), action, buttonStyle)) onClick();
        GUI.enabled = true;
    }

    private double WorkerCost() => 25.0 * System.Math.Pow(1.18, workers);
    private double FactoryCost() => 160.0 * System.Math.Pow(1.24, factories);

    private static string Format(double value)
    {
        if (value >= 1000000000) return (value / 1000000000d).ToString("0.0") + "B";
        if (value >= 1000000) return (value / 1000000d).ToString("0.0") + "M";
        if (value >= 1000) return (value / 1000d).ToString("0.0") + "K";
        return value.ToString("0");
    }
}
