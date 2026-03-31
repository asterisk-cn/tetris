using System.Collections.Generic;
using GameDataAnalysis;
using UnityEngine;

public class RecorderManager : MonoBehaviour
{
    public string gameTitle = "Tetris";
    public int age = 25;
    public string skillLevel = "intermediate";
    public string difficulty = "normal";
    public bool soundEnabled = true;

    DataRecorder dataRecorder;

    void Start()
    {
        dataRecorder = DataRecorder.Instance;
        if (dataRecorder == null)
        {
            Debug.LogError("DataRecorder not found");
            return;
        }

        // value取得メソッドを登録
        dataRecorder.GetValue = () => new Dictionary<string, object>
        {
            { "score", ScoreManager.score },
            { "level", LevelManager.level },
        };

        // sessionInfoに任意のセクションを追加
        dataRecorder.SetSessionInfo("generalInfo", "gameTitle", gameTitle);
        dataRecorder.SetSessionInfo("generalInfo", "sessionId", dataRecorder.sessionId);

        dataRecorder.SetSessionInfo("playerAttributes", "age", age);
        dataRecorder.SetSessionInfo("playerAttributes", "skillLevel", skillLevel);

        dataRecorder.SetSessionInfo("gameSettings", "difficulty", difficulty);
        dataRecorder.SetSessionInfo("gameSettings", "soundEnabled", soundEnabled);
    }
}
