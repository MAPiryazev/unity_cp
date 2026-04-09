using UnityEngine;
using UnityEngine.Events;

public class PooledObject : MonoBehaviour
{
	[SerializeField] private PolledObjectTag tag;
	[SerializeField] private float lifeTime;
	[SerializeField] private UnityEvent onReady;

	public PolledObjectTag Tag => tag;

	public void GetReady()
	{
		gameObject.SetActive(true);
		onReady.Invoke();
		Invoke(nameof(GoToPool), lifeTime);
	}

	public void GoToPool()
	{
		gameObject.SetActive(false);
		ObjectPool.AddToPool(this);
	}

	private void OnDestroy()
	{
		CancelInvoke();
	}
}
