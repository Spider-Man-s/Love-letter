using Fusion;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [Networked] public int SeatIndex { get; set; }
    [Networked] public NetworkString<_16> PlayerName { get; set; }
    public bool IsLocal { get; private set; }

    [Header("Visual References")]
    [SerializeField] private TMPro.TMP_Text nameLabel;

    public override void Spawned()
    {
        IsLocal = Object.HasInputAuthority;

        if (nameLabel != null)
            nameLabel.text = PlayerName.ToString();

        Debug.Log($"Player spawned | Name = {PlayerName} | Seat = {SeatIndex} | Local = {IsLocal}");
    }

    public void Initialize(string name, int seat)
    {
        // Only server can set networked properties
        if (Runner.IsServer)
        {
            PlayerName = name;
            SeatIndex = seat;
        }
    }
}
