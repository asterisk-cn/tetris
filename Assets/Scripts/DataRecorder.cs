using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
#endif

namespace GameDataAnalysis
{
    /// <summary>
    /// ゲームプレイデータを記録し、JSON形式で圧縮出力するコンポーネント
    /// </summary>
    public class DataRecorder : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("データ記録間隔（秒）")]
        public float recordInterval = 0.1f;

        [Tooltip("自動保存を有効化")]
        public bool autoSave = true;

        [Tooltip("出力ディレクトリ")]
        public string outputDirectory = "GameData";

        [Header("Unity Recorder（Editor Only）")]
        [Tooltip("Game View を MP4 で記録する。Player ビルドでは無効（API はコンパイルされません）。")]
        public bool enableUnityMovieRecorder;

        [Tooltip("動画のフレームレート")]
        public float unityMovieFrameRate = 30f;

        public int unityMovieWidth = 1280;
        public int unityMovieHeight = 720;

        [Tooltip("MP4 にゲーム音声を含める（コーデックが対応する場合）")]
        public bool unityMovieCaptureAudio = true;

        private SessionData sessionData;
        private float nextRecordTime;
        private float sessionStartTime;
        private bool isRecording;
        public string sessionId;

        public delegate Dictionary<string, object> ValueGetter();

        public ValueGetter GetValue;

        public static DataRecorder Instance { get; private set; }

#if UNITY_EDITOR
        RecorderController unityRecorderController;
