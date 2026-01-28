using System.Linq;
using UnityEngine;

public class SeatArranger : MonoBehaviour
{
    [Header("Seat Transforms (index 0 is OWN seat)")]
    public Transform[] seatTransforms;

    private Player[] players;

    private void Start()
    {
        Invoke(nameof(ArrangeSeats), 0.15f);
    }

    public void ArrangeSeats()
    {
        players = FindObjectsOfType<Player>();

        if (players.Length == 0)
            return;

        Player local = players.FirstOrDefault(p => p.IsLocal);
        if (local == null)
            return;

        int mySeat = local.SeatIndex;
        int count = seatTransforms.Length;

        local.transform.SetParent(seatTransforms[0], false);
        local.transform.localPosition = Vector3.zero;

        foreach (var p in players)
        {
            if (p == local)
                continue;

            int global = p.SeatIndex;
            int relativeSeat = (global - mySeat + count) % count;

            p.transform.SetParent(seatTransforms[relativeSeat], false);
            p.transform.localPosition = Vector3.zero;
        }
    }
}
