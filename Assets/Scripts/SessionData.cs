using System;
using System.Collections.Generic;

namespace GameDataAnalysis
{
    /// <summary>
    /// セッション全体のデータ
    /// </summary>
    [Serializable]
    public class SessionData
    {
        /// <summary>
        /// セッション情報（任意の階層構造）
        /// </summary>
        public Dictionary<string, object> sessionInfo;

        /// <summary>
        /// データポイントのリスト
        /// </summary>
        public List<DataPoint> dataPoints;

        public SessionData()
        {
            sessionInfo = new Dictionary<string, object>();
            dataPoints = new List<DataPoint>();
        }

        /// <summary>
        /// sessionInfo内のセクションを取得（存在しない場合は作成）
        /// </summary>
        public Dictionary<string, object> GetOrCreateSection(string sectionName)
        {
            if (!sessionInfo.ContainsKey(sectionName))
            {
                sessionInfo[sectionName] = new Dictionary<string, object>();
            }
            return sessionInfo[sectionName] as Dictionary<string, object>;
        }

        /// <summary>
        /// sessionInfo内の値を設定
        /// </summary>
        public void SetSessionInfo(string sectionName, string key, object value)
        {
            var section = GetOrCreateSection(sectionName);
            section[key] = value;
        }

        /// <summary>
        /// sessionInfo内の値を取得
        /// </summary>
        public object GetSessionInfo(string sectionName, string key)
        {
            if (sessionInfo.ContainsKey(sectionName) && sessionInfo[sectionName] is Dictionary<string, object> section)
            {
                return section.ContainsKey(key) ? section[key] : null;
            }
            return null;
        }
    }
}
