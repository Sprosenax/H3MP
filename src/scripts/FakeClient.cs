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
        private int fakePlayerID = -1;
        
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
        
        private int GetNextAvailableID()
        {
            int id = 900; // Start at 900 to avoid conflicts
            while (GameManager.players.ContainsKey(id))
            {
                id++;
            }
            return id;
        }
        
        public void SpawnFakeClient()
        {
            if (fakePlayerObject != null)
            {
                Mod.LogWarning("Fake client already spawned!");
                return;
            }

            // Get the player prefab - try multiple methods
            GameObject playerPrefab = null;
            
            // Method 1: Try to get from existing player in the scene
            if (GameManager.players.Count > 0)
            {
                foreach (PlayerManager existingPlayer in GameManager.players.Values)
                {
                    if (existingPlayer != null && existingPlayer.gameObject != null)
                    {
                        // Clone the existing player's game object as our prefab base
                        playerPrefab = existingPlayer.gameObject;
                        Mod.LogInfo("Using existing player as prefab template");
                        break;
                    }
                }
            }
            
            // Method 2: Try to get from IM.OD using playerPrefabID
            if (playerPrefab == null && GameManager.playerPrefabID != null)
            {
                if (IM.OD != null && IM.OD.TryGetValue(GameManager.playerPrefabID, out FVRObject fvrObj))
                {
                    playerPrefab = fvrObj.GetGameObject();
                    Mod.LogInfo("Got player prefab from IM.OD");
                }
            }
            
            // Method 3: Search for "PlayerBody" or "Player" prefab in IM.OD
            if (playerPrefab == null && IM.OD != null)
            {
                foreach (var kvp in IM.OD)
                {
                    if (kvp.Value != null && 
                        (kvp.Value.name.Contains("Player") || kvp.Value.name.Contains("PlayerBody")))
                    {
                        GameObject obj = kvp.Value.GetGameObject();
                        if (obj != null && obj.GetComponent<PlayerManager>() != null)
                        {
                            playerPrefab = obj;
                            Mod.LogInfo("Found player prefab: " + kvp.Value.name);
                            break;
                        }
                    }
                }
            }

            if (playerPrefab == null)
            {
                Mod.LogError("Could not find player prefab! Methods exhausted.");
                Mod.LogError("GameManager.playerPrefabID: " + (GameManager.playerPrefabID ?? "null"));
                Mod.LogError("GameManager.players.Count: " + GameManager.players.Count);
                return;
            }

            PlayerManager prefabManager = playerPrefab.GetComponent<PlayerManager>();
            if (prefabManager == null)
            {
                Mod.LogError("Player prefab has no PlayerManager component!");
                return;
            }

            // Get next available ID
            int fakeID = GetNextAvailableID();
            fakePlayerID = fakeID;

            // Spawn the fake player
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
            else
            {
                GameManager.players[fakeID] = fakePlayerManager;
            }

            Mod.LogInfo("Fake client spawned with ID: " + fakeID);
        }
        
        public void DespawnFakeClient()
        {
            if (fakePlayerObject != null)
            {
                if (fakePlayerID != -1 && GameManager.players.ContainsKey(fakePlayerID))
                {
                    GameManager.players.Remove(fakePlayerID);
                }
                
                Destroy(fakePlayerObject);
                fakePlayerObject = null;
                fakePlayerManager = null;
                fakePlayerID = -1;
                
                Mod.LogInfo("Fake client despawned");
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
