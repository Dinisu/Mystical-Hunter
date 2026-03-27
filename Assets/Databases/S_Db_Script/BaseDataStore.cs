using UnityEngine;

namespace App.BaseSystem.DataStores.ScriptableObjects
{
    /// <summary>
    /// データベースを外部から参照できるようにする
    /// </summary>
    public abstract class BaseDataStore<T, U> : MonoBehaviour where T : BaseDataBase<U> where U : BaseData
    {
        public T DataBase => dataBase;
        [SerializeField]
        protected T dataBase; // エディターでデータベースを指定

        /// <summary>
        /// 文字列を用いてデータベース内のデータを取得
        /// </summary>
        public U FindWithName(string name)
        {
            if (string.IsNullOrEmpty(name)) { return default; } // null回避

            return dataBase.ItemList.Find(e => e.name == name);
        }

        /// <summary>
        /// idを用いてデータベース内のデータを取得
        /// </summary>
        public U FindWithId(int id)
        {
            return dataBase.ItemList.Find(e => e.Id == id);
        }
    }
}