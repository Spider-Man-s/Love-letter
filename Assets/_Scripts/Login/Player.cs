using Fusion;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Diagnostics;
using LoveLetter.Networking;

public class Player : NetworkBehaviour
{
    [Networked] public int SeatIndex { get; set; }
    [Networked] public NetworkString<_16> PlayerName { get; set; }
    [Networked] public int AvatarId { get; set; }

    public bool IsLocal { get; private set; }

    [Header("Visual References")]
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text upNameLabel;
    [SerializeField] private Image avatarImageRenderer;
    [SerializeField] private GameObject frameOwn;
    [SerializeField] private GameObject frameEnemy;



    public override void Spawned()
    {
        IsLocal = Object.HasInputAuthority;
        UpdateVisuals();
        if (IsLocal)
        {
            RPC_SetNameAndAvatar(
                BasicSpawner.PlayerData.LocalPlayerName,
                BasicSpawner.PlayerData.LocalAvatarId
            );
        }
        Invoke(nameof(NotifySeatArranger), 0.1f);
    }

    public void Initialize(string name, int seat, int avatarId)
    {
        SeatIndex = seat;

        if (Runner.IsServer)
        {
            PlayerName = name;
            AvatarId = avatarId;
        }
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {

        if (nameLabel != null)
            nameLabel.text = PlayerName.ToString();
        if (upNameLabel != null)
            upNameLabel.text = PlayerName.ToString();

        if (avatarImageRenderer != null && AvatarScriptable.Instance != null)
        {
            var avatars = AvatarScriptable.Instance.avatars;
            if (AvatarId >= 0 && AvatarId < avatars.Length)
                avatarImageRenderer.sprite = avatars[AvatarId];
        }
        if (IsLocal)
        {
            frameOwn.SetActive(true);
            frameEnemy.SetActive(false);
        }
        else
        {
            frameOwn.SetActive(false);
            frameEnemy.SetActive(true);
        }
        UnityEngine.Debug.Log($"Player visuals updated | Name = {PlayerName} | AvatarId = {AvatarId}");
    }

    public override void Render()
    {
        UpdateVisuals();
    }
    private void NotifySeatArranger()
    {
        var arranger = FindObjectOfType<SeatArranger>();
        arranger?.ArrangeSeats();
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetNameAndAvatar(string name, int avatarId)
    {
        PlayerName = name;
        AvatarId = avatarId;
    }

}
