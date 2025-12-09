using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static UnityEngine.GraphicsBuffer;

public class Camera: MonoBehaviour
{
    public TransfEv followmode;
    public float rotateSpeed = 5f;
    public float radius = 5f;

    private void Start()
    {
        followmodes.Add("simple",(tr) =>//a simple follow mode
        {
            tr.position = ct.pp + Vector3.left * 5;
            ct.LookAt(tr, ct.pp);
        });
        followmodes.Add("simpleSurround", (obj) =>
        {
            // momentum = 角速度，因此 * deltaTime 积分成角度
            ct.yawCamera += -ct.mouseDirection.x * Time.deltaTime;
            ct.pitchCamera += -ct.mouseDirection.y * Time.deltaTime;

            // 限制上下角（防止翻转）
            ct.pitchCamera = Mathf.Clamp(ct.pitchCamera, -1.2f, 1.2f);

            // 角度 → 世界坐标偏移
            Vector3 offset = new Vector3(
                Mathf.Cos(ct.yawCamera) * Mathf.Cos(ct.pitchCamera),
                Mathf.Sin(ct.pitchCamera),
                Mathf.Sin(ct.yawCamera) * Mathf.Cos(ct.pitchCamera)
            ) * radius;

            // 更新位置
            obj.position = ct.pp + offset;

            // 始终朝向中心
            obj.LookAt(ct.pp);
        });

        followmode = followmodes["simpleSurround"];

        
    }
    private void LateUpdate()
    {
        followmode?.Invoke(transform);
    }


    public Dictionary<string,TransfEv> followmodes = new();
}