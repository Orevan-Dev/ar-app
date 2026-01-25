using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif
public class PermissionsUtilities : MonoBehaviour
{

    bool scan = false;

#if PLATFORM_ANDROID
    private void Awake()
    {

        if (Permission.HasUserAuthorizedPermission(Permission.CoarseLocation))
        {
            // The user authorized use of the microphone.
            scan = false;
        }
        else
        {
            // We do not have permission to use the microphone.
            // Ask for permission or proceed without the functionality enabled.
            Permission.RequestUserPermission(Permission.CoarseLocation);
            scan = true;
        }
        

    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Permission.HasUserAuthorizedPermission(Permission.CoarseLocation) && scan)
        {
            // The user authorized use of the microphone.
            Debug.Log("Location has updated");
            GetComponent<GPSLocation>().GetLocation();
            scan = false;
        }
    }

    public static void CheckCameraPermission()
    {
        if (Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            // The user authorized use of the microphone.
        }
        else
        {
            // We do not have permission to use the microphone.
            // Ask for permission or proceed without the functionality enabled.
            Permission.RequestUserPermission(Permission.Camera);
        }
    }
#endif
}

