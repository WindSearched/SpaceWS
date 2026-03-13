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
    public Vector3 exPos;
    public bool changeTarget = false;

    public static int stick = 30;
    private Pinner camMove => ct.cameraMove;
    private void Start()
    {
        ct.sCamera = this;
        target = ct.player;

        lib.Write("i", stick);

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
            Vector3 tw = lib.Read<Vector3>("toward");
            float a = lib.Read<float>("a");
            int i = lib.Read<int>("i");

            Vector3 offset = new Vector3();

            bool smoothing = lib.Read<bool>("smooth");
            if (smoothing)
            {
                if (changeTarget)
                {
                    tw = (target.transform.position - cam.transform.position) / ((SMath.CosA(a * i) + 1) / 2) ;
                    lib.Write("toward", tw);

                    changeTarget = false;
                }
            }
            else if (changeTarget)
            {
                Vector3 tpos = target.transform.position;//position of now target
                Vector3 expos = exTarget ? exTarget.transform.position : exPos;

                var toward = tpos - expos;
                float angle = 180 / stick;

                lib.Write("smooth", true);
                smoothing = true;
                lib.Write("i", 0);// number of iteration
                i = 0;
                lib.Write("toward", toward);
                tw = toward;
                lib.Write("a", angle);
                a = angle;

                changeTarget = false;
            }

            if (smoothing)// if the target has swapping
            {
                if (i >= stick)
                {//finish iteration

                    offset = Vector3.zero;
                    lib.Write("smooth", false);
                }
                else
                {
                    float t = (SMath.CosA(a * i) + 1);
                    Vector3 mp =  t/ 2 * tw;//position be move
                    offset = -mp;

                    lib.Write("i", i+1);
                }
            }


            ct.yawCamera += -ct.mouseDirection.x * Time.deltaTime;
            ct.pitchCamera += -ct.mouseDirection.y * Time.deltaTime;

            // �������½ǣ���ֹ��ת��
            ct.pitchCamera = Mathf.Clamp(ct.pitchCamera, -1.2f, 1.2f);

            // �Ƕ� �� ��������ƫ��
           var offsetsurround = new Vector3(
                Mathf.Cos(ct.yawCamera) * Mathf.Cos(ct.pitchCamera),
                Mathf.Sin(ct.pitchCamera),
                Mathf.Sin(ct.yawCamera) * Mathf.Cos(ct.pitchCamera)
            ) * radius;


            // ����λ��
            cam.position = target.transform.position + offset + offsetsurround;

            // ʼ�ճ�������
            cam.LookAt(target.transform.position + offset);
        });

        followmode = followmodes["surround"];

        
    }
    private void LateUpdate()
    {
        if(camMove.loosing)
            followmode?.Invoke(transform);
    }
    public void ChangeTarget(GameObject curTarget)
    {
        exTarget = target;
        exPos = target.transform.position;
        changeTarget = true;
        target = curTarget;
    }

    public Dictionary<string,TransfEv> followmodes = new();
}