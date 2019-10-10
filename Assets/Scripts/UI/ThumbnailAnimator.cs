using UnityEngine;
using UnityEngine.UI;


namespace Tanks.UI
{
    //Helper class for animating EveryPlay thumbnails
    public class ThumbnailAnimator : MonoBehaviour
    {

        protected virtual void OnEnable()
        {
            Debug.LogError("ThumbnailAnimator should be used, since EveryPlay is removed (unsupported)!");
        }
    }
}