using UnityEngine;

public abstract class Item : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float pickupRadius = 1.2f;
    [SerializeField] private float checkInterval = 0.1f;

    private static Transform cachedPlayerTransform;
    private static Player cachedPlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticCache()
    {
        cachedPlayerTransform = null;
        cachedPlayer = null;
    }

    private float nextCheckTime;
    private float pickupRadiusSqr;

    protected virtual void Awake()
    {
        pickupRadiusSqr = pickupRadius * pickupRadius;
        if (checkInterval < 0.02f)
        {
            checkInterval = 0.02f;
        }
    }

    private void Update()
    {
        if (Time.time < nextCheckTime)
        {
            return;
        }

        nextCheckTime = Time.time + checkInterval;

        if (!TryGetPickupPlayer(out Player player))
        {
            return;
        }

        Vector2 delta = (Vector2)player.transform.position - (Vector2)transform.position;
        if (delta.sqrMagnitude > pickupRadiusSqr)
        {
            return;
        }

        if (!CanBePickedBy(player))
        {
            return;
        }

        OnPicked(player);
    }

    private bool TryGetPickupPlayer(out Player player)
    {
        if (cachedPlayer == null || cachedPlayerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject == null)
            {
                player = null;
                return false;
            }

            cachedPlayerTransform = playerObject.transform;
            cachedPlayer = playerObject.GetComponent<Player>();
        }

        if (cachedPlayer == null)
        {
            player = null;
            return false;
        }

        player = cachedPlayer;
        return true;
    }

    protected virtual bool CanBePickedBy(Player player)
    {
        return player != null;
    }

    protected abstract void OnPicked(Player player);
}
