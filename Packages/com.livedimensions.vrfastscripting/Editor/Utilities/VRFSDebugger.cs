using UnityEngine;

namespace VRFastScripting.Editor
{
    public static class VRFSDebugger
    {
        public static void Log(string message, Object obj = null)
        {
            Debug.Log("<color=#44DDBF>[VRFastScripting]</color> " + message, obj);
        }
        
        public static void LogWarning(string message, Object obj = null)
        {
            Debug.LogWarning("<color=#44DDBF>[VRFastScripting]</color> " + message, obj);
        }
        
        public static void LogError(string message, Object obj = null)
        {
            Debug.LogError("<color=#44DDBF>[VRFastScripting]</color> " + message, obj);
        }
    }
}