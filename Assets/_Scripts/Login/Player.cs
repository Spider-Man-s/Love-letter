using Fusion;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using LoveLetter.Networking;
using System.Linq;

public class Player : NetworkBehaviour
{
    [Networked] public int SeatIndex { get; set; }
    [Networked] public NetworkString<_16> PlayerName { get; set; }
    [Networked] public int AvatarId { get; set; }
    [Networked] public bool IsProtectedNet { get; set; }


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

        // Register THIS player object on every machine
        BasicSpawner.Instance.RegisterSpawnedPlayer(Object.InputAuthority, Object);

        IsLocal = Object.HasInputAuthority;

        if (IsLocal)
        {
            StartCoroutine(UpdateLocalSeatLater());

            RPC_SetNameAndAvatar(
                BasicSpawner.PlayerData.LocalPlayerName,
                BasicSpawner.PlayerData.LocalAvatarId
            );
        }

        UpdateVisuals();
        Invoke(nameof(NotifySeatArranger), 0.1f);
    }


    private System.Collections.IEnumerator UpdateLocalSeatLater()
    {
        yield return null; // Wait 1 frame for SeatIndex to sync from server
        BasicSpawner.PlayerData.LocalSeatIndex = SeatIndex;
    }

    // ====================================================================
    // UPDATE LOOP (Fusion 2 replacement for OnChanged)
    // ====================================================================
    public override void Render()
    {
        if (SeatIndex != lastSeatIndex)
        {
            lastSeatIndex = SeatIndex;
            HandleSeatIndexChanged();

            if (Object.HasInputAuthority)
            {
                Debug.Log($"[LOCAL] SeatIndex replicated correctly: {SeatIndex}");
                BasicSpawner.PlayerData.LocalSeatIndex = SeatIndex;
            }
        }

        UpdateVisuals();
    }


    private void HandleSeatIndexChanged()
    {
        Debug.Log($"[SeatIndex Changed] Player={Object.InputAuthority}, Seat={SeatIndex}");

        if (Object.HasInputAuthority)
        {
            // Same as Spawned(): delayed safe update
            StartCoroutine(UpdateLocalSeatLater());
        }
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_SendLocalHand(int seatIndex, int[] cardTypes)
    {

        var cards = cardTypes
            .Select(t => new Card((CardType)t))
            .ToList();

        Debug.Log($"[CLIENT] Received hand update for seat {seatIndex}. Cards: {string.Join(",", cardTypes)}");

        // FIX: Sync real game state on client
        GameManager.Instance.GetPlayer(seatIndex).Hand = cards;

        // UI update
        TableUIController.Instance.SetLocalHand(seatIndex, cards);
    }


    // ====================================================================
    // ROLES RPC
    // ====================================================================
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_ShowBaronResult(int myCard, int opponentCard, int result)
    {
        TargetSelectionUI.Instance.ShowBaronDuel(myCard, opponentCard, result);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_ShowPriestResult(int opponentCard)
    {
        TargetSelectionUI.Instance.ShowPriestCard(opponentCard);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_OpenChancellorUI(int seatIndex)
    {
        Debug.Log("[CLIENT] RPC_OpenChancellorUI received for seat=" + seatIndex);

        int localSeat = BasicSpawner.PlayerData.LocalSeatIndex;
        Debug.Log($"[CLIENT] Local seat = {localSeat}");


        if (localSeat != seatIndex)
        {
            Debug.Log("[CLIENT] Ignoring Chancellor UI (not for this player)");
            return;
        }

        Debug.Log("[CLIENT] Opening Chancellor UI NOW.");
        TargetSelectionUI.Instance.OpenChancellorUI();
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SubmitChancellorChoices(int[] choices)
    {
        GameManager.Instance.ServerResolveChancellor(Object.InputAuthority, choices);
    }



}
