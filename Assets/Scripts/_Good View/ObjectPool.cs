using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public struct PrefabByTag
{
	public PolledObjectTag tag;
	public PooledObject prefab;
}

public enum PolledObjectTag
{
	HitEffect
}

public class ObjectPool : MonoBehaviour
{
	private static ObjectPool instance;

	[SerializeField] private PrefabByTag[] prefabs;
	private Dictionary<PolledObjectTag, Queue<PooledObject>> pools = new Dictionary<PolledObjectTag, Queue<PooledObject>>();
	private Dictionary<PolledObjectTag, PooledObject> prefabByTag = new Dictionary<PolledObjectTag, PooledObject>();

	private void Start()
	{
		instance = this;

		foreach (PrefabByTag prefab in prefabs)
		{
			pools.Add(prefab.tag, new Queue<PooledObject>());
			prefabByTag.Add(prefab.tag, prefab.prefab);
		}
	}

	public static PooledObject GetObjectByTag(PolledObjectTag tag)
	{
		if (!instance.pools.ContainsKey(tag))
		{
			Debug.LogError($"ObjectPool don't have {tag} - tag");
			return null;
		}

		PooledObject result;
		if (instance.pools[tag].Count == 0)
			result = Instantiate(instance.prefabByTag[tag]);
		else
			result = instance.pools[tag].Dequeue();

		result.GetReady();
		return result;
	}

	public static void AddToPool(PooledObject obj)
	{
		if (!instance.pools.ContainsKey(obj.Tag))
		{
			Debug.LogError($"ObjectPool don't have {obj.Tag} - tag");
			return;
		}

		instance.pools[obj.Tag].Enqueue(obj);
	}
}
