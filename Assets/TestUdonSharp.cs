using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRFastScripting;

[Singleton] [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class TestUdonSharp : UdonSharpBehaviour
{
    [SerializeField] [GetComponent] private BoxCollider[] players;
    [SerializeField] [GetComponentInChildren] private BoxCollider[] players2;

    [SerializeField] private TestUdonSharp test;
}