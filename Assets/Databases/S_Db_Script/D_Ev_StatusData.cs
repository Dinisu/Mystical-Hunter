using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using UnityEngine;


namespace App.BaseSystem.DataStores.ScriptableObjects.Status
{
    [CreateAssetMenu(menuName = "ScriptableObject/Data/EventStatus")]
    public class D_Ev_StatusData : BaseData
    {
        [SerializeField, Header("セーブ用ID")]
        public string ID;

        [TextArea, Header("イベント1説明")]
        public string Event1Explanation;
        public bool Event1;

        [TextArea, Header("イベント2説明")]
        public string Event2Explanation;
        public bool Event2;

        [TextArea, Header("イベント3説明")]
        public string Event3Explanation;
        public bool Event3;
    }
}
