using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class ct
{
    public static CenterSystem ctsym;
    public static GameObject player => ctsym.player;
    public static Player plyComp;
    public static Camera camera;
    public static Set setting = new();
    public static Rule curWorldRule = new();
    public static Log log = new();
    public static Mod mod = new();
    public static CommandBranch CommandBranch = new("main");
    public static MethValues methValues = new ();
    public static SDebug debugInfo;
    public static ChunkLoader chunkLoader;
    public static SAction action = new();
    public static PageStm pages = new();
    public static DebugPage dePa => ctsym.debugPage;
    public static BuildPage buildPage;
    public static ViewPage viewPage;

    public static ModeStm modes = new();
    public static Dictionary<string,InputAction> acts = new();
    public static Vector3 playerPosition;//updated every frame
    public static V3I intPlayerPosition;
    public static V3I playerchunkPosition;
    public static Vector2 wasdDirection;
    public static Vector2 mousePosition;
    /// <summary>
    /// delta position of mouse this frame
    /// </summary>
    public static Vector2 mouseDirection;
    public static Pinner playerMove = new();
    public static bool mouseCanMove = false;
    public static Pinner cameraMove = new();
    public static SCamera sCamera;
    public static float fps;
    public static float yawCamera;
    public static float pitchCamera;

    public static Vector2 screenSize;

    public static Transform bodiesParent;
    public static GameObject defualtBody;
    public static GameObject mouseCasted => mousecast.casted;
    /// <summary>
    /// player position
    /// </summary>
    public static Vector3 pp
    {
        get => playerPosition;
        set => playerPosition = value;
    }

    public static V3I pip
    {
        get => intPlayerPosition;
        set => intPlayerPosition = value;
    }
    /// <summary>
    /// player chunk position
    /// </summary>
    public static V3I pcp
    {
        get => playerchunkPosition;
        set
        {
            if (playerchunkPosition == value) return;
            playerchunkPosition = value;
            onChunkPositionChange?.Invoke();
        }
    }

    public static Bodies bodies = new();
    public static MouseRaycast mousecast;

    public static Dictionary<string, Sprite> structIcons = new();
    public static SDict<string, ItemData> items = new();
    public static SMTDict<MaterialData> materials = new();
    public static SMTDict<StructInfo> structsInfo = new();

    public static Meth LoadAfterInconsFinishLoading
    {
        set
        {
            if(ctsym.finishLoadIcon)
                value?.Invoke();
            else
                ctsym.WhenIconsFinisheLoading +=  value;;
        }
    }

    public static Dictionary<string, ItemData> itemsData = new();

    public static Dictionary<string, Sprite> indicatorSprites = new();
    public static Indicator indicator = new();

    public static SScroll buildScroll;
    public static Transform templateParent => ctsym.templateParent;
    public static Transform structFacesTemplateParent => ctsym.structFacesTemplateParent;
    public static List<string> structTypes = new();

    public static Material defaultMat;
    public static Material outlineMat => ctsym.m_outline;
    public static Material trasparentMat => ctsym.trasparentMat;

    public static event Meth updatePerTick;
    /// <summary>
    /// invoke when chunk position of player change and a time when game start
    /// </summary>
    public static event Meth onChunkPositionChange;
    public static InputAction leftMouse_act;
    public static InputAction rightMouse_act;

    public static CommandPage cmdPg;

    public static void OnChunkPositionChange() => onChunkPositionChange?.Invoke();
    //
    //  Methods
    //
    public static void LookAt(Transform tr, Vector3 target)
    {
        tr.LookAt(target);
    }
    public static void LookAt(Transform tr, Transform target)
    {
        LookAt(tr, target.position);
    }
    public static void UpdatePerTick()
    {
        updatePerTick?.Invoke();
    }


    public static void InstantiaateObject(GameObject gameObject)
    {
        GameObject.Instantiate(gameObject);
    }

    public static void LockMouse()
    {
        // 锁定鼠标到屏幕中心
        Cursor.lockState = CursorLockMode.Locked;

        // 可见性控制
        Cursor.visible = false;
    }

    public static void  UnlockMouse()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public static void MouseLocking(bool locked)
    {
        if(locked)
            LockMouse();
        else
            UnlockMouse();
    }

    public static void ExitGame()
    {
        #if UNITY_EDITOR
            EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public static StructState GetState(StructIdPath idp)
    {
        return bodies.datas[idp.bodyIndex].structs[idp.structIndex];
    }

    public static bool TryGetState(StructIdPath idp, out StructState state)
    {
        state = null;
        if (idp.IsNull())
            return false;
        state = GetState(idp);
        return true;
    }

    public static StructData GetData(StructState state)
    {
        return structsInfo.Get(state.type).data;
    }

    public static void Log(object message)
    {
        log.Write(message.ToString());
    }

    public static void dLog(object message)
    {
        Debug.Log(message);
    }
}
public delegate void Meth();

public static class SMath
{
    public static float Angle(Vector3 dir)
    {
        return Vector3.SignedAngle(Vector3.right, dir, Vector3.down);
    }
    public static float Angle(Vector2 dir)
    {
        Vector3 v = dir;
        return Angle(v);
    }
    public static float AngleStandardization(float angle)
    {
        angle %= 360;
        if (angle < 0)
            angle += 360;
        return angle;
    }
    public static float Smooth(float x)
    {
        x *= degRad;
        return math.sin(x);
    }
    public static float Smooth(float timeMax, float time)
    {
        float t = time / timeMax * 90 * degRad;
        return Sin(t);
    }
    public static float Parabola(float x, float p)
        => math.pow(x, p);
    public static float Abs(float v)
        => Mathf.Abs(v);
    public static int Abs(int v) => Mathf.Abs(v);

    public static float degRad = Mathf.Deg2Rad;

    public static float pi = math.PI;
    public static float Cos(float x)
        => math.cos(x);
    public static float CosA(float angle)
    {
        angle *= degRad;
        return math.cos(angle);
    }
    public static float Sin(float x)
        => math.sin(x);
    public static float SinA(float angle)
    {
        angle *= degRad;
        return math.sin(angle);
    }
    public static int Random(int seed, int max, int min)
    {
        UnityEngine.Random.InitState(seed);
        return UnityEngine.Random.Range(min, max);
    }
    public static float Random(int seed, float max, float min)
    {
        UnityEngine.Random.InitState(seed);
        return UnityEngine.Random.Range(min, max);
    }
    public static float Random(float max, float min)
    {
        return UnityEngine.Random.Range(min, max);
    }
    public static int Random(int max, int min)
    {
        return UnityEngine.Random.Range(min, max);
    }
    public static bool Random()
    {
        return Random(1, 0) == 0;
    }
    public static int RandomInt()
    {
        return Random(int.MaxValue, int.MinValue);
    }
    public static int Floor(float var)
    {
        return (int)math.floor(var);
    }
    /// <summary>
    /// get vec2 from angle
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    public static Vector2 GetVector(float angle) => new(CosA(angle), SinA(angle));
    public static float PerlingNoise(float x, float y)
    {
        return Mathf.PerlinNoise(x, y);
    }

    public static List<float> InsertInOrder(float inserted, List<float> list, bool descending = true)
    {
        int low = 0;
        int high = list.Count;

        while (low < high)
        {
            int mid = (low + high) / 2;
            if (descending)
            {
                if (list[mid] < inserted)
                    high = mid;
                else
                    low = mid + 1;
            }
            else
            {
                if (list[mid] > inserted)
                    high = mid;
                else
                    low = mid + 1;
            }
        }

        List<float> v = new(list);
        v.Insert(low, inserted);
        return v;
    }
    public static List<List<T>> SliptList<T>(List <T> list, int size=30)
    {
        var result = list?.Select((item, index) => new { item, index })
            .GroupBy(x => x.index / size)
            .Select(g => g.Select(x => x.item).ToList())
            .ToList();
        return result;
    }
    public static class Spr
    {
        /// <summary>
        /// 通过本地 PNG 文件路径创建 Sprite
        /// </summary>
        /// <param name="filePath">PNG 文件完整路径</param>
        /// <param name="pixelsPerUnit">Sprite 的 Pixels Per Unit，默认 100</param>
        /// <param name="pivot">Sprite 的 Pivot（0~1），默认中心</param>
        public static Sprite LoadFromPNG(
            string filePath,
            float pixelsPerUnit = 100f,
            Vector2? pivot = null)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"PNG 文件不存在: {filePath}");
                return null;
            }

            byte[] pngData = File.ReadAllBytes(filePath);

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(pngData);

            // 自动设置纹理属性
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.anisoLevel = 1;

            Rect rect = new Rect(0, 0, texture.width, texture.height);
            Vector2 spritePivot = pivot ?? new Vector2(0.5f, 0.5f);

            Sprite sprite = Sprite.Create(
                texture,
                rect,
                spritePivot,
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect
            );

            sprite.name = Path.GetFileNameWithoutExtension(filePath);

            return sprite;
        }

        public static bool TryLoadFromPNG(string filePath,
            out Sprite sprite,
            float pixelsPerUnit = 100f,
            Vector2? pivot = null)
        {
            sprite = LoadFromPNG(filePath, pixelsPerUnit, pivot);
            return !sprite;
        }
    }
}

public class StructInfo
{
    public GameObject template;
    public SMesh.RuntimeFace[]  faces;
    public int[] connectorFaceIndexes;
    public GameObject facesTamplate;
    public GameObject connectorTamplate;
    public StructData data;
    public Updater updater;
    public Sprite sprite;
}