using System;
using System.Collections.Generic;
using UnityEngine;
//ステートサンプル　使用しているわけではない
[Serializable]
public class Statesample<T1,T2> : MonoBehaviour
{
    /*
    private Dictionary<T1, T2> d_list = new();
    [SerializeField] private List<Entry> list;

    [Serializable]
    public class Entry
    {
        public T1 aiu;
        public T2 bi;
    }

    public void Init()
    {
        foreach(var d in list)
        {
            d_list[d.aiu] = d.bi;
        }
    }

    public T2 Get(T1 state)
    {
        if(d_list.TryGetValue(state,out var value))
        {
            return value;
        }
        return default;
    }

    public static T Gatdinisu<T>(GameObject name) where T : Component
    {
        return name.GetComponent<T>();
    }

    /*
     * [SerializeField] Statesample<int, GameObject> aaalist;
    [SerializeField] Statesample<string , int> aaaaa;
     * aaalist.Init();
        var gameobj = aaalist.Get(2);
     */
}
