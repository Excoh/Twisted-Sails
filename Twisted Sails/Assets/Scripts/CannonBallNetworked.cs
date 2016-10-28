﻿// The CannonBall class destroys the instance of the CannonBall GameObject it is 
// attached to if the vertical position of the CannonBall is below the despawnDepth
// and applies realistic gravity to the cannonBall. The scaleFactor scales gravity
// to the size of the game world (for example, a 1/10th scale game world needs 1/10th
// the gravity and a scaleFactor of 0.1).
//
// Edward Borroughs 9/15/2016
// 10th Oct 16 Scotty Molt
//	- Cannonball Scale is now static and set on Start() to ensure uniformity across Server and Clients
//	- Added helper function SetInitScale(Vetor3) to be called from BroadSideCannonNetworked script
//	- Added initScale so cannonballs across network spawn at same size

// Developer: Kyle Aycock
// Date: 10/26/2016
//	- Aesthetic changes. Upon reaching water level, spawn splash particlesystem.
//	- Variables added to ensure only one splash is created
//	- Changes to movement upon entering water (higher drag)

using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class CannonBallNetworked : NetworkBehaviour {

	public float despawnDepth = -3f;
	public float scaleFactor = 0.1f;
    public GameObject splashPrefab;
	private static Vector3 initScale = Vector3.zero;
    private bool splashed;

    [SyncVar]
    public NetworkInstanceId owner;

	//Set size of cannonball
	void Start()
	{
        splashed = false;
		this.transform.localScale = initScale;
	}

    //Set default scale of all cannonballs
    public static void SetInitScale (Vector3 newInitScale)
	{
		initScale = newInitScale;
	}

	private void FixedUpdate () {
		// vertical displacement = initial vertical velocity * time + .5 * -9.81 * scaleFactor * time^2
		this.transform.GetComponent<Rigidbody> ().AddForce(new Vector3(0, -9.81f * scaleFactor, 0), ForceMode.Acceleration);

	    //Note: Water level assumed to be 0. Changes to water level must be reflected here.
        if(transform.position.y < 0 && !splashed)
        {
            Instantiate(splashPrefab, transform.position, splashPrefab.transform.rotation);
            splashed = true;
            GetComponent<Rigidbody>().drag = 1f;
        }

		//Check for destruction
		if (this.transform.position.y <= despawnDepth)
		{
			Object.Destroy(this.gameObject);
		}
	}
}
