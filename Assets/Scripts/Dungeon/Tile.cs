using UnityEngine;
[System.Serializable]
public class Tile 
{
    public Transform tile;
    public Connector connector;
    public Transform origin;

//Tile is a class that sets the tile, the connector, and the origin of the tile
    public Tile(Transform _tile, Transform _origin){
        tile = _tile;
        origin = _origin;
    }
}
