using Fusion;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Diagnostics;

public class Player : NetworkBehaviour
{
    [Networked] public int SeatIndex { get; set; }
    [Networked] public NetworkString<_16> PlayerName { get; set; }
    [Networked] public int AvatarId { get; set; }

    public bool IsLocal { get; private set; }

    [Header("Visual References")]
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private Image avatarImageRenderer;

    // Called when the object spawns on a client
    public override void Spawned()
    {
        IsLocal = Object.HasInputAuthority;
        UpdateVisuals();
        UnityEngine.Debug.Log($"Player spawned | Name = {PlayerName} | Seat = {SeatIndex} | Local = {IsLocal}");
    }

    // Initialize called on server
    public void Initialize(string name, int seat, int avatarId)
    {
        SeatIndex = seat;

        if (Runner.IsServer)
        {
            PlayerName = name;
            AvatarId = avatarId;
        }

        // Update visuals immediately for local client
        UpdateVisuals();
    }

    // Helper to update TMP_Text and SpriteRenderer
    private void UpdateVisuals()
    {

        if (nameLabel != null)
            nameLabel.text = PlayerName.ToString();

        if (avatarImageRenderer != null && AvatarScriptable.Instance != null)
        {
            var avatars = AvatarScriptable.Instance.avatars;
            if (AvatarId >= 0 && AvatarId < avatars.Length)
                avatarImageRenderer.sprite = avatars[AvatarId];
        }
        UnityEngine.Debug.Log($"Player visuals updated | Name = {PlayerName} | AvatarId = {AvatarId}");
    }

    // Optional: called each render frame, keeps visuals synced
    public override void Render()
    {
        UpdateVisuals();
    }
}
