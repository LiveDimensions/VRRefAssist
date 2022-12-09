using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRFastScripting;

public class RunOnBuildTest
{
    [RunOnBuild]
    private static void PrivateTest()
    {
        Debug.Log("PrivateTest");
    }

    [RunOnBuild]
    private static void PublicTest()
    {
        Debug.Log("PublicTest");
    }
}

public static class StaticClassRunOnBuildTest
{
    [RunOnBuild]
    private static void PrivateTest()
    {
        Debug.Log("PrivateTest");
    }

    [RunOnBuild]
    private static void PublicTest()
    {
        Debug.Log("PublicTest");
    }
}