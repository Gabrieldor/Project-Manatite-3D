using System.Collections.Generic;
using UnityEngine;

namespace Manatite.Core
{
    /// <summary>
    /// Generic per-prefab pool. Enemies/projectiles/VFX must Get/Release through this
    /// instead of Instantiate/Destroy. Lazily creates its own host GameObject on first use.
    /// </summary>
    public sealed class ObjectPooler : MonoBehaviour
    {
        private static ObjectPooler _instance;

        private static ObjectPooler Instance
        {
            get
            {
                if (_instance == null)
                {
                    var host = new GameObject("ObjectPooler");
                    _instance = host.AddComponent<ObjectPooler>();
                    DontDestroyOnLoad(host);
                }
                return _instance;
            }
        }

        private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();
        private readonly Dictionary<GameObject, GameObject> _instanceSource = new Dictionary<GameObject, GameObject>();

        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return Instance.GetInternal(prefab, position, rotation);
        }

        public static void Release(GameObject instance)
        {
            Instance.ReleaseInternal(instance);
        }

        private GameObject GetInternal(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!_pools.TryGetValue(prefab, out Queue<GameObject> pool))
            {
                pool = new Queue<GameObject>();
                _pools[prefab] = pool;
            }

            while (pool.Count > 0)
            {
                GameObject pooled = pool.Dequeue();
                if (pooled == null) continue;

                pooled.transform.SetPositionAndRotation(position, rotation);
                pooled.SetActive(true);
                return pooled;
            }

            GameObject instance = Instantiate(prefab, position, rotation);
            _instanceSource[instance] = prefab;
            return instance;
        }

        private void ReleaseInternal(GameObject instance)
        {
            if (instance == null) return;

            instance.SetActive(false);

            if (_instanceSource.TryGetValue(instance, out GameObject prefab) && _pools.TryGetValue(prefab, out Queue<GameObject> pool))
                pool.Enqueue(instance);
            else
                Destroy(instance);
        }
    }
}
