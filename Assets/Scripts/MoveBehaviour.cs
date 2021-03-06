﻿using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityEngine.Networking;
using System.Collections.Generic;
using UnityEngine.UI;
[RequireComponent(typeof(Rigidbody))]

public class MoveBehaviour : NetworkBehaviour {

    public struct _Transform
    {
        public Transform transform;
    }

    Camera maincamera;
    public Material mymaterial;
    [SerializeField]
    private Rigidbody rb;
    [SerializeField]
    private GameObject weaponHolder;
    [SerializeField]
    private GameObject player;
    private PlayerClass playerClass;
    private int snapfingerid;
    private float timer;
#if UNITY_ANDROID
    public RectTransform ui;
#endif


    private void Start()
    {
#if UNITY_ANDROID
        ui = GameObject.FindGameObjectWithTag("GameController").GetComponent<RectTransform>();
#endif
        playerClass = gameObject.transform.GetComponent<PlayerClass>();
        rb = gameObject.GetComponent<Rigidbody>();
        if(weaponHolder == null)
        {
            Debug.Log("Weapon Holder Empty");
            return;
        }
        if(player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        timer = 0;
        snapfingerid = -1;
        maincamera = Camera.main;
    }

    void FixedUpdate()
    {
        if (PauseMenu.IsOn)
        { 
            rb.velocity = Vector3.zero;
            return;
        }
        rb.velocity = (new Vector3(
        CrossPlatformInputManager.GetAxis("Horizontal") * Time.deltaTime * 100 * playerClass.speed,0f,
        CrossPlatformInputManager.GetAxis("Vertical") * Time.deltaTime * 100 * playerClass.speed));
        Ray ray = maincamera.ScreenPointToRay(CrossPlatformInputManager.mousePosition); //Remove if on android
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f))//do a raycast in direction of the ground, where it hits is the new end point
        {
            Ray RightHeightRay = new Ray(weaponHolder.transform.position, new Vector3(hit.point.x, weaponHolder.transform.position.y, hit.point.z)-weaponHolder.transform.position);
            player.transform.LookAt(RightHeightRay.GetPoint(playerClass.range));
                player.transform.eulerAngles = new Vector3(0, player.transform.eulerAngles.y, player.transform.eulerAngles.z);
            if (Input.GetMouseButton(0)&& (timer>1/playerClass.firerate))
            {
                GameObject myLine = new GameObject();
                myLine.transform.position = weaponHolder.transform.position;
                myLine.AddComponent<LineRenderer>();
                LineRenderer lr = myLine.GetComponent<LineRenderer>();
                lr.material = mymaterial;
                lr.startColor = Color.red;
                lr.endColor = Color.red;
                lr.startWidth = 0.1f;
                lr.endWidth = 0.1f;
                lr.SetPosition(0, weaponHolder.transform.position);
                lr.SetPosition(1, RightHeightRay.GetPoint(playerClass.range));//do the line
                GameObject.Destroy(myLine, 0.5f);
            
                if (Physics.Raycast(new Ray(weaponHolder.transform.position, RightHeightRay.direction), out hit, playerClass.range))
                {
                    if(hit.transform.CompareTag("Player"))
                    {
                        CmdPlayerShot("Player" + hit.transform.GetComponent<NetworkIdentity>().netId.ToString(), playerClass.damage);
                    }
                }
        }
    }
        timer += Time.deltaTime;
    
#if UNITY_ANDROID
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (snapfingerid == -1 &&
                    (touch.position.x < ui.position.x + ui.rect.size.x + 100 && 
                    touch.position.y < ui.position.y + ui.rect.size.y + 100))//if there is no finger on id and the player touches the movement joystick 
                {
                    snapfingerid = touch.fingerId; //it is set to be the snap finger id
                }
                if(snapfingerid != touch.fingerId && timer > 1/gameObject.GetComponent<PlayerClass>().firerate)//if the touch isn't what is on the joystick and the fire rate cooldown is over, it fires a ray(shoot)
                {
                    timer = 0;
                    Ray ray = maincamera.ScreenPointToRay(touch.position);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 100f))//do a raycast in direction of the ground, where it hits is the new end point
                    {
                        Ray normalizedRay = new Ray(gameObject.transform.position, new Vector3(hit.point.x, 0.5f, hit.point.z));
                        Vector3 maxDistance = normalizedRay.GetPoint(gameObject.GetComponent<PlayerClass>().range);
                        maxDistance.y = 0.5f;
                        GameObject myLine = new GameObject();
                        myLine.transform.position = gameObject.transform.position;
                        myLine.AddComponent<LineRenderer>();
                        LineRenderer lr = myLine.GetComponent<LineRenderer>();
                        lr.material = mymaterial;
                        lr.startColor = Color.red;
                        lr.endColor = Color.red;
                        lr.startWidth = 0.1f;
                        lr.endWidth = 0.1f;
                        lr.SetPosition(0, gameObject.transform.position);
                        lr.SetPosition(1, maxDistance);//do the line
                        GameObject.Destroy(myLine, 0.5f);
                        if (Physics.Raycast(new Ray(gameObject.transform.position, maxDistance - gameObject.transform.position), out hit, gameObject.GetComponent<PlayerClass>().range))
                        {
                            Debug.Log(hit.transform.name);
                        }
                    }
                }
            }
        }
        else
        {
            snapfingerid = -1;
        }
        timer += Time.deltaTime;
    }
#endif
    }
    [Command]
    void CmdPlayerShot(string _ID, int damage)
    {
        Debug.Log(_ID + " has ben shot with " + damage);
        PlayerClass player = GameManager.GetPlayer(_ID);
        player.TakeDamage(damage);
    }
}
