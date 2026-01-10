using FistVR;
using H3MP.Networking;
using UnityEngine;

namespace H3MP.Scripts
{
    /// <summary>
    /// Fake client for solo testing of H3MP multiplayer features
    /// Spawns a dummy player that follows you around
    /// </summary>
    public class FakeClient : MonoBehaviour
    {
        public static FakeClient instance;
        
        private GameObject fakePlayerObject;
        private PlayerManager fakePlayerManager;
        private int fakeClientID = 999; // Use high ID to avoid conflicts
        
        // Movement settings
        public float followDistance = 2f;
        public float followSpeed = 3f;
        public float rotationSpeed = 5f;
        
        void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }
        
        public void SpawnFakeClient()
        {
            if (fakePlayerObject != null)
            {
                Mod.LogWarning("Fake client already spawned!");
                return;
            }
            
            if (Mod.playerPrefab == null)
            {
                Mod.LogError("Cannot spawn fake client - player prefab is null!");
                return;
            }
            
            // Spawn the fake player
            fakePlayerObject = Instantiate(Mod.playerPrefab);
            fakePlayerManager = fakePlayerObject.GetComponent<PlayerManager>();
            
            if (fakePlayerManager == null)
            {
                Mod.LogError("Fake client player prefab has no PlayerManager!");
                Destroy(fakePlayerObject);
                return;
            }
            
            // Configure fake player
            fakePlayerManager.ID = fakeClientID;
            fakePlayerManager.username = "FakeClient_TestDummy";
            fakePlayerManager.scene = GameManager.scene;
            fakePlayerManager.instance = GameManager.instance;
            
            // Set IFF to friendly
            if (GM.CurrentPlayerBody != null)
            {
                fakePlayerManager.SetIFF(GM.CurrentPlayerBody.GetPlayerIFF());
            }
            
            // Add to GameManager
            if (!GameManager.players.ContainsKey(fakeClientID))
            {
                GameManager.players.Add(fakeClientID, fakePlayerManager);
            }
            
            // Position behind the real player
            if (GM.CurrentPlayerBody != null)
            {
                Vector3 spawnPos = GM.CurrentPlayerBody.Head.position - GM.CurrentPlayerBody.Head.forward * followDistance;
                fakePlayerObject.transform.position = spawnPos;
            }
            
            Mod.LogInfo("Fake client spawned successfully!", false);
        }
        
        public void DespawnFakeClient()
        {
            if (fakePlayerObject != null)
            {
                if (GameManager.players.ContainsKey(fakeClientID))
                {
                    GameManager.players.Remove(fakeClientID);
                }
                
                Destroy(fakePlayerObject);
                fakePlayerObject = null;
                fakePlayerManager = null;
                
                Mod.LogInfo("Fake client despawned", false);
            }
        }
        
        void Update()
        {
            if (fakePlayerObject == null || fakePlayerManager == null || GM.CurrentPlayerBody == null)
            {
                return;
            }
            
            // Make fake client follow the real player
            Vector3 targetPosition = GM.CurrentPlayerBody.Head.position - GM.CurrentPlayerBody.Head.forward * followDistance;
            
            // Smooth movement
            fakePlayerObject.transform.position = Vector3.Lerp(
                fakePlayerObject.transform.position,
                targetPosition,
                Time.deltaTime * followSpeed
            );
            
            // Look at player
            Vector3 lookDirection = GM.CurrentPlayerBody.Head.position - fakePlayerObject.transform.position;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                fakePlayerObject.transform.rotation = Quaternion.Slerp(
                    fakePlayerObject.transform.rotation,
                    targetRotation,
                    Time.deltaTime * rotationSpeed
                );
            }
            
            // Update player manager with current transform
            if (fakePlayerManager.head != null)
            {
                fakePlayerManager.head.position = fakePlayerObject.transform.position + Vector3.up * 1.7f;
                fakePlayerManager.head.rotation = fakePlayerObject.transform.rotation;
            }
        }
        
        void OnDestroy()
        {
            DespawnFakeClient();
        }
    }
}
