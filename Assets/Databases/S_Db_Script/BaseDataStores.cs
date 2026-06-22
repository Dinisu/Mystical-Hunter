using App.BaseSystem.DataStores.ScriptableObjects;
using App.BaseSystem.DataStores.ScriptableObjects.Status;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace App.BaseSystem.DataStores.ScriptableObjects
{
    public class BaseDataStores<T, U> : MonoBehaviour where T : BaseDataBases<U> where U : ScriptableObject
    {
        //データベース検索用
        public T DataBases => dataBases;
        [SerializeField]
        protected T dataBases; // エディターでデータベースを指定

        // データベースからnameに一致するデータを検索
        public U FindDatabaseWithName(string name)
        {
            if (string.IsNullOrEmpty(name)) { return null; } // nullや空文字列を回避



            return dataBases.List.Find(e => e.name == name);
        }
    }
}
