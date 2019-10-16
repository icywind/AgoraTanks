using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TankNt = Tanks.Networking;

public class AgoraPlayerController
{
    private Dictionary<TankNt.NetworkPlayer, uint> NetworkToAgoraIDMap = new Dictionary<TankNt.NetworkPlayer, uint>();
    private List<TankNt.NetworkPlayer> m_NetworkPlayers = new List<TankNt.NetworkPlayer>();
    private List<uint> m_AgoraUserIds = new List<uint>();

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
    }

    public void AddNetworkPlayer(TankNt.NetworkPlayer player) 
    {
        if (player.playerId == 0)
        {
            Debug.LogWarning("List: Player ignore for being LocalPlayer:" + player);
        }
        else
        {
            m_NetworkPlayers.Add(player);
        }
    }

    /// <summary>
    ///   A networking user join callback occurs,save it with store networkid
    /// </summary>
    /// <param name="uid"></param>
    public void AddAgoraPlayer(uint uid)
    {
        Debug.LogWarning("Adding Agora player: " + uid);
        m_AgoraUserIds.Add(uid);
    }
    
    /// <summary>
    ///   Assign local player uid upon ChannelJoin event
    /// </summary>
    /// <param name="uid"></param>
    public void OnChannelJoins(uint uid)
    {
        Reset();
    }

    void Bind()
    {
        int total = Math.Max(m_AgoraUserIds.Count, m_NetworkPlayers.Count);
        for(int i=0; i<total; i++)
        {
            NetworkToAgoraIDMap[m_NetworkPlayers[i]] = m_AgoraUserIds[i];
        }
    }
    
    public void Reset()
    {
        m_NetworkPlayers.Clear();
        m_AgoraUserIds.Clear();
        NetworkToAgoraIDMap.Clear();
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
        if (NetworkToAgoraIDMap.Count == 0)
        {
            Bind();
        }
        
        if (NetworkToAgoraIDMap.ContainsKey(player))
        {
            return NetworkToAgoraIDMap[player];
        }

        return 0;
    }
}
