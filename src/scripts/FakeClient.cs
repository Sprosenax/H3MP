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
                playerPrefab = existingPlayer.gameObject;
                Mod.LogInfo("Using existing player as prefab template");
                break;
            }
        }
    }
    
    // Method 2: Try direct instantiation from GameManager's prefab reference
    if (playerPrefab == null && GameManager.playerPrefab != null)
    {
        playerPrefab = GameManager.playerPrefab;
        Mod.LogInfo("Using GameManager.playerPrefab directly");
    }
    
    // Method 3: Try to get from IM.OD using playerPrefabID
    if (playerPrefab == null && GameManager.playerPrefabID != null)
    {
        if (IM.OD != null && IM.OD.TryGetValue(GameManager.playerPrefabID, out FVRObject fvrObj))
        {
            playerPrefab = fvrObj.GetGameObject();
            Mod.LogInfo("Got player prefab from IM.OD");
        }
    }
    
    // Method 4: Try to load it using the ItemID directly
    if (playerPrefab == null)
    {
        string prefabID = "Default"; // The player prefab ID
        if (IM.OD.TryGetValue(prefabID, out FVRObject obj))
        {
            playerPrefab = obj.GetGameObject();
            Mod.LogInfo("Loaded player prefab from ItemID: " + prefabID);
        }
    }
    
    // Method 5: Search for any prefab with PlayerManager component
    if (playerPrefab == null && IM.OD != null)
    {
        Mod.LogInfo("Searching all " + IM.OD.Count + " items in IM.OD for PlayerManager...");
        foreach (var kvp in IM.OD)
        {
            if (kvp.Value != null)
            {
                GameObject obj = kvp.Value.GetGameObject();
                if (obj != null && obj.GetComponent<PlayerManager>() != null)
                {
                    playerPrefab = obj;
                    Mod.LogInfo("Found player prefab with PlayerManager: " + kvp.Key);
                    break;
                }
            }
        }
    }

    if (playerPrefab == null)
    {
        Mod.LogError("Could not find player prefab! All methods exhausted.");
        Mod.LogError("GameManager.playerPrefabID: " + (GameManager.playerPrefabID ?? "null"));
        Mod.LogError("GameManager.playerPrefab: " + (GameManager.playerPrefab != null ? GameManager.playerPrefab.name : "null"));
        Mod.LogError("GameManager.players.Count: " + GameManager.players.Count);
        Mod.LogError("IM.OD.Count: " + (IM.OD != null ? IM.OD.Count.ToString() : "null"));
        return;
    }

    PlayerManager prefabManager = playerPrefab.GetComponent<PlayerManager>();
    if (prefabManager == null)
    {
        Mod.LogError("Player prefab '" + playerPrefab.name + "' has no PlayerManager component!");
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

    Mod.LogInfo("Fake client spawned successfully with ID: " + fakeID);
}
