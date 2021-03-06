﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;

// Developer:   Kyle Aycock
// Date:        11/10/2016
// Description: This class is responsible for bouncing input to the
//              object representing the player, which bounces it to the server.
//              It is currently also responsible for controlling the UI.
//              It is sufficient for now, but probably should networked later.

// Developer:   Nizar Kury
// Date:        11/17/2016
// Description: Added the SwitchShip() class which deals with a player picking a ship
//              they want to use. Also added shipIcons instance variable for the 
//              representation of a person's selection.

// Developer:   Kyle Aycock
// Date:        2/1/2017
// Description: Adapted to work with new team system

// Developer:   Kyle Aycock
// Date:        2/24/2017
// Description: Revamped this system to manage the flow of the lobby, separating
//              it into phases as prescribed by the associated design doc.

// Developer:   Kyle Aycock
// Date:        3/24/2017
// Description: Removed some unneeded code

public class LobbyManager : NetworkBehaviour
{
    [Header("Team Select")]
    public GameObject teamSelectContainer;
    public GameObject startButton;
    public Text lobbyText;
    public Text ipText;
    public Text lanIPText;
    public Transform redTeam;
    public Transform blueTeam;
    public GameObject gameOptions;
    public Text pointsToWin;
    public Text matchTime;

    [Header("Ship Select")]
    public GameObject shipSelectContainer;
    public GameObject[] ShipSelectNameContainers;
    public GameObject lockButton;
    public Toggle humanToggle;
    public Text shipTimer;
    public Text infoText;

    [Header("Preparing")]
    public GameObject preparingContainer;
    public Text preparingTimer;
    public GameObject[] redIcons;
    public GameObject[] blueIcons;

    [SyncVar(hook = "OnHostNameChange")]
    public string hostName;
    [SyncVar(hook = "OnHostPubIPChange")]
    public string hostPublicIP;
    [SyncVar(hook = "OnHostLANIPChange")]
    public string hostLANIP;
    public Sprite[] shipIcons;

    public static LobbyManager instance;

    private MultiplayerManager manager;
    private float timer;

    //Simple enum to track lobby state
    public enum LobbyState
    {
        TeamSelect,
        ShipSelect,
        Preparing
    }
    public LobbyState currentState = LobbyState.TeamSelect;

    // Use this for initialization
    void Start()
    {
        instance = this;
        //Variable initialization
        manager = MultiplayerManager.GetInstance();

        //This if statement statement checks if it's running on the host
        if (!MultiplayerManager.IsHost())
        {
            startButton.SetActive(false);
            gameOptions.SetActive(false);
        } else
        {
            CmdSetHostName(MultiplayerManager.GetLocalPlayer().name);
            CmdSetHostIP(MultiplayerManager.GetLocalIPAddress(), "Fetching...");
            StartCoroutine(GetAssignPublicIPAddress());
        }

        OnHostNameChange(hostName);
        OnHostPubIPChange(hostPublicIP);
        OnHostLANIPChange(hostLANIP);
    }

    void Update()
    {
        if(timer > 0)
        {
            timer -= Time.deltaTime;
            string timeString = "" + (timer <= 9 ? "0" : "") + Mathf.CeilToInt(timer);
            shipTimer.text = "Time Remaining: " + timeString;
            preparingTimer.text = timeString;
            if (timer <= 0) preparingTimer.text = "Loading, please wait...";
            if(timer <= 0 && isClient && hasAuthority)
            {
                if(currentState == LobbyState.ShipSelect)
                {
                    CmdLockShips();
                } else
                {
                    CmdEnterGame();
                }
            }
        }
    }

    /// <summary>
    /// Returns true if a new player can join the lobby
    /// </summary>
    /// <returns></returns>
    public bool IsJoinable()
    {
        return currentState == LobbyState.TeamSelect;
    }

    /// <summary>
    /// Called when a player clicks a team region to switch to that team
    /// </summary>
    /// <param name="team">Index in array of teams to switch to</param>
    public void SwitchTeam(int team)
    {
        GetLocalPlayer().GetComponent<PlayerIconController>().CmdChangeTeam((short)team);
    }

    public void PointsValueChanged(float newVal)
    {
        int points = (int)newVal;
        pointsToWin.text = points.ToString();
        ((TeamDeathmatch)manager.currentGamemode).pointsToWin = points; //no need for networking this, it already happens on host = server
    }

    public void TimeValueChanged(float newVal)
    {
        int time = (int)newVal;
        matchTime.text = time.ToString();
        ((TeamDeathmatch)manager.currentGamemode).timeRemaining = time*60; //no need for networking this, it already happens on host = server
    }

    // NK
    /// <summary>
    /// Called when a player clicks a ship button to switch to that ship
    /// </summary>
    /// <param name="ship">Index in array of ships to switch to</param>
    public void SwitchShip(int ship)
    {
        manager.localShipType = (Ship)ship;
        GetLocalPlayer().GetComponent<PlayerIconController>().CmdChangeShip((Ship)ship);
    }

    /// <summary>
    /// Locks in the player's choice of ship, moving the lobby to the next phase if all players have locked in
    /// </summary>
    public void LockIn()
    {
        lockButton.GetComponent<Button>().interactable = false;
        GetLocalPlayer().GetComponent<PlayerIconController>().CmdLockIn();
    }

