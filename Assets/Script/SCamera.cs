using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static UnityEngine.GraphicsBuffer;

public class SCamera: MonoBehaviour
{
    public TransfEv followmode;
    public float rotateSpeed = 5f;
    public float radius = 5f;
    public SLibrary lib;
    public GameObject target;
    public GameObject exTarget;
    public bool changeTarget = false;
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
        followmodes.Add("surround", cam =>
        {
            Vector3 tw = (Vector3)lib.Read("toward");
            float a = (float)lib.Read("a");
            int i = (int)lib.Read("i");

            if (changeTarget)
            {
                tw = (target.transform.position - cam.transform.position) / ((SMath.CosA(a * i) + 1) / 2) ;
                lib.Write("toward", tw);

                changeTarget = false;
            }
            Vector3 offset = new Vector3();

            bool smoothing = (bool)lib.Read("smooth");
            if (smoothing)// if the target has swapping
            {
                int smoothTick = 30;// is also i. time

                if (i >= smoothTick)
                {//finish iteration

                    offset -= tw;
                    lib.Write("smooth", false);
                    return;
                }

                Vector3 mp = (SMath.CosA(a * i) + 1) / 2 * tw;//position be move
                offset -= mp;

                lib.Write("i", i+1);
            }
            else if (changeTarget)
            {
                Vector3 tpos = target.transform.position;//position of now target
                Vector3 expos = exTarget.transform.position;//position of ex target
                int smoothTick = 30;//pre val, test val

                var toward = tpos - expos;
                float angle = 180 / smoothTick;

                lib.Write("smooth", true);
                lib.Write("i", 0);// number of iteration
                lib.Write("toward", toward);
                lib.Write("a", angle);

                changeTarget = false;
            }

            ct.yawCamera += -ct.mouseDirection.x * Time.deltaTime;
            ct.pitchCamera += -ct.mouseDirection.y * Time.deltaTime;

            // �������½ǣ���ֹ��ת��
            ct.pitchCamera = Mathf.Clamp(ct.pitchCamera, -1.2f, 1.2f);

            // �Ƕ� �� ��������ƫ��
           offset += new Vector3(
                Mathf.Cos(ct.yawCamera) * Mathf.Cos(ct.pitchCamera),
                Mathf.Sin(ct.pitchCamera),
                Mathf.Sin(ct.yawCamera) * Mathf.Cos(ct.pitchCamera)
            ) * radius;

            // ����λ��
            cam.position = target.transform.position + offset;

            // ʼ�ճ�������
            cam.LookAt(target.transform.position);
        });

        followmode = followmodes["surround"];

        
    }
    private void LateUpdate()
    {
        if(canMove)
            followmode?.Invoke(transform);
    }
    public void ChangeTarget(GameObject curTarget)
    {
        exTarget = target;
        changeTarget = true;
        target = curTarget;
    }

    public Dictionary<string,TransfEv> followmodes = new();
}