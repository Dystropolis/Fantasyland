using UnityEngine;

public class EnemyDropTable : MonoBehaviour
{
    [System.Serializable]
    public struct DropOption
    {
        public GameObject prefab;
        [Range(0f, 1f)] public float chance;
    }

    public DropOption[] drops;

    public void SpawnDrop(Vector3 position)
    {
        // pick a random drop, independent roll per option, first success wins
        for (int i = 0; i < drops.Length; i++)
        {
            if (drops[i].prefab == null) continue;
            if (Random.value <= drops[i].chance)
            {
                Instantiate(drops[i].prefab, position, Quaternion.identity);
                break;
            }
        }
    }
}
