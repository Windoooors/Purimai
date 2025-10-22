using System;
using UnityEngine;
using UnityEngine.UI;

namespace Rendering
{
    public class CreateRenderTexture : MonoBehaviour
    {
        public RawImage rawImage;
        
        private void Start()
        {
            RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);
            rt.useMipMap = false;
            rt.autoGenerateMips = false; 
                var cameraA = GetComponent<Camera>();
                
                cameraA.clearFlags = CameraClearFlags.SolidColor;
                cameraA.backgroundColor = new Color(0,0,0,0);
                
                cameraA.targetTexture = rt;

            rawImage.gameObject.SetActive(true);
            rawImage.texture = rt;
        }
    }
}