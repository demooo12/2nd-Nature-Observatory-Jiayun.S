using UnityEngine;
using UnityEngine.Video;

public class VHSTextureMovie : MonoBehaviour
{
    public VideoClip videoClip;
    public MeshRenderer meshRenderer;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();

        RenderTexture rt = new RenderTexture(512, 512, 0);
        meshRenderer.material.mainTexture = rt;

        VideoPlayer vp = gameObject.AddComponent<VideoPlayer>();
        vp.clip = videoClip;
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = rt;
        vp.isLooping = true;
        vp.Play();
    }
}
