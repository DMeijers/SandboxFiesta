using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class DungeonGenerator : MonoBehaviour
{
    public GameObject[] tilePrefabs;
    public GameObject[] startPrefabs;
    public GameObject[] endPrefabs;
    public GameObject[] blockedPrefabs;
    public GameObject[] doorPrefabs;

    [Header("Debugging Options")]
    public bool useBoxColliders;
    public bool useLightsForDebugging;
    public bool restoreLightsAfterDebugging;

    [Header("Controls")]
    public KeyCode toggleMapKey = KeyCode.M;
    public KeyCode reloadKey = KeyCode.Backspace;

    [Header("Map Limits")]
    [Range(2, 100)] public int mainLength = 10;
    [Range(0, 50)] public int branchLength = 5;
    [Range(0, 25)] public int numBranches = 10;
    [Range(0, 100)] public int doorPercent = 25;
    [Range(0, 1f)] public float constructionDelay;

    [Header("Available at Runtime")]
    public List<Tile> generatedTiles = new List<Tile>();

    GameObject goCamera, goPlayer;
    List<Connector> availableConnectors = new List<Connector>();
    Color startLightColor = Color.white;
    Transform tileFrom, tileTo, tileRoot;
    Transform container;
    int attempts;
    int maxAttempts = 50;
    void Start()
    {
        goCamera = GameObject.Find("OverheadCamera");
        goPlayer = GameObject.FindWithTag("Player");
        StartCoroutine(DungeonBuild());

        // tileFrom = CreateStartTile();
        // tileTo = CreateTile();
        // ConnectTiles();
        // for(int i = 0; i < 10; i++){
        //     tileFrom = tileTo;
        //     tileTo = CreateTile();
        //     ConnectTiles();
        // }
    }

    void Update()
    {
        if (Input.GetKeyDown(reloadKey))
        {
            SceneManager.LoadScene("DungeonGen");
        }

        if (Input.GetKeyDown(toggleMapKey))
        {
            goCamera.SetActive(!goCamera.activeInHierarchy);
            goPlayer.SetActive(!goPlayer.activeInHierarchy);
        }
    }


//DungeonBuild is the main function that builds the dungeon.
    IEnumerator DungeonBuild() {
        goCamera.SetActive(true);
        goPlayer.SetActive(false);
        GameObject goContainer = new GameObject("Main Path");
        container = goContainer.transform;
        container.SetParent(transform);
        tileRoot = CreateStartTile();
        DebugRoomLighting(tileRoot, Color.cyan);
        tileTo =  tileRoot;
        for(int i = 0; i < mainLength - 1; i++){
            yield return new WaitForSeconds(constructionDelay);
            tileFrom = tileTo;
            tileTo = CreateTile();
            DebugRoomLighting(tileTo, Color.yellow);
            ConnectTiles();
            CollisionCheck();
            if(attempts >= maxAttempts) {break;}
        }
        // get all open connectors that are not connected 
        foreach(Connector connector in container.GetComponentsInChildren<Connector>()) {
            if(!connector.IsConnected) {
                if(!availableConnectors.Contains(connector)) {
                    availableConnectors.Add(connector);
                }
            }
        }
        // create branches
        for(int b = 0; b < numBranches; b++) {
            if(availableConnectors.Count > 0) {
                goContainer = new GameObject("Branch " + (b + 1));
                container = goContainer.transform;
                container.SetParent(transform);
                int availIndex = Random.Range(0, availableConnectors.Count);
                tileRoot = availableConnectors[availIndex].transform.parent.parent;
                availableConnectors.RemoveAt(availIndex);
                tileTo = tileRoot;
                for(int i = 0; i < branchLength - 1; i++) {
                    yield return new WaitForSeconds(constructionDelay);
                    tileFrom = tileTo;
                    tileTo = CreateTile();
                    DebugRoomLighting(tileTo, Color.green);
                    ConnectTiles();
                    CollisionCheck();
                    if(attempts >= maxAttempts) { break; }
                }
            }
            else { break; }
        }
        LightRestoration();
        CleanupBoxes();
        BlockedPassages();
        yield return null;
        goCamera.SetActive(false);
        goPlayer.SetActive(true);
        
    }

    void BlockedPassages() {
        foreach(Connector connector in transform.GetComponentsInChildren<Connector>()) {
            if(!connector.IsConnected) {
                Vector3 pos = connector.transform.position;
                int wallIndex = Random.Range(0, blockedPrefabs.Length);
                GameObject goWall = Instantiate(blockedPrefabs[wallIndex], pos, connector.transform.rotation, connector.transform) as GameObject;
                goWall.name = blockedPrefabs[wallIndex].name;
            }
        }
    }


// The CollisionCheck method checks for collisions between the current tile and other tiles in the dungeon.

    void CollisionCheck() {
        BoxCollider box = tileTo.GetComponent<BoxCollider>();
        if(box == null) {
            box = tileTo.gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
        }
        Vector3 offset = (tileTo.right * box.center.x) + (tileTo.up * box.center.y) + (tileTo.forward * box.center.z);
        Vector3 halfExtents = box.bounds.extents;
        List<Collider> hits = Physics.OverlapBox(tileTo.position + offset, halfExtents, Quaternion.identity, LayerMask.GetMask("Tile")).ToList();
        if(hits.Count > 0) {
            if(hits.Exists(x => x.transform != tileFrom && x.transform != tileTo)) {
                // hit something (other than tileFrom or tileTo)
                attempts++;
                int toIndex = generatedTiles.FindIndex(x => x.tile == tileTo);
                if(generatedTiles[toIndex].connector != null) {
                    generatedTiles[toIndex].connector.IsConnected = false;
                }
                generatedTiles.RemoveAt(toIndex);
                DestroyImmediate(tileTo.gameObject);
                // backtracking
                if(attempts >= maxAttempts) {
                    int fromIndex = generatedTiles.FindIndex(x => x.tile == tileFrom);
                    Tile myTileFrom = generatedTiles[fromIndex];
                    if(tileFrom != tileRoot) {
                        if(myTileFrom.connector != null) {
                            myTileFrom.connector.IsConnected = false;
                        }
                        availableConnectors.RemoveAll(x => x.transform.parent.parent == tileFrom);
                        generatedTiles.RemoveAt(fromIndex);
                        DestroyImmediate(tileFrom.gameObject);
                        if(myTileFrom.origin != tileRoot) {
                            tileFrom = myTileFrom.origin;
                        }
                        else if(container.name.Contains("Main")) {
                            if(myTileFrom.origin !=  null) {
                                tileRoot = myTileFrom.origin;
                                tileFrom = tileRoot;
                            }
                        }
                        else if(availableConnectors.Count > 0) {
                            int availIndex = Random.Range(0, availableConnectors.Count);
                            tileRoot = availableConnectors[availIndex].transform.parent.parent;
                            availableConnectors.RemoveAt(availIndex);
                            tileFrom = tileRoot;
                        }
                        else { return; }

                    }
                    else if(container.name.Contains("Main")) {
                        if(myTileFrom.origin != null) {
                            tileRoot = myTileFrom.origin;
                            tileFrom = tileRoot;
                        }
                    }
                    else if(availableConnectors.Count > 0) {
                        int availIndex = Random.Range(0, availableConnectors.Count);
                        tileRoot = availableConnectors[availIndex].transform.parent.parent;
                        availableConnectors.RemoveAt(availIndex);
                        tileFrom = tileRoot;
                    }
                    else { return; }
                }
                // retry
                if(tileFrom !=  null) {
                    if(generatedTiles.Count == mainLength - 1) {
                        DebugRoomLighting(tileTo, Color.magenta);
                    }
                    else {
                        tileTo = CreateTile();
                        Color retryColor = container.name.Contains("Branch") ? Color.green : Color.yellow;
                        DebugRoomLighting(tileTo, retryColor * 2f);
                    }
                    ConnectTiles();
                    CollisionCheck();
                }
            }
            else { attempts = 0; } // nothing other than tileTo and tileFrom was hit (so restore attempts back to zero)
        }
    }



    void LightRestoration() {
        if(useLightsForDebugging && restoreLightsAfterDebugging && Application.isEditor) {
            Light[] lights = transform.GetComponentsInChildren<Light>();
            foreach(Light light in lights) {
                light.color = startLightColor;
            }
        }
    }

    void CleanupBoxes() {
        if(!useBoxColliders) {
            foreach(Tile myTile in generatedTiles) {
                BoxCollider box = myTile.tile.GetComponent<BoxCollider>();
                if(box != null) { Destroy(box); }
            }
        }
    }

    void DebugRoomLighting(Transform tile, Color lightColor) {
        if(useLightsForDebugging && Application.isEditor) {
            Light[] lights = tile.GetComponentsInChildren<Light>();
            if(lights.Length > 0) {
                if(startLightColor == Color.white) {
                    startLightColor = lights[0].color;
                }
                foreach(Light light in lights) {
                    light.color = lightColor;
                }
            }
        }
    }

//ConnectTiles connects the two tiles.
    void ConnectTiles() {
        Transform connectFrom = GetRandomConnector(tileFrom);
        if(connectFrom == null) { return; }
        Transform connectTo = GetRandomConnector(tileTo);
        if(connectTo == null) { return; }
        connectTo.SetParent(connectFrom);
        tileTo.SetParent(connectTo);
        connectTo.localPosition = Vector3.zero;
        connectTo.localRotation = Quaternion.identity;
        connectTo.Rotate(0, 180f, 0);
        tileTo.SetParent(container);
        connectTo.SetParent(tileTo.Find("Connectors"));
        generatedTiles.Last().connector = connectFrom.GetComponent<Connector>();
    }


//GetRandomConnector returns a random connector from the tile passed in as a parameter.
    Transform GetRandomConnector(Transform tile){
        if(tile == null) { return null; }
        List<Connector> connectorList = tile.GetComponentsInChildren<Connector>().ToList().FindAll(x => x.IsConnected == false);
        if(connectorList.Count > 0){
            int connectorIndex = Random.Range(0, connectorList.Count);
            connectorList[connectorIndex].IsConnected = true;
            if(tile == tileFrom) {
                BoxCollider box = tile.GetComponent<BoxCollider>();
                if(box != null){
                 box = tile.gameObject.AddComponent<BoxCollider>();
                 box.isTrigger = true;
                }
            }
            return connectorList[connectorIndex].transform;
        }
        return null;
    }


    Transform CreateTile() {
        int index = Random.Range(0, tilePrefabs.Length);
        GameObject goTile = Instantiate(tilePrefabs[index], Vector3.zero, Quaternion.identity, container) as GameObject;
        goTile.name = tilePrefabs[index].name;
        Transform origin = generatedTiles[generatedTiles.FindIndex(x => x.tile == tileFrom)].tile;
        generatedTiles.Add(new Tile(goTile.transform, origin));
        return goTile.transform;
    }

    Transform CreateStartTile() {
        int index = Random.Range(0, startPrefabs.Length);
        GameObject goTile = Instantiate(startPrefabs[index], Vector3.zero, Quaternion.identity, container) as GameObject;
        goTile.name = "Start Room";
        float yRot = Random.Range(0, 4) * 90f;
        goTile.transform.Rotate(0, yRot, 0);
        goPlayer.transform.LookAt(goTile.GetComponentInChildren<Connector>().transform);
        generatedTiles.Add(new Tile(goTile.transform, null));
        return goTile.transform;
    }
}
