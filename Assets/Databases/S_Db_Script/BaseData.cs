using UnityEngine;

namespace App.BaseSystem.DataStores.ScriptableObjects
{
    /// <summary>
    /// ScriptableObjectで管理されるデータの基盤
    /// </summary>
    public abstract class BaseData : ScriptableObject
    {
        public string Name
        {
            get => name;//Name プロパティを取得する際に、name というフィールドの値を返す。
            set => name = value;//Name プロパティに値を代入する際に、name フィールドにその値を設定。
                                //value は、プロパティに設定された値が格納される暗黙的な変数。
        }
        [SerializeField]
        private new string name;
        public int Id => id;
        [SerializeField]
        private int id;

        [TextArea, Header("説明")]
        public string ItemDescription;
    }
}
