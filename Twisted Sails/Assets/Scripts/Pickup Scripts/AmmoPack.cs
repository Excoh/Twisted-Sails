﻿using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class AmmoPack : InteractiveObject
{
    public int ammoAmmount = 1;
    public float packRespawnTime = 90f;
    public MeshRenderer packMesh;
    public Collider packCollider;
    public Material onMat;
    public Material offMat;

    public void Start()
    {
        packMesh.material = offMat;
        packCollider.enabled = false;
        if(isServer)
            InvokeRepeating("Respawn", packRespawnTime, packRespawnTime);
    }

    void Respawn()
    {
        RpcRespawn();
    }

    [ClientRpc]
    void RpcRespawn()
    {
        packMesh.material = onMat;
        packCollider.enabled = true;
    }

    public override void OnInteractWithPlayerTrigger(Health playerHealth, GameObject playerBoat, StatusEffectsManager manager, Collider collider)
    {
        //notifies the player events system that the player who interacted with this object picked up a health pack (this object)
        //also sets isHealthPack to true, since this is a health pack

        if (isServer)
        {
            if (playerBoat.GetComponent<HeavyWeapon>().AmmoCount >= playerBoat.GetComponent<HeavyWeapon>().ammoCapacity) return;
            playerBoat.GetComponent<HeavyWeapon>().AddAmmo(ammoAmmount);
            Player.ActivateEventPlayerPickup(MultiplayerManager.FindPlayer(playerBoat.GetComponent<NetworkIdentity>().netId), true);
            RpcConsumePack(playerBoat.GetComponent<NetworkIdentity>().netId);
        }
    }

    [ClientRpc]
    public void RpcConsumePack(NetworkInstanceId player)
    {
        GameObject playerBoat = ClientScene.FindLocalObject(player);
        Health playerHealth = playerBoat.GetComponent<Health>();
        //play sounds and send command for ammo
        if (MultiplayerManager.GetLocalPlayer() != null && MultiplayerManager.GetLocalPlayer().objectId == playerBoat.GetComponent<NetworkIdentity>().netId)
        {
            playerBoat.transform.Find("ShipSounds").Find("AmmoPickupVO").GetComponent<AudioSource>().Play();
            //Debug.Log(MultiplayerManager.GetLocalPlayer().name);
        }
        Instantiate(playerHealth.powerupParticle, playerBoat.transform).transform.localPosition = Vector3.zero;

        playerBoat.transform.Find("ShipSounds").Find("AmmoPickup").GetComponent<AudioSource>().Play();

        packMesh.material = offMat;
        packCollider.enabled = false;
    }

    public override bool DoesDestroyInInteract()
    {
        return false;
    }

}