    /// <summary>
    /// Convenience method to fetch local player object (only works clientside)
    /// </summary>
    private GameObject GetLocalPlayer()
    {
        return manager.client.connection.playerControllers[0].gameObject;
    }

    private IEnumerator GetAssignPublicIPAddress()
    {
        WWW myExtIPWWW = new WWW("http://checkip.dyndns.org");
        if (myExtIPWWW == null) yield break;
        yield return myExtIPWWW;
        string myExtIP = myExtIPWWW.text;
        myExtIP = myExtIP.Substring(myExtIP.IndexOf(":") + 1);
        myExtIP = myExtIP.Substring(0, myExtIP.IndexOf("<"));
        CmdSetHostIP(MultiplayerManager.GetLocalIPAddress(), myExtIP);
    }

    /// <summary>
    /// Exists only to bounce messages to the client
    /// Sets the host's name for display in the lobby
    /// </summary>
    /// <param name="name"></param>
    [Command]
    public void CmdSetHostName(string name)
    {
        hostName = name;
    }

    //Hooked onto SyncVar hostName
    public void OnHostNameChange(string newName)
    {
        hostName = newName;
        lobbyText.text = newName + "'s Lobby";
    }

    //Hooked onto SyncVar hostName
    public void OnHostPubIPChange(string newIP)
    {
        hostPublicIP = newIP;
        ipText.text = newIP;
    }

    //Hooked onto SyncVar hostName
    public void OnHostLANIPChange(string newIP)
    {
        hostLANIP = newIP;
        lanIPText.text = newIP;
    }

    [Command]
    public void CmdSetHostIP(string lan, string pub)
    {
        hostPublicIP = pub;
        hostLANIP = lan;
    }

    /// <summary>
    /// Notifies server that lobby is moving to ship select
    /// </summary>
    [Command]
    public void CmdLockTeams()
    {
        LockTeams();
        RpcLockTeams();
    }

    /// <summary>
    /// Notifies clients that lobby is moving to ship select, performing necessary actions
    /// </summary>
    [ClientRpc]
    public void RpcLockTeams()
    {
        LockTeams();
        List<Player> list = MultiplayerManager.GetInstance().playerList;
        Player localPlayer = MultiplayerManager.FindPlayer(GetLocalPlayer().GetComponent<NetworkIdentity>().netId);
        string info = "You are on the " + (localPlayer.team == 0 ? "Red" : "Blue") + " team ";
        List<Player> teammates = list.FindAll(p => p.team == localPlayer.team && !p.Equals(localPlayer));
        if (teammates.Count > 0)
        {
            info += "(Teammates: ";
            for (int i = 0; i < teammates.Count - 1; i++)
                info += teammates[i].name + ", ";
            info += teammates[teammates.Count - 1].name + ")";
        }
        infoText.text = info;
        //Disable other team's player names
        foreach(Player p in list)
        {
            if(p.team != localPlayer.team)
            {
                ClientScene.FindLocalObject(p.objectId).SetActive(false);
            }
        }
        humanToggle.isOn = true;
    }

    /// <summary>
    /// Performs some common operations when moving to ship select phase
    /// </summary>
    public void LockTeams()
    {
        currentState = LobbyState.ShipSelect;
        teamSelectContainer.SetActive(false);
        shipSelectContainer.SetActive(true);
        timer = 60;
    }

    /// <summary>
    /// Notifies server that lobby is moving to preparing phase
    /// </summary>
    [Command]
    public void CmdLockShips()
    {
        LockShips();
        RpcLockShips();
    }

    /// <summary>
    /// Notifies clients that lobby is moving to preparing phase
    /// </summary>
    [ClientRpc]
    public void RpcLockShips()
    {
        LockShips();
    }

    /// <summary>
    /// Performs common operations when moving to preparing phase
    /// </summary>
    public void LockShips()
    {
        currentState = LobbyState.Preparing;
        shipSelectContainer.SetActive(false);
        preparingContainer.SetActive(true);
        timer = 10;

        List<Player> playerList = manager.playerList;
        int redIndex = 0;
        int blueIndex = 0;
        for (int i = 0; i < playerList.Count; i++)
        {
            Player player = playerList[i];
            if (player.team == 0)
            {
                redIcons[redIndex].transform.FindChild("Image").GetComponent<Image>().sprite = shipIcons[(short)player.ship];
                redIcons[redIndex].GetComponentInChildren<Text>().text = player.name;
                redIndex++;
            } else
            {
                blueIcons[blueIndex].transform.FindChild("Image").GetComponent<Image>().sprite = shipIcons[(short)player.ship];
                blueIcons[blueIndex].GetComponentInChildren<Text>().text = player.name;
                blueIndex++;
            }
        }
    }

    /// <summary>
    /// Tells server to start the game
    /// </summary>
    [Command]
    public void CmdEnterGame()
    {
        MultiplayerManager.GetInstance().EnterGame();
    }

    public void QuitToMenu()
    {
        MultiplayerManager.GetInstance().StopHost();
    }
}
