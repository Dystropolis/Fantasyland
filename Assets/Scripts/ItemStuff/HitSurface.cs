using UnityEngine;

public class HitSurface : MonoBehaviour
{
    public enum SurfaceType
    {
        Flesh,
        Bone,
        Wood,
        Stone,
        Metal,
        Dirt,
        Default
    }

    public SurfaceType surfaceType = SurfaceType.Default;
}