#endif

        void Awake()
        {
            Instance = this;
            StartRecording();
        }

        /// <summary>
        /// 記録を開始
        /// </summary>
        public void StartRecording()
        {
            sessionData = new SessionData();
            sessionId = Guid.NewGuid().ToString();

            sessionStartTime = Time.time;
            nextRecordTime = 0f;
            isRecording = true;

            Debug.Log($"[GameDataRecorder] Recording started. Session ID: {sessionId}");

#if UNITY_EDITOR
            TryStartUnityMovieRecorder();
#endif
        }

        void Update()
        {
            if (!isRecording) return;

            float elapsedTime = Time.time - sessionStartTime;

            if (elapsedTime >= nextRecordTime)
            {
                RecordDataPoint(elapsedTime);
                nextRecordTime += recordInterval;
            }
        }

        /// <summary>
        /// データポイントを記録
        /// </summary>
        private void RecordDataPoint(float timestamp)
        {
            DataPoint dataPoint = new DataPoint(timestamp);

            // GetValueデリゲートからデータを取得
            if (GetValue != null)
            {
                dataPoint.value = GetValue();
            }
            else
            {
                // デフォルト値（デモ用）
                dataPoint.value = new Dictionary<string, object>();
            }

            sessionData.dataPoints.Add(dataPoint);
        }

        /// <summary>
        /// イベントを記録（最後のデータポイントに）
        /// </summary>
        public void RecordEvent(string eventKey, object eventValue)
        {
            if (sessionData.dataPoints.Count > 0)
            {
                var lastPoint = sessionData.dataPoints[sessionData.dataPoints.Count - 1];
                if (lastPoint.@event == null)
                {
                    lastPoint.@event = new Dictionary<string, object>();
                }
                lastPoint.@event[eventKey] = eventValue;
            }
            else
            {
                Debug.LogWarning("[GameDataRecorder] No data points to attach event. Record some data first.");
            }
        }

        /// <summary>
        /// valueパラメータを追加（最後のデータポイントに）
        /// </summary>
        public void AddValueParameter(string key, object value)
        {
            if (sessionData.dataPoints.Count > 0)
            {
                var lastPoint = sessionData.dataPoints[sessionData.dataPoints.Count - 1];
                lastPoint.value[key] = value;
            }
            else
            {
                Debug.LogWarning("[GameDataRecorder] No data points to add value parameter. Record some data first.");
            }
        }

        /// <summary>
        /// sessionInfo内の値を設定（セクション名とキーを指定）
        /// </summary>
        public void SetSessionInfo(string sectionName, string key, object value)
        {
            sessionData.SetSessionInfo(sectionName, key, value);
        }

        /// <summary>
        /// sessionInfo内のセクション全体を設定
        /// </summary>
        public void SetSessionInfoSection(string sectionName, Dictionary<string, object> sectionData)
        {
            sessionData.sessionInfo[sectionName] = sectionData;
        }

        /// <summary>
        /// sessionInfoから値を取得
        /// </summary>
        public object GetSessionInfo(string sectionName, string key)
        {
            return sessionData.GetSessionInfo(sectionName, key);
        }

        /// <summary>
        /// 記録を停止してデータを保存
        /// </summary>
        public void StopRecording()
        {
            if (!isRecording) return;

            isRecording = false;

#if UNITY_EDITOR
            StopUnityMovieRecorderInternal();
#endif

            if (autoSave)
            {
                SaveData();
            }

            Debug.Log($"[GameDataRecorder] Recording stopped. Total data points: {sessionData.dataPoints.Count}");
        }

        /// <summary>
        /// データをJSON形式で圧縮保存
        /// </summary>
        public string SaveData()
        {
            try
            {
                // カスタムJSONシリアライズ（Dictionaryサポート）
                string json = SerializeToJson(sessionData);

                // 出力ディレクトリ作成
                string fullOutputPath = Path.Combine(Application.dataPath, "..", outputDirectory);
                Directory.CreateDirectory(fullOutputPath);

                // ファイル名生成（タイムスタンプ付き）
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"{sessionId}_{timestamp}";

                // 非圧縮JSON保存（デバッグ用）
                string jsonPath = Path.Combine(fullOutputPath, $"{fileName}.json");
                File.WriteAllText(jsonPath, json, Encoding.UTF8);

                // GZIP圧縮保存
                string gzipPath = Path.Combine(fullOutputPath, $"{fileName}.json.gz");
                using (FileStream fileStream = File.Create(gzipPath))
                using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Compress))
                {
                    byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                    gzipStream.Write(jsonBytes, 0, jsonBytes.Length);
                }

                Debug.Log($"[GameDataRecorder] Data saved:\n- JSON: {jsonPath}\n- GZIP: {gzipPath}");
                Debug.Log($"[GameDataRecorder] Compression ratio: {new FileInfo(jsonPath).Length / (float)new FileInfo(gzipPath).Length:F2}x");

                return gzipPath;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameDataRecorder] Failed to save data: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// SessionDataをJSON文字列に変換（Dictionaryサポート）
        /// </summary>
        private string SerializeToJson(SessionData data)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");

            // sessionInfo
            sb.AppendLine("  \"sessionInfo\": {");
            bool firstSection = true;
            foreach (var section in data.sessionInfo)
            {
                if (!firstSection) sb.AppendLine(",");
                firstSection = false;

                sb.Append($"    \"{section.Key}\": ");
                sb.Append(SerializeObject(section.Value, 2));
            }
            sb.AppendLine();
            sb.AppendLine("  },");

            // dataPoints
            sb.AppendLine("  \"dataPoints\": [");
            for (int i = 0; i < data.dataPoints.Count; i++)
            {
                var point = data.dataPoints[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"timestamp\": {point.timestamp},");
                sb.Append("      \"value\": ");
                sb.Append(SerializeObject(point.value, 3));
                sb.AppendLine(",");
                sb.Append("      \"event\": ");
                sb.Append(point.@event == null ? "null" : SerializeObject(point.@event, 3));
                sb.AppendLine();
                sb.Append("    }");
                if (i < data.dataPoints.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("  ]");
            sb.Append("}");

            return sb.ToString();
        }

        /// <summary>
        /// オブジェクトをJSON文字列に変換
        /// </summary>
        private string SerializeObject(object obj, int indentLevel)
        {
            if (obj == null) return "null";

            string indent = new string(' ', indentLevel * 2);
            string nextIndent = new string(' ', (indentLevel + 1) * 2);

            if (obj is Dictionary<string, object> dict)
            {
                if (dict.Count == 0) return "{}";

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("{");
                bool first = true;
                foreach (var kvp in dict)
                {
                    if (!first) sb.AppendLine(",");
                    first = false;

                    sb.Append($"{nextIndent}\"{kvp.Key}\": ");
                    sb.Append(SerializeObject(kvp.Value, indentLevel + 1));
                }
                sb.AppendLine();
                sb.Append($"{indent}}}");
                return sb.ToString();
            }
            else if (obj is Vector3Serializable vec3)
            {
                return $"{{\"x\": {vec3.x}, \"y\": {vec3.y}, \"z\": {vec3.z}}}";
            }
            else if (obj is string str)
            {
                return $"\"{EscapeJsonString(str)}\"";
            }
            else if (obj is bool b)
            {
                return b ? "true" : "false";
            }
            else if (obj is int || obj is float || obj is double || obj is long)
            {
                return obj.ToString();
            }
            else
            {
                return $"\"{obj}\"";
            }
        }

        /// <summary>
        /// JSON文字列のエスケープ処理
        /// </summary>
        private string EscapeJsonString(string str)
        {
            return str.Replace("\\", "\\\\")
                      .Replace("\"", "\\\"")
                      .Replace("\n", "\\n")
                      .Replace("\r", "\\r")
                      .Replace("\t", "\\t");
        }

#if UNITY_EDITOR
        void TryStartUnityMovieRecorder()
        {
            if (!enableUnityMovieRecorder)
                return;

            StopUnityMovieRecorderInternal();

            try
            {
                var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
                var movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();

                movieSettings.FrameRate = unityMovieFrameRate;
                movieSettings.ImageInputSettings = new GameViewInputSettings
                {
                    OutputWidth = unityMovieWidth,
                    OutputHeight = unityMovieHeight,
                };
                movieSettings.EncoderSettings = new CoreEncoderSettings
                {
                    Codec = CoreEncoderSettings.OutputCodec.MP4,
                    EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.Medium,
                };
                movieSettings.CaptureAudio = unityMovieCaptureAudio;

                string relativePath = Path.Combine(outputDirectory, $"{sessionId}.mp4").Replace('\\', '/');
                movieSettings.OutputFile = relativePath;
                movieSettings.Enabled = true;

                controllerSettings.AddRecorderSettings(movieSettings);

                unityRecorderController = new RecorderController(controllerSettings);
                unityRecorderController.PrepareRecording();
                unityRecorderController.StartRecording();

                Debug.Log($"[GameDataRecorder] Unity Recorder 開始: {relativePath}.mp4");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameDataRecorder] Unity Recorder の開始に失敗: {ex.Message}");
            }
        }

        void StopUnityMovieRecorderInternal()
        {
            if (unityRecorderController == null)
                return;

            try
            {
                unityRecorderController.StopRecording();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameDataRecorder] Unity Recorder の停止に失敗: {ex.Message}");
            }

            unityRecorderController = null;
        }
#endif

        void OnApplicationQuit()
        {
            StopRecording();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            StopRecording();
        }
    }
}
