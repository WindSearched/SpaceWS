using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public static class Projector
{
    public static Transform parent;
    public static Camera previewCamera;
    public static RenderTexture renderTexture;

    static Texture2D CaptureRT(RenderTexture rt, int width, int height)
    {
        RenderTexture prev = RenderTexture.active;
        try
        {
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply(false, false);
            return tex;
        }
        finally
        {
            RenderTexture.active = prev;
        }
    }

    // public static Sprite Project(GameObject projected, int width, int height)
    // {
    //     // 备份 Transform
    //     Transform tr = projected.transform;
    //     Transform oldParent = tr.parent;
    //     Vector3 oldPos = tr.position;
    //     Quaternion oldRot = tr.rotation;
    //     Vector3 oldScale = tr.localScale;
    //     //int oldLayer = projected.layer;
    //
    //     // 挂到预览节点
    //     tr.SetParent(parent, false);
    //
    //     //projected.layer = LayerMask.NameToLayer("projection");
    //
    //     // 自动居中
    //     Bounds bounds = CalculateBounds(projected);
    //     tr.localPosition = -bounds.center;
    //     float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
    //     previewCamera.transform.localPosition = new Vector3(0, 0, -size * 2.5f);
    //
    //     // 强制渲染
    //     previewCamera.targetTexture = renderTexture;
    //     previewCamera.Render();
    //
    //     Texture2D tex = CaptureRT(renderTexture, width, height);
    //
    //     // 还原 Transform
    //     tr.SetParent(oldParent);
    //     tr.position = oldPos;
    //     tr.rotation = oldRot;
    //     tr.localScale = oldScale;
    //     //projected.layer = oldLayer;
    //
    //     return Sprite.Create(
    //         tex,
    //         new Rect(0, 0, tex.width, tex.height),
    //         new Vector2(0.5f, 0.5f),
    //         100f
    //     );
    // }
    public static IEnumerator ProjectAsync(GameObject projected, int width, int height, System.Action<Sprite> callback)
    {
        var o = Object.Instantiate(projected, parent, true);
        o.transform.position = new Vector3(0, 0, 2);
        o.SetActive(true);
        o.layer = LayerMask.NameToLayer("projection");

        previewCamera.targetTexture = renderTexture;
        previewCamera.Render();

        // 等渲染完成
        yield return new WaitForEndOfFrame();

        var tex = CaptureRT(renderTexture, width, height);
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);

        callback?.Invoke(sprite);
        o.SetActive(false);
        Object.Destroy(o);
    }
    static Bounds CalculateBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
}