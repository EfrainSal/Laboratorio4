#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public static class BuildPiso3
{
    const int W = 120, H = 90;
    static bool[,] floor = new bool[W,H];
    static Tilemap floors, walls, details;
    static readonly Dictionary<string, Tile> tiles = new Dictionary<string, Tile>();
    static Transform props;
    static System.Random random = new System.Random(317);

    [MenuItem("Laboratorio Yiba/Generar Piso 3 modular")]
    public static void Build()
    {
        if (!EditorUtility.DisplayDialog("Generar Piso 3", "Se creara/abrira una escena nueva Piso3_Modular.unity. Guarda tus cambios antes de continuar.", "Continuar", "Cancelar")) return;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        floor = new bool[W,H]; random = new System.Random(317); tiles.Clear();
        // Grandes salas conectadas mediante corredores anchos, con zona central abierta.
        Room(4,32,21,26); Room(12,59,24,20); Room(28,11,26,22);
        Room(30,35,27,21); Room(39,64,26,20); Room(61,28,27,33);
        Room(65,64,26,20); Room(91,58,25,24); Room(91,29,26,26);
        Room(84,6,32,20); Room(53,4,27,20);
        Hall(15,44,74,44,7); Hall(74,44,103,44,7);
        Hall(20,44,20,68,6); Hall(43,44,43,20,7); Hall(73,44,73,72,7);
        Hall(101,44,101,69,7); Hall(100,44,100,17,7);
        Hall(73,44,73,13,7); Hall(43,20,67,20,7);
        Hall(25,69,49,69,7); Hall(56,74,77,74,7);
        Hall(91,69,101,69,7); Hall(78,16,100,16,7);
        Hall(9,45,4,45,6); Hall(104,16,119,16,6);

        var grid = new GameObject("Grid_Piso3", typeof(Grid));
        floors = MakeMap(grid.transform,"01_Suelo", "Ground",-30000,false);
        details = MakeMap(grid.transform,"02_DetallesSuelo", "Ground",-29999,false);
        walls = MakeMap(grid.transform,"03_Paredes_Colision", "Walls",10000,true);
        var baseTile = GetTile("floor",false);
        var worn = GetTile("floor_worn",false);
        var cracked = GetTile("floor_cracked",false);
        var stain = GetTile("floor_stain",false);
        var wall = GetTile("wall",true);
        for(int x=0;x<W;x++) for(int y=0;y<H;y++)
        {
            var pos = new Vector3Int(x,y,0);
            if(floor[x,y])
            {
                int r = random.Next(100);
                floors.SetTile(pos,r<6?stain:r<19?cracked:r<35?worn:baseTile);
            }
            else if (AdjacentFloor(x,y)) walls.SetTile(pos,wall);
        }
        props = new GameObject("04_Objetos_Independientes").transform;
        // Solo objetos independientes: cada uno con su SpriteRenderer y BoxCollider2D.
        Place("tank",17,70); Place("tank",21,70); Place("desk",16,62);
        Place("machine",44,71); Place("panel",56,72); Place("gurney",34,14);
        Place("shelf",16,15); Place("crate",22,16); Place("barrel",24,17);
        Place("tank",68,67); Place("tank",76,67); Place("desk",72,36);
        Place("machine",81,13); Place("panel",92,13); Place("desk",98,72);
        Place("computer",107,71); Place("shelf",109,73); Place("crate",110,65);
        Place("barrel",96,34); Place("gurney",101,35); Place("lamp",33,64,false);
        Place("lamp",53,44,false); Place("lamp",83,44,false); Place("lamp",107,42,false);
        Place("crate",12,37); Place("shelf",11,39); Place("desk",36,43);
        Place("computer",39,44); Place("barrel",65,33);
        // Marcadores de spawn; la IA y el combate se agregan en una fase posterior.
        Marker("Entrada_desde_Piso2",8,44,Color.cyan);
        Marker("Salida_hacia_Piso4",114,16,Color.green);
        Marker("Spawn_Zorbo_A",41,48,Color.yellow);
        Marker("Spawn_Zorbo_B",76,40,Color.yellow);
        Marker("Spawn_Zorbo_C",98,60,Color.yellow);
        var player = new GameObject("Jugador_Prueba");
        player.transform.position = new Vector3(9.5f,44.5f,0);
        var ps = Sprite("test_player");
        var renderer = player.AddComponent<SpriteRenderer>(); renderer.sprite=ps; renderer.sortingLayerName="Default";
        player.AddComponent<Rigidbody2D>().gravityScale=0;
        var pc=player.AddComponent<CircleCollider2D>();pc.radius=0.33f;
        player.AddComponent<Piso3TestPlayer>();
        var camera = new GameObject("Main Camera"); camera.tag="MainCamera";
        var cam=camera.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=9;cam.backgroundColor=new Color(.055f,.075f,.09f);cam.clearFlags=CameraClearFlags.SolidColor;
        camera.AddComponent<AudioListener>();camera.AddComponent<Piso3CameraFollow>().target=player.transform;
        camera.transform.position=player.transform.position+Vector3.back*10;
        var light = new GameObject("Global Light 2D (si usas URP, agregar manualmente)");
        light.transform.SetParent(grid.transform);
        var path="Assets/Scenes/Piso3_Modular.unity";
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),path);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("Piso 3 creado: " + path + ". Pulsa Play para recorrerlo con WASD/flechas.");
    }
    static void Room(int x,int y,int w,int h){for(int i=x;i<x+w && i<W;i++)for(int j=y;j<y+h && j<H;j++)if(i>=0&&j>=0)floor[i,j]=true;}
    static void Hall(int x1,int y1,int x2,int y2,int width)
    {
        int x=x1,y=y1;
        while(true){Room(x-width/2,y-width/2,width,width);if(x==x2&&y==y2)break;if(x!=x2)x+=x<x2?1:-1;else y+=y<y2?1:-1;}
    }
    static bool AdjacentFloor(int x,int y){for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++){int xx=x+dx,yy=y+dy;if(xx>=0&&yy>=0&&xx<W&&yy<H&&floor[xx,yy])return true;}return false;}
    static Tilemap MakeMap(Transform parent,string name,string sorting,int order,bool collider)
    {
        var obj=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));obj.transform.SetParent(parent);
        var tm=obj.GetComponent<Tilemap>();var r=obj.GetComponent<TilemapRenderer>();r.sortingOrder=order;
        if(collider)obj.AddComponent<TilemapCollider2D>();return tm;
    }
    static Sprite Sprite(string name)
    {
        var s=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Piso3/Props/"+name+".png");
        if(s==null)Debug.LogError("Falta sprite: "+name);return s;
    }
    static Tile GetTile(string name,bool solid)
    {
        if(tiles.TryGetValue(name,out var t))return t;
        string p="Assets/Resources/Piso3/Tiles/"+name+".png";
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(p);
        var tile=ScriptableObject.CreateInstance<Tile>();tile.sprite=sprite;tile.colliderType=solid?Tile.ColliderType.Grid:Tile.ColliderType.None;
        string dir="Assets/Resources/Piso3/GeneratedTiles";
        if(!AssetDatabase.IsValidFolder(dir))AssetDatabase.CreateFolder("Assets/Resources/Piso3","GeneratedTiles");
        string assetPath=dir+"/"+name+".asset"; var existing=AssetDatabase.LoadAssetAtPath<Tile>(assetPath); if(existing!=null){Object.DestroyImmediate(tile);tiles[name]=existing;return existing;} AssetDatabase.CreateAsset(tile,assetPath);tiles[name]=tile;return tile;
    }
    static void Place(string name,int x,int y,bool solid=true)
    {
        var s=Sprite(name);if(s==null)return;
        var obj=new GameObject(name+"_"+x+"_"+y);obj.transform.SetParent(props);
        obj.transform.position=new Vector3(x+.5f,y+.5f,0);
        var sr=obj.AddComponent<SpriteRenderer>();sr.sprite=s;sr.sortingOrder=Mathf.RoundToInt(-y*10);
        if(solid){var box=obj.AddComponent<BoxCollider2D>();box.size=new Vector2(s.bounds.size.x*.8f,s.bounds.size.y*.48f);box.offset=new Vector2(0,-s.bounds.size.y*.23f);}
        obj.AddComponent<Piso3SortByY>();
    }
    static void Marker(string name,int x,int y,Color c)
    {
        var o=new GameObject(name);o.transform.position=new Vector3(x+.5f,y+.5f,0);
        var m=o.AddComponent<Piso3Marker>();m.markerColor=c;
    }
}
#endif
