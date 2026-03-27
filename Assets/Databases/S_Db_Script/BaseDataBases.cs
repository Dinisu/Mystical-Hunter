using System.Collections.Generic;
using UnityEngine;

namespace App.BaseSystem.DataStores.ScriptableObjects
{
    public class BaseDataBases<T> : ScriptableObject
    {
        [SerializeField]
        private List<T> list = new List<T>(); // データベース

        // 外部から読み取るためのプロパティ
        public List<T> List => list;
    }
}

