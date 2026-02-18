using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static UnityEngine.GraphicsBuffer;

public class SCamera: MonoBehaviour
{
    public TransfEv followmode;
    public float rotateSpeed = 5f;
    public float radius = 5f;
    private bool canMove => ct.cameraCanMove;
    private void Start()
    {
        ct.sCamera = this;
        followmodes.Add("simple",(tr) =>//a simple follow mode
        {
            tr.position = ct.pp + Vector3.left * 5;
            ct.LookAt(tr, ct.pp);
        });
        followmodes.Add("simpleSurround", (obj) =>
        {
            // momentum = ���ٶȣ���� * deltaTime ���ֳɽǶ�
            ct.yawCamera += -ct.mouseDirection.x * Time.deltaTime;
            ct.pitchCamera += -ct.mouseDirection.y * Time.deltaTime;

            // �������½ǣ���ֹ��ת��
            ct.pitchCamera = Mathf.Clamp(ct.pitchCamera, -1.2f, 1.2f);

            // �Ƕ� �� ��������ƫ��
            Vector3 offset = new Vector3(
                Mathf.Cos(ct.yawCamera) * Mathf.Cos(ct.pitchCamera),
                Mathf.Sin(ct.pitchCamera),
                Mathf.Sin(ct.yawCamera) * Mathf.Cos(ct.pitchCamera)
            ) * radius;

            // ����λ��
            obj.position = ct.pp + offset;

            // ʼ�ճ�������
            obj.LookAt(ct.pp);
        });

        followmode = followmodes["simpleSurround"];

        
    }
    private void LateUpdate()
    {
        if(canMove)
            followmode?.Invoke(transform);
    }


    public Dictionary<string,TransfEv> followmodes = new();
}