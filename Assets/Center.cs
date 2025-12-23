using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public static class ct
{
    public static Set setting = new();
    public static Log log = new();
    public static Action action = new();
    public static Dictionary<string,InputAction> acts = new();
    public static Vector3 playerPosition;//updated every frame
    public static Vector2 wasdDirection;
    public static Vector2 mousePosition;
    /// <summary>
    /// delta position of mouse this frame
    /// </summary>
    public static Vector2 mouseDirection;
    public static bool playerCanMove = false;
    public static bool mouseCanMove = false;
    public static float fps;
    public static float yawCamera;
    public static float pitchCamera;

    public static Transform bodiesParent;
    public static GameObject defualtBody;
    public static Vector3 pp
    {
        get => playerPosition;
        set => playerPosition = value;
    }
    
    public static Bodies bodies = new();
    public static MouseRaycast mousecast;
    /// <summary>
    /// all type of mesh loaded
    /// </summary>
    public static Dictionary<string, Mesh> meshTypes = new();
    public static Dictionary<string, LogicalFace[]> meshFaces = new();
    public static Dictionary<string, StructData> structDatas = new();


    public static Material defaultMat;

    public static event Meth updatePerTick;
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

    public static class V3
    {
        /// <summary>
        /// around parallele by plane xz
        /// </summary>
        public static Vector3 ParaAround(Vector3 center, float angle, float radius)
        {
            angle *= degRad;
            Vector3 rela = new Vector3(Cos(angle), 0, Sin(angle)) * radius;

            return center + rela;
        }
        public static float Length(Vector3 to, Vector3 from)
        {
            Vector3 r = to - from;
            return r.magnitude;
        }
        public static Vector3 GetVector(float x = 0, float y = 0, float z = 0)
            => new(x, y, z);
        /// <summary>
        /// get from a plan position |||
        /// => (x,h,y);
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static Vector3 GetVector(Vector2 vec, float height = 0) => new(vec.x, height, vec.y);
        public static Vector3 Parse(string p)
        {
            try
            {
                p = p.TrimStart('{');
                p = p.TrimEnd('}');
                string[] s = p.Split(',');
                return new(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
            }
            catch
            {
                return Vector3.zero;
            }
        }

        public static Vector3 DirectionAdjustment(Vector3 dir, float angle)
        {
            float b = Angle(dir);
            float r = b - 90 + angle;

            return GetVector(SMath.GetVector(r)) * dir.magnitude;
        }
    }
    public static class V2
    {
        public static Vector2Int Floor(Vector2 position)
        {
            return new(SMath.Floor(position.x), SMath.Floor(position.y));
        }
        public static float Length(Vector2 from, Vector2 to)
        {
            Vector2 v = from - to;
            return v.magnitude;
        }
        public static Vector2Int Random(Vector2Int max, Vector2Int min)
        {
            return new(SMath.Random(max.x, min.x), SMath.Random(max.y, min.y));
        }
        public static Vector2 Random(float max, float min)
        {
            return new(SMath.Random(max, min), SMath.Random(max, min));
        }
        public static Vector2 RandomByDirection(float dirangle, float angleArea)
        {
            float a = angleArea / 2;
            float b = SMath.Random(a, -a);
            float c = dirangle + b;
            return GetVector(c);
        }
        public static Vector2 RandomByDirection(Vector2 dir, float dirangle)
        {
            float a = Angle(dir);
            return RandomByDirection(a, dirangle);
        }
    }
    public static class Spr
    {
        public static int pxPerUnit = 32;
        public static Vector2Int GetDistance(Sprite sprite)
        {
            Texture2D tex = sprite.texture;
            Color co = new();
            Vector2Int v = new();
            for (int x = 0; x < 32; x++)
            {
                bool found = false;
                for (int i = 0; i < 32; i++)
                {
                    if (tex.GetPixel(x, i) != co)
                    {
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    v.x = x + 1;
                    break;
                }
            }
            for (int y = 0; y < 32; y++)
            {
                bool found = false;
                for (int i = 0; i < 32; i++)
                {
                    if (tex.GetPixel(i, y) != co)
                    {
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    v.y = y + 1;
                    break;
                }
            }
            return v;
        }
        /// <summary>
        /// Get area of opaque pixels
        /// </summary>
        /// <param name="sprite"></param>
        /// <returns></returns>
        public static Rect GetValidPixels(Sprite sprite)
        {
            return GetValidPixels(sprite.texture, sprite.rect);
        }
        public static Rect GetValidPixels(Texture2D texture, Rect spriteRect)
        {
            //get sprite area
            int startX = (int)spriteRect.x;
            int startY = (int)spriteRect.y;
            int width = (int)spriteRect.width;
            int height = (int)spriteRect.height;

            int minX = width, maxX = 0, minY = height, maxY = 0;
            bool hasOpaquePixel = false;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = texture.GetPixel(startX + x, startY + y);

                    if (pixel.a > 0) //check just opaque pixel
                    {
                        hasOpaquePixel = true;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (hasOpaquePixel)
            {
                Debug.Log($"[SMath.Spr]Area of opaque px: minX={minX}, maxX={maxX}, minY={minY}, maxY={maxY}");
            }
            else
            {
                Debug.Log("[SMath.Spr]Has not opaque area!!");
            }

            return new(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

    }
}