using UnityEngine;

namespace Manatite.UI
{
    public class MenuSkyboxScroller : MonoBehaviour
    {
        [SerializeField] private Material baseSkybox;
        [SerializeField] private float scrollSpeedDegPerSec = 4f;

        private Material _runtimeSkybox;
        private float _rotation;

        private void Start()
        {
            _runtimeSkybox = new Material(baseSkybox);
            RenderSettings.skybox = _runtimeSkybox;
            _rotation = _runtimeSkybox.GetFloat("_Rotation");
        }

        private void Update()
        {
            _rotation = Mathf.Repeat(_rotation + scrollSpeedDegPerSec * Time.deltaTime, 360f);
            _runtimeSkybox.SetFloat("_Rotation", _rotation);
        }

        private void OnDestroy()
        {
            if (_runtimeSkybox != null)
            {
                Destroy(_runtimeSkybox);
            }
        }
    }
}
