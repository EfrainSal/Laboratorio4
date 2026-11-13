#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public static class Piso3FinalBuilder
{
    const string Base = "Assets/Resources/Piso3/";
    static readonly System.Random Rng = new System.Random(17);
    static readonly HashSet<Vector2Int> Cells = new HashSet<Vector2Int>();
    static readonly HashSet<Vector2Int> Walls = new HashSet<Vector2Int>();
    static readonly List<RectInt> Rooms = new List<RectInt>();
    static GameObject objectsRoot;
    static Grid grid;

    [MenuItem("Laboratorio Yiba/Generar Piso 3 FINAL (modular)")]
    public static void Generate()
    {
        if (!EditorUtility.DisplayDialog("Generar Piso 3 modular", "Se creará una escena NUEVA en Assets/Scenes/Piso3_Final_Modular.unity. No se sobrescribirá tu escena Piso3_Modular anterior. ¿Continuar?", "Generar", "Cancelar")) return;
        ConfigureImports();
        Cells.Clear(); Walls.Clear(); Rooms.Clear();
        // Plano aproximado de la referencia aprobada: 120 x 90 tiles, 16 espacios conectados.
        Room(9,48,30,22); Room(9,21,27,20); Room(25,70,22,12);
        Room(42,61,31,22); Room(41,36,27,23); Room(32,9,33,24);
        Room(67,57,28,26); Room(72,35,35,24); Room(74,10,31,23);
        Room(101,46,17,19); Room(101,19,18,22); Room(53,80,17,8);
        Corridor(13,44,111,44,8); Corridor(38,23,38,77,7);
        Corridor(67,17,67,81,8); Corridor(98,23,98,72,8);
        Corridor(17,61,111,61,6); Corridor(20,31,110,31,6);
        Corridor(14,60,14,76,6); Corridor(112,61,112,80,6);
        Room(4,55,9,14); Room(111,73,8,14);
        // Wall ring only at boundaries; doors/corridors are open where cells connect.
        foreach (var p in Cells)
            foreach (var n in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})
                if (!Cells.Contains(p+n)) Walls.Add(p+n);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var gridGo = new GameObject("Grid_Piso3"); grid = gridGo.AddComponent<Grid>(); grid.cellSize = Vector3.one;
        var floor = Map("01_Suelo", 0, false); var marks = Map("02_Detalles_NoInteractivos", 1, false);
        var wall = Map("03_Paredes_Colision", 5, true);
        var floorTiles = new[]{Tile("floor"),Tile("floor_dirty"),Tile("floor_crack"),Tile("floor_chemical"),Tile("floor_blood")};
        var detailTiles = new[]{Tile("grate"),Tile("threshold")};
        var wallTile = Tile("wall");
        foreach (var p in Cells)
        {
            int r=Rng.Next(100);
            floor.SetTile(new Vector3Int(p.x,p.y,0), floorTiles[r<84?0:r<91?1:r<96?2:r<99?3:4]);
            if (Rng.Next(140)==0) marks.SetTile(new Vector3Int(p.x,p.y,0),detailTiles[0]);
        }
        foreach(var p in Walls) wall.SetTile(new Vector3Int(p.x,p.y,0),wallTile);
        objectsRoot = new GameObject("04_Objetos_Independientes");
        // Positions based on room purposes; 16 types of reusable individual sprites.
        Place("tanque_contencion",30,73, true); Place("tanque_contencion",35,73,true);
        Place("mesa_trabajo",24,64,true); Place("panel_pared",19,68,true);
        Place("computadora",14,56,true); Place("estanteria",20,53,true);
        Place("escritorio",16,26,true); Place("sillon",22,24,true);
        Place("caja",12,28,true); Place("caja",13,28,true);
        Place("camilla",54,68,true); Place("maquina_experimental",63,76,true);
        Place("mesa_trabajo",58,53,true); Place("tanque_contencion",48,47,true);
        Place("tanque_contencion",59,47,true); Place("maquina_experimental",48,22,true);
        Place("tanque_contencion",53,18,true); Place("tanque_contencion",60,18,true);
        Place("panel_pared",76,72,true); Place("escritorio",86,71,true);
        Place("computadora",90,67,true); Place("estanteria",80,63,true);
        Place("mesa_trabajo",85,51,true); Place("maquina_experimental",92,54,true);
        Place("camilla",84,27,true); Place("escritorio",94,27,true);
        Place("estanteria",103,29,true); Place("barril",105,36,true);
        Place("caja",110,27,true); Place("caja",110,28,true);
        Place("planta",25,34,false); Place("planta",105,56,false);
        Place("tuberia",35,66,true); Place("tuberia",75,21,true);
        Place("panel_pared",50,35,true); Place("estanteria",42,15,true);
        Place("lampara",21,69,false); Place("lampara",53,58,false);
        Place("lampara",79,81,false); Place("lampara",102,62,false);
        Place("ventana",68,82,false); Place("ventana",101,76,false);
        Place("barril",33,16,true); Place("barril",89,13,true);
        Place("sillon",107,52,true); Place("puerta",112,78,false);
        Place("puerta",7,60,false);
        var spawn = Marker("Entrada_desde_Piso2",9,60,Color.cyan);
        Marker("Salida_hacia_Piso4",115,81,Color.yellow);
        Marker("Spawn_Zorbo_A",25,40,Color.red);
        Marker("Spawn_Zorbo_B",58,64,Color.red);
        Marker("Spawn_Zorbo_C",89,43,Color.red);
        Marker("Spawn_Zorbo_D",102,26,Color.red);
        var player = new GameObject("Jugador_Prueba"); player.transform.position = spawn.transform.position;
        var pr = player.AddComponent<SpriteRenderer>(); pr.sprite = LoadSprite("Props/barril"); pr.color = new Color(0.4f,0.9f,1f);
        pr.sortingOrder = 15;
        var rb=player.AddComponent<Rigidbody2D>();rb.gravityScale=0;rb.freezeRotation=true;
        var pc=player.AddComponent<CircleCollider2D>();pc.radius=.3f;
        player.AddComponent<Piso3TestMovement>();
        var camGo=new GameObject("Main Camera");camGo.tag="MainCamera";
        var cam=camGo.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=8;cam.backgroundColor=new Color(.025f,.06f,.08f);cam.clearFlags=CameraClearFlags.SolidColor;
        camGo.AddComponent<AudioListener>();camGo.AddComponent<Piso3CameraFollow>().target=player.transform;
        camGo.transform.position=player.transform.position+new Vector3(0,0,-10);
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/Piso3_Final_Modular.unity");
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Piso 3 generado", "Escena guardada en Assets/Scenes/Piso3_Final_Modular.unity. Abre Game y presiona Play. WASD/flechas para caminar. Los objetos son GameObjects independientes.","OK");
    }
    static void Room(int x,int y,int w,int h){Rooms.Add(new RectInt(x,y,w,h));for(int i=x;i<x+w;i++)for(int j=y;j<y+h;j++)Cells.Add(new Vector2Int(i,j));}
    static void Corridor(int x1,int y1,int x2,int y2,int width){int r=width/2;for(int x=Math.Min(x1,x2)-r;x<=Math.Max(x1,x2)+r;x++)for(int y=Math.Min(y1,y2)-r;y<=Math.Max(y1,y2)+r;y++)Cells.Add(new Vector2Int(x,y));}
    static Tile Tile(string name)
    {
        string path="Assets/Resources/Piso3/GeneratedTiles/"+name+".asset";
        var t=AssetDatabase.LoadAssetAtPath<Tile>(path);if(t!=null)return t;
        Directory.CreateDirectory("Assets/Resources/Piso3/GeneratedTiles");
        t=ScriptableObject.CreateInstance<Tile>();t.sprite=LoadSprite("Tiles/"+name);t.colliderType=UnityEngine.Tilemaps.Tile.ColliderType.None;
        AssetDatabase.CreateAsset(t,path);return t;
    }
    static Sprite LoadSprite(string sub){return AssetDatabase.LoadAssetAtPath<Sprite>(Base+sub+".png");}
    static Tilemap Map(string name,int order,bool collision)
    {
        var go=new GameObject(name);go.transform.SetParent(grid.transform);
        var tm=go.AddComponent<Tilemap>();var renderer=go.AddComponent<TilemapRenderer>();renderer.sortingOrder=order;
        if(collision)
        {
            var rb=go.AddComponent<Rigidbody2D>();rb.bodyType=RigidbodyType2D.Static;
            var collider=go.AddComponent<TilemapCollider2D>();
            // Tile collider shape is taken from the generated Tile asset.
            var wall=Tile("wall");wall.colliderType=UnityEngine.Tilemaps.Tile.ColliderType.Grid;EditorUtility.SetDirty(wall);
        }
        return tm;
    }
    static void Place(string type,int x,int y,bool solid)
    {
        if(!Cells.Contains(new Vector2Int(x,y)))return;
        var go=new GameObject(type+"_"+x+"_"+y);go.transform.SetParent(objectsRoot.transform);
        go.transform.position=new Vector3(x+.5f,y+.5f,0);
        var sprite=LoadSprite("Props/"+type);var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;
        sr.sortingOrder=10;
        if(solid){var box=go.AddComponent<BoxCollider2D>();box.size=new Vector2(Mathf.Max(.5f,sprite.rect.width/32f-.18f),Mathf.Max(.45f,sprite.rect.height/32f-.18f));}
        go.AddComponent<Piso3YSort>();
    }
    static GameObject Marker(string name,int x,int y,Color color)
    {
        var go=new GameObject(name);go.transform.position=new Vector3(x+.5f,y+.5f,0);
        var giz=go.AddComponent<Piso3Marker>();giz.markerColor=color;return go;
    }
    static void ConfigureImports()
    {
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Base}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);
            var imp=AssetImporter.GetAtPath(path) as TextureImporter;if(imp==null)continue;
            bool change=imp.textureType!=TextureImporterType.Sprite||imp.spritePixelsPerUnit!=32||imp.filterMode!=FilterMode.Point||imp.textureCompression!=TextureImporterCompression.Uncompressed;
            if(change){imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.spritePixelsPerUnit=32;imp.filterMode=FilterMode.Point;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.mipmapEnabled=false;imp.SaveAndReimport();}
        }
    }
}
#endif
