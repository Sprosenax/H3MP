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

    // Get the player prefab correctly
    GameObject playerPrefab = null;
    
    // Try to get from IM
    if (IM.OD != null && IM.OD.TryGetValue(GameManager.playerPrefabID, out FVRObject fvrObj))
    {
        playerPrefab = fvrObj.GetGameObject();
    }
    
    // If that didn't work, try to find an existing player and clone their prefab reference
    if (playerPrefab == null)
    {
        foreach (PlayerManager existingPlayer in GameManager.players.Values)
        {
            if (existingPlayer != null && existingPlayer.gameObject != null)
            {
                // Get the prefab name from the existing player
                string prefabName = existingPlayer.gameObject.name.Replace("(Clone)", "").Trim();
                if (IM.OD != null)
                {
                    foreach (var kvp in IM.OD)
                    {
                        if (kvp.Value.name == prefabName)
                        {
                            playerPrefab = kvp.Value.GetGameObject();
                            break;
                        }
                    }
                }
                if (playerPrefab != null) break;
            }
        }
    }

    if (playerPrefab == null)
    {
        Mod.LogError("Could not find player prefab!");
        return;
    }

    PlayerManager prefabManager = playerPrefab.GetComponent<PlayerManager>();
    if (prefabManager == null)
    {
        Mod.LogError("Fake client player prefab has no PlayerManager!");
        return;
    }

    // Rest of your spawn code...
    int fakeID = GetNextAvailableID();
    fakePlayerID = fakeID;

    Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward * 2f;
    fakePlayerObject = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
    fakePlayerObject.name = "FakePlayer_" + fakeID;

    fakePlayerManager = fakePlayerObject.GetComponent<PlayerManager>();
    if (fakePlayerManager == null)
    {
        Mod.LogError("Instantiated fake player has no PlayerManager!");
        Destroy(fakePlayerObject);
        fakePlayerObject = null;
        return;
    }

    // Initialize the fake player
    fakePlayerManager.ID = fakeID;
    fakePlayerManager.username = "FakePlayer" + fakeID;
    fakePlayerManager.scene = GameManager.scene;
    fakePlayerManager.instance = GameManager.instance;
    fakePlayerManager.IFF = GM.CurrentPlayerBody.GetPlayerIFF();

    // Add to GameManager
    if (!GameManager.players.ContainsKey(fakeID))
    {
        GameManager.players.Add(fakeID, fakePlayerManager);
    }

    Mod.LogInfo("Fake client spawned with ID: " + fakeID);
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
