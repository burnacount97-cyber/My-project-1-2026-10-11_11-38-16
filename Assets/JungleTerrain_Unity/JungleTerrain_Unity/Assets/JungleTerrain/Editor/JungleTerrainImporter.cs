using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;

public class JungleTerrainImporter : EditorWindow
{
    int terrainSize = 500;
    int terrainHeight = 90;

    [MenuItem("Tools/Jungle Terrain/Crear Terreno")]
    static void Open() => GetWindow<JungleTerrainImporter>("Jungle Terrain");

    void OnGUI()
    {
        GUILayout.Label("Jungle Terrain - Solo terreno", EditorStyles.boldLabel);
        terrainSize = EditorGUILayout.IntSlider("Tamaño (m)", terrainSize, 100, 1000);
        terrainHeight = EditorGUILayout.IntSlider("Altura (m)", terrainHeight, 30, 180);

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "Genera únicamente el Terrain. No crea árboles, agua, personajes ni edificios.",
            MessageType.Info);

        if (GUILayout.Button("CREAR TERRENO 3D", GUILayout.Height(42)))
            CreateTerrain();
    }

    // Busca un asset por nombre de archivo en TODO el proyecto.
    static string FindAssetPath(string fileName)
    {
        string[] guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(fileName));
        foreach (var g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (string.Equals(Path.GetFileName(p), fileName,
                              System.StringComparison.OrdinalIgnoreCase))
                return p;
        }
        return null;
    }

    // Devuelve una copia legible de la textura (funciona aunque "Read/Write" esté apagado).
    static Texture2D MakeReadable(Texture2D src)
    {
        if (src == null) return null;
        RenderTexture rt = RenderTexture.GetTemporary(
            src.width, src.height, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Graphics.Blit(src, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D result = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
        result.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    void CreateTerrain()
    {
        string heightPath = FindAssetPath("JungleHeightmap16.png");
        string texPath    = FindAssetPath("JungleGround.png");

        if (heightPath == null || texPath == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontraron las texturas.\n\n" +
                "Buscadas:\n  - JungleHeightmap16.png\n  - JungleGround.png\n\n" +
                "Asegúrate de que estén dentro de Assets (en cualquier subcarpeta).",
                "OK");
            return;
        }

        Texture2D heightSrc = AssetDatabase.LoadAssetAtPath<Texture2D>(heightPath);
        Texture2D groundTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

        if (heightSrc == null || groundTex == null)
        {
            EditorUtility.DisplayDialog("Error", "No se pudieron cargar las texturas.", "OK");
            return;
        }

        // Carpeta de salida = misma carpeta que el heightmap.
        string outFolder = Path.GetDirectoryName(heightPath).Replace("\\", "/");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Texture2D heightTex = MakeReadable(heightSrc);

        TerrainData td = new TerrainData();
        td.heightmapResolution = heightTex.width;
        td.size = new Vector3(terrainSize, terrainHeight, terrainSize);

        float[,] heights = new float[heightTex.height, heightTex.width];
        Color[] pixels = heightTex.GetPixels();

        for (int z = 0; z < heightTex.height; z++)
            for (int x = 0; x < heightTex.width; x++)
                heights[z, x] = pixels[z * heightTex.width + x].grayscale;

        td.SetHeights(0, 0, heights);

        string dataPath = $"{outFolder}/JungleTerrainData.asset";
        AssetDatabase.CreateAsset(td, dataPath);

        Terrain terrain = Terrain.CreateTerrainGameObject(td).GetComponent<Terrain>();
        terrain.name = "Jungle_Terrain_ONLY";
        terrain.heightmapPixelError = 8;
        terrain.basemapDistance = 800;
        terrain.drawInstanced = true;

        TerrainLayer layer = new TerrainLayer();
        layer.diffuseTexture = groundTex;
        layer.tileSize = new Vector2(terrainSize * 0.65f, terrainSize * 0.65f);
        layer.tileOffset = Vector2.zero;

        string layerPath = $"{outFolder}/JungleGround.terrainlayer";
        AssetDatabase.CreateAsset(layer, layerPath);
        td.terrainLayers = new TerrainLayer[] { layer };

        GameObject sun = new GameObject("Sun");
        Light l = sun.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.05f;
        l.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientIntensity = 0.65f;

        string scenePath = $"{outFolder}/JungleTerrain.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = terrain.gameObject;
        EditorUtility.DisplayDialog("Listo",
            "Terreno creado.\n\n" + scenePath, "OK");
    }
}