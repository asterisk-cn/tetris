using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDataAnalysis
{
    /// <summary>
    /// 特定時点のゲームデータポイント
    /// </summary>
    [Serializable]
    public class DataPoint
    {
        /// <summary>
        /// セッション開始からの経過時間（秒）
        /// </summary>
        public float timestamp;

        /// <summary>
        /// 任意の数値パラメータを含むオブジェクト
        /// </summary>
        public Dictionary<string, object> value;

        /// <summary>
        /// イベント情報（キーと値のペア）
        /// </summary>
        public Dictionary<string, object> @event;

        public DataPoint()
        {
            value = new Dictionary<string, object>();
            @event = null;
        }

        public DataPoint(float timestamp)
        {
            this.timestamp = timestamp;
            this.value = new Dictionary<string, object>();
            this.@event = null;
        }
    }

    /// <summary>
    /// Vector3のシリアル化可能バージョン
    /// </summary>
    [Serializable]
    public class Vector3Serializable
    {
        public float x;
        public float y;
        public float z;

        public Vector3Serializable() { }

        public Vector3Serializable(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }
}
