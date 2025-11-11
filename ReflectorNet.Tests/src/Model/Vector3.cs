
namespace com.IvanMurzak.ReflectorNet.Tests.Model
{
    public class Vector3
    {
        public static Vector3 zero = new Vector3(0f, 0f, 0f);
        public static Vector3 one = new Vector3(1f, 1f, 1f);
        public static Vector3 up = new Vector3(0f, 1f, 0f);
        public static Vector3 down = new Vector3(0f, -1f, 0f);
        public static Vector3 left = new Vector3(-1f, 0f, 0f);
        public static Vector3 right = new Vector3(1f, 0f, 0f);
        public static Vector3 forward = new Vector3(0f, 0f, 1f);
        public static Vector3 backward = new Vector3(0f, 0f, -1f);

        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        public Vector3()
        {
            x = 0f;
            y = 0f;
            z = 0f;
        }
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override string ToString()
        {
            return $"Vector3({x}, {y}, {z})";
        }
    }
}