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
