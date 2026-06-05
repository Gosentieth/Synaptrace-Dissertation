using Synaptrace.Core;
using Synaptrace.Telemetry;
using UnityEngine;

namespace Synaptrace.UI
{
    public sealed class PrototypeHUD : MonoBehaviour
    {
        [SerializeField] private bool showTelemetryPanel = true;

        private const float PanelMargin = 14f;
        private const float PanelWidth = 372f;
        private const float PanelHeight = 224f;
        private const float ButtonWidth = 142f;
        private const float ButtonHeight = 28f;

        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle smallLabelStyle;
        private GUIStyle buttonStyle;
        private Texture2D panelTexture;
        private Texture2D buttonTexture;
        private string statusMessage = "Reach the finish zone.";

        public void SetStatus(string message)
        {
            statusMessage = message;
        }

        private void OnGUI()
        {
            if (!showTelemetryPanel)
            {
                return;
            }

            EnsureStyles();

            LevelTelemetrySnapshot snapshot = GameManager.Instance != null
                ? GameManager.Instance.GetTelemetrySnapshot()
                : new LevelTelemetrySnapshot();

            GUILayout.BeginArea(new Rect(PanelMargin, PanelMargin, PanelWidth, PanelHeight), panelStyle);
            GUILayout.Label("Synaptrace Prototype", titleStyle);
            GUILayout.Space(4f);
            GUILayout.Label("Status: " + statusMessage, labelStyle);
            GUILayout.Label("Time: " + snapshot.elapsedTime.ToString("0.00") + "s", labelStyle);
            GUILayout.Label("Deaths: " + snapshot.deathCount + "    Retries: " + snapshot.retryCount + "    Jumps: " + snapshot.jumpCount, labelStyle);
            GUILayout.Label("Hazard hits: " + snapshot.hazardHitCount + "    Completed: " + snapshot.completed, labelStyle);
            GUILayout.Label("Controls: A/D or arrows, Space/W/Up, wall jump, R", smallLabelStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Restart Level", buttonStyle, GUILayout.Width(ButtonWidth), GUILayout.Height(ButtonHeight)) && LevelManager.Instance != null)
            {
                LevelManager.Instance.RestartLevelFromInput();
            }

            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            panelTexture = CreateTexture(new Color(0.015f, 0.026f, 0.04f, 0.84f));
            buttonTexture = CreateTexture(new Color(0.08f, 0.32f, 0.38f, 0.92f));

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(14, 14, 12, 12),
                normal = { background = panelTexture }
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.68f, 1f, 0.96f, 1f) }
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.92f, 0.98f, 1f, 1f) }
            };

            smallLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.72f, 0.85f, 0.92f, 1f) }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = buttonTexture, textColor = Color.white },
                hover = { background = buttonTexture, textColor = new Color(0.68f, 1f, 0.96f, 1f) },
                active = { background = buttonTexture, textColor = new Color(1f, 0.92f, 0.32f, 1f) }
            };
        }

        private Texture2D CreateTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
