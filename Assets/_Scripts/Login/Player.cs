using Fusion;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using LoveLetter.Networking;

public class Player : NetworkBehaviour
{
    [Networked]
    public int SeatIndex { get; set; }

    [Networked]
    public NetworkString<_16> PlayerName { get; set; }

    [Networked]
    public int AvatarId { get; set; }

    public bool IsLocal { get; private set; }

    [Header("Visual References")]
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text upNameLabel;
    [SerializeField] private Image avatarImageRenderer;
    [SerializeField] private GameObject frameOwn;
    [SerializeField] private GameObject frameEnemy;

    private int lastSeatIndex = -1;

    public override void Spawned()
    {
        Debug.Log($"[Player.Spawned] Object {Object.Id} HasInputAuth={Object.HasInputAuthority} => SeatIndex={SeatIndex}");

        IsLocal = Object.HasInputAuthority;

        // Local player sends up their UI identity
        if (IsLocal)
        {
            BasicSpawner.PlayerData.LocalSeatIndex = SeatIndex;

            RPC_SetNameAndAvatar(
                BasicSpawner.PlayerData.LocalPlayerName,
                BasicSpawner.PlayerData.LocalAvatarId
            );
        }

        UpdateVisuals();
        Invoke(nameof(NotifySeatArranger), 0.1f);
    }



    // ====================================================================
    // UPDATE LOOP (Fusion 2 Replacement for OnChanged)
    // ====================================================================
    public override void Render()
    {
        // detect SeatIndex change manually
        if (SeatIndex != lastSeatIndex)
        {
            lastSeatIndex = SeatIndex;
            HandleSeatIndexChanged();
        }

        UpdateVisuals();
    }

    private void HandleSeatIndexChanged()
    {
        Debug.Log($"[SeatIndex Changed] Player={Object.InputAuthority}, Seat={SeatIndex}");

        if (Object.HasInputAuthority)
            BasicSpawner.PlayerData.LocalSeatIndex = SeatIndex;
    }

    // ====================================================================
    // VISUALS
    // ====================================================================

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
    }

    private void NotifySeatArranger()
    {
        var arranger = FindObjectOfType<SeatArranger>();
        arranger?.ArrangeSeats();
    }

    // ====================================================================
    // RPCs
    // ====================================================================

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetNameAndAvatar(string name, int avatarId)
    {
        PlayerName = name;
        AvatarId = avatarId;
    }

    // called from GameManager → send local-visible cards only to the right player
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_SendLocalHand(int seatIndex, int[] cardTypes)
    {
        var cards = new List<Card>();
        foreach (int ct in cardTypes)
            cards.Add(new Card((CardType)ct));

        TableUIController.Instance.SetLocalHand(seatIndex, cards);
    }
}
