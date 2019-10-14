using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TankNt = Tanks.Networking;

public class AgoraPlayerController
{
    private Dictionary<TankNt.NetworkPlayer, uint> NetworkToAgoraIDMap = new Dictionary<TankNt.NetworkPlayer, uint>();

    private TankNt.NetworkPlayer pendingPlayer;
    private uint? pendingAgoraId;

    private static AgoraPlayerController _instance;
    public static AgoraPlayerController instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new AgoraPlayerController();
            }
            return _instance;
        }
    }

    private AgoraPlayerController()
    {
        ResetIds();
    }

    public void AddNetworkPlayer(TankNt.NetworkPlayer playerId) {
        if (pendingAgoraId.HasValue)
        {
            NetworkToAgoraIDMap[playerId] = (uint)pendingAgoraId;
            pendingAgoraId = null;
            return;
        }

        if (pendingPlayer == null) {
            pendingPlayer = playerId;
        } else {
            Debug.LogError(string.Format("Player id exists:{0} while adding:{1}", pendingPlayer, playerId));
        }
    }

    /// <summary>
    ///   A networking user join callback occurs,save it with store networkid
    /// </summary>
    /// <param name="uid"></param>
    public void AddAgoraPlayer(uint uid)
    {
        if (pendingPlayer != null)
        {
            NetworkToAgoraIDMap[pendingPlayer] = uid;
            pendingPlayer = null;
        }
        else
        {
            pendingAgoraId = uid;
        }
    }
    /// <summary>
    ///   Assign local player uid upon ChannelJoin event
    /// </summary>
    /// <param name="uid"></param>
    public void OnChannelJoins(uint uid)
    {
        if (TankNt.NetworkPlayer.s_LocalPlayer != null)
        { 
            Debug.Log("Channel joined as local player");
          //  NetworkToAgoraIDMap[TankNt.NetworkPlayer.s_LocalPlayer] = uid;
            // clear the pendingPlayer which sets at the local player set up
            ResetIds();
        }
        else
        {
            Debug.LogWarning("Channel joined as local player but no networkplayer instance!"); 
        }
    }

    void ResetIds()
    {
        pendingPlayer = null;
        pendingAgoraId = null;
    }

    public void RemoveNetworkPlayer(TankNt.NetworkPlayer player)
    {
        if (NetworkToAgoraIDMap.ContainsKey(player))
        {
            NetworkToAgoraIDMap.Remove(player);
        }
    }

    public uint GetAgoraID(TankNt.NetworkPlayer player)
    {
        if (NetworkToAgoraIDMap.ContainsKey(player))
        {
            return NetworkToAgoraIDMap[player];
        }

        return 0;
    }
}
