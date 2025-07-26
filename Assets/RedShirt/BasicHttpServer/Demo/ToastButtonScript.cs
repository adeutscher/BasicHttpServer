using UnityEngine;

namespace RedShirt.BasicHttpServer.Demo
{
    public class ToastButtonScript : MonoBehaviour
    {
        private int _times;

        public void OnPress()
        {
            ToastRegionScript.Instance.Add($"ButtonPressed times: {++_times}");
        }
    }
}