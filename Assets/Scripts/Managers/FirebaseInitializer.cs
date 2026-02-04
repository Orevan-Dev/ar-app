using UnityEngine;
using Firebase;
using Firebase.Extensions;
using System;

public class FirebaseInitializer : MonoBehaviour
{
        public static FirebaseInitializer Instance { get; private set; }
        public bool IsInitialized { get; private set; } = false;
        public DependencyStatus DependencyStatus { get; private set; } = DependencyStatus.UnavailableOther;

        public static event Action OnFirebaseInitialized;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeFirebase();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeFirebase()
        {
            Debug.Log("[FirebaseInitializer] Checking dependencies...");
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                DependencyStatus = task.Result;
                if (DependencyStatus == DependencyStatus.Available)
                {
                    Debug.Log("[FirebaseInitializer] Firebase is ready.");
                    IsInitialized = true;
                    OnFirebaseInitialized?.Invoke();
                }
                else
                {
                    Debug.LogError($"[FirebaseInitializer] Could not resolve all Firebase dependencies: {DependencyStatus}");
                    // Even if not perfectly available, some parts might work, 
                    // but usually status 17 (Developer Error) comes from missing SHA-1 or similar.
                }
            });
        }
    }
