using UnityEngine;
using UnityEngine.UI;

namespace Manatite.UI
{
    /// <summary>
    /// Redirects <see cref="MenuCursor"/>'s selection tint to a different <see cref="Graphic"/>
    /// than the selected object's own — e.g. a Slider highlighting its row's label text instead
    /// of its own track.
    /// </summary>
    public sealed class SelectionGlowTarget : MonoBehaviour
    {
        [SerializeField] private Graphic target;

        public Graphic Target => target;
    }
}
