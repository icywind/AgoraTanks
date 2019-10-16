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
        TankNt.NetworkManager.s_Instance.playerJoined += AddNetworkPlayer;
        TankNt.NetworkManager.s_Instance.playerLeft += RemoveNetworkPlayer;
    }

    ~AgoraPlayerController()
    {
        if (TankNt.NetworkManager.s_Instance != null)
        {
            TankNt.NetworkManager.s_Instance.playerJoined -= AddNetworkPlayer;
            TankNt.NetworkManager.s_Instance.playerLeft -= RemoveNetworkPlayer;
        }
    }

    public void AddNetworkPlayer(TankNt.NetworkPlayer player) 
    {
		Debug.LogFormat("Player joined----> {0}", player);
        if (player.isLocalPlayer)
        {
            Debug.Log("List: Player ignore for being LocalPlayer:" + player);
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
        m_AgoraUserIds.Add(uid);
        Debug.Log("Adding Agora player: " + uid);
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
        int total = Math.Min(m_AgoraUserIds.Count, m_NetworkPlayers.Count);
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
        Debug.LogWarningFormat("Player left and removed:{0}", player);
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

    public void Print()
    {
        Debug.LogFormat("NetWorkPlayers:{0}", String.Join(" : ", m_NetworkPlayers ));
        Debug.LogFormat("AgoraIds:{0}", String.Join(" : ", m_AgoraUserIds ));
    }
}
