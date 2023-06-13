using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

[CreateAssetMenu(fileName = "TexturesSO", menuName = "ScriptableObjects/Texture Extractor SO", order = 1)]
/* TexturesSO helps saving and extracting the limb assets used
 * by a specific texture. Serializes the data in files for
 * later reference by the skeleton. */
public class TexturesSO : ScriptableObject {
    [HideInInspector] [SerializeField] private Texture2D texture;
    [SerializeField] private Texture2D textureToSearch;
    [SerializeField] private string subGroupRoute = "";
    [HideInInspector] [SerializeField] private List<Limb> limbs = new List<Limb>();
    [SerializeField] HashSet<string> hashSet;
    [HideInInspector] [SerializeField] private string path = "";

    //Making data save consistently;
    private void OnEnable() {
        EditorUtility.SetDirty(this);
    }

    private void OnValidate() {
        AssetDatabase.SaveAssets();
    }

    public List<Limb> GetLimbs(){ return limbs; }
    public void ClearLimbs(){ limbs.Clear(); }
    public void AddLimb(Limb limb){ limbs.Add(limb); }
    public Limb CallLimb(string name){ return limbs.Find(limbSearcher => limbSearcher.GetName() == name); }
    public void SetLimbs(List<Limb> Limbs){ limbs = Limbs; }
    public Texture2D GetTexture() { return texture; }
    public void SetTexture(Texture2D Texture) { 
        texture = Texture;
    }
    public Texture2D GetTextureToSearch() { return textureToSearch; }
    public void SetTextureToSearch() { texture = textureToSearch; }
    public string GetSubGroupRoute(){ return subGroupRoute; }
    public void SetSubGroupRoute(string SubGroupRoute){ subGroupRoute = SubGroupRoute; }
    public string GetPath(){ return path; }
    public void SetPath(){ 
        path = AssetDatabase.GetAssetPath(textureToSearch); 
    }


    //***********************************

    //System Management;
    public Texture2D LoadTextureData(string route, TexturesSO textures, bool saveAsPng) {
        //Sprite has to be located in the "Resources" folder;
        Sprite[] sprites = Resources.LoadAll<Sprite>(FormatRoute(route));
        string path = CreateFolder(textures.name, textures.GetSubGroupRoute());

        
        //Extracting limb data;
        foreach(Sprite sprite in sprites) {
            //Formatting the limb;
            Limb limb = new Limb();
            limb.SetName(sprite.name);

            Rect rect = sprite.rect;
            int x = (int) rect.x, y = (int) rect.y,
                w = (int) rect.width, h = (int) rect.height;

            limb.SetCoordinates(new Vector4(x, y, w, h));
            /* 0: X; Where the sprite box starts;
             * 1: Y; Where the sprite box ends;
             * 2: Width; Size of the texture;
             * 3: Height; Size of the texture; */

            //Sprite center;
            limb.SetPivot(new Vector2(sprite.pivot.x / w, sprite.pivot.y / h));
            //Generating the textures & mapping it to a transparent color;
            Texture2D texture = Utils.CreateTransparent2DTexture(w, h);

            //Mapping the limb to the texture;
            Color[] pixels = sprite.texture.GetPixels(x, y, w, h);
            texture.SetPixels(pixels);
            
            texture.Apply();

            // Saving the texture as an asset for later reference;
            string texturePath = path + "/" + sprite.name; // Specify the desired path and filename
            string assetPath = texturePath + ".asset";
            
            if(saveAsPng) SaveTexture(texturePath, (int) Prefs.padding, texture, sprite, saveAsPng);
            AssetDatabase.CreateAsset(texture, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            //Saving the data;
            limb.SetTexture(AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath));
            textures.AddLimb(limb);
        }

        return Resources.Load<Texture2D>(route);
    }

    private void SaveTexture(string route, int padding, Texture2D texture, Sprite sprite, bool saveAsPng) {
        route += ".png";
        Texture2D png = Utils.CreateTransparent2DTexture(texture.width - padding, texture.height - padding);
        int paddingMid = padding / 2;

        //Mapping the limb to the texture;
        Rect rect = sprite.textureRect;
        Color[] pixels = sprite.texture.GetPixels((int) rect.x + paddingMid, (int) rect.y + paddingMid, 
                                                  (int) rect.width - padding, (int) rect.height - padding);
        png.SetPixels(pixels);
        png.Apply();

        byte[] bytes = png.EncodeToPNG();
        File.Create(route).Dispose();
        File.WriteAllBytes(route, bytes);
    }

    //Create a readable route by the asset database;
    public string FormatRoute(string route) {
        //Removing the first part of the route;
        route = route.Replace("Assets/Resources/", "");

        //Removing the .extension part of the route;
        route = route.Substring(0, route.IndexOf("."));

        return route;
    }

    //Create a folder to hold assets;
    public string CreateFolder(string textureName, string groupRoute){
        string baseRoute = "Assets/Resources/Skeletons";
        string[] groupSplit;
        string folderPath = "";

        // If the group doesn't have a route
        if (groupRoute.Equals("")){
            groupSplit = textureName.Split("/");
        }
        else{
            groupSplit = (groupRoute + "/" + textureName).Split("/");
        }

        foreach (string folderName in groupSplit){
            folderPath += "/" + folderName;

            // If the folder has a valid path and name, create it
            string relativeFolderPath = baseRoute + folderPath;
            if (!AssetDatabase.IsValidFolder(relativeFolderPath)){
                AssetDatabase.CreateFolder(baseRoute + folderPath.Substring(0, folderPath.LastIndexOf('/')), folderName);
                Debug.Log("Folder Created: " + folderName);
            }
        }

        return baseRoute + folderPath;
    }
}


////////////////////////////////////////////////////////////////////


/* The Limb class is mandatory for all dependencies of the 
 * texture swapping system, as they store all the pertinent
 * information to iterate over multiple limbs if needed. */
[System.Serializable]
public class Limb {
    [SerializeField] private Texture2D texture;
    [SerializeField] private string name;
    [SerializeField] private Vector4 coordinates;
    [SerializeField] private Vector2 pivot;

    public Texture2D GetTexture() { return texture; }
    public void SetTexture(Texture2D _Texture) { texture = _Texture; }
    public string GetName() { return name; }
    public void SetName(string Name) { name = Name; }
    /* x: X; Where the sprite box starts;
     * y: Y; Where the sprite box ends;
     * z: Width; Size of the texture;
     * w: Height; Size of the texture; */
    public Vector4 GetCoordinates() { return coordinates; }
    public void SetCoordinates(Vector4 Coordinates) { coordinates = Coordinates; }
    public int GetX() { return (int) coordinates.x; }
    public void SetX(int X) { coordinates.x = X; }
    public int GetY() { return (int) coordinates.y; }
    public void SetY(int Y) { coordinates.y = Y; }
    public int GetWidth() { return (int) coordinates.z; }
    public void SetWidth(int Width) { coordinates.z = Width; }
    public int GetHeight() { return (int) coordinates.w; }
    public void SetHeight(int Height) { coordinates.w = Height; }
    public Vector2 GetPivot() { return pivot; }
    public void SetPivot(Vector2 Pivot) { pivot = Pivot; }
}


////////////////////////////////////////////////////////////////////

//Custom Editor;
[CustomEditor(typeof(TexturesSO))] // Replace with the name of your ScriptableObject class
public class TexturesSOEditor : Editor{
    public bool saveTextureAsPng = false;

    public override void OnInspectorGUI(){
        TexturesSO textures = (TexturesSO) target;
        base.OnInspectorGUI();
        string subGroupName = textures.GetSubGroupRoute();
        textures.SetPath();
        textures.SetTextureToSearch();

        //Naming a subgroup of folders if needed;
        if (!subGroupName.Equals("") && textures.GetLimbs().Count == 0) {
            EditorGUILayout.HelpBox("Specify this field ONLY IF the texture belongs to a subgroup of a main group.", MessageType.Info);
        }

        //Removing the "/" character as it can generate conflicts between routes;
        if (subGroupName.EndsWith("/")){
            textures.SetSubGroupRoute(subGroupName.Substring(0, subGroupName.Length - 1));
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Save Texture Assets + PNGs?", GUILayout.Width(250));
        saveTextureAsPng = EditorGUILayout.Toggle(saveTextureAsPng);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        // Display a label for a property
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Skeleton Scriptable Object Data:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(textures.GetLimbs().Count + " Limbs found");
        EditorGUILayout.EndHorizontal();
        if(textures.GetLimbs().Count == 0)
            EditorGUILayout.HelpBox("� Ensure the texture is located anywhere within the 'Resources' folder in the project's root.\n� The texture name cannot contain '.' characters.", MessageType.Warning);
        EditorGUILayout.Space();

        //Confirm search and load button;
        if (GUILayout.Button("Load Limb Data From Texture")){
            textures.ClearLimbs();
            textures.LoadTextureData(textures.GetPath(), textures, saveTextureAsPng);
        }

        //Clearing padding artifacts;
        if (GUILayout.Button("Clean Texture Padding Artifacts")){
            Texture2D source = ConvertDXT5ToRGBA32(textures.GetTexture());
            
            foreach(Limb limb in textures.GetLimbs()) {
                int padding = (int) Prefs.padding / 2;
                int x = limb.GetX(), y = limb.GetY(), w = limb.GetWidth(), h = limb.GetHeight();

                Utils.ClearTextureAt(new Vector4(x, y + h - padding, w, padding), source);
                Utils.ClearTextureAt(new Vector4(x + w - padding, y, padding, h), source);
                Utils.ClearTextureAt(new Vector4(x, y, w, padding), source);
                Utils.ClearTextureAt(new Vector4(x, y, padding, h), source);
            }

            Utils.SaveTexture(source, textures.GetPath());
            textures.SetTexture(source);
        }
    
        EditorGUILayout.Space();

        try {
            foreach(Limb limb in textures.GetLimbs()) {
                LoadLimbInGUI(limb, 2, true);
            }
        } catch { }
    }

    public Texture2D ConvertDXT5ToRGBA32(Texture2D dxt5Texture){
        Texture2D rgba32Texture = new Texture2D(dxt5Texture.width, dxt5Texture.height, TextureFormat.RGBA32, false);
        rgba32Texture.SetPixels32(dxt5Texture.GetPixels32());
        rgba32Texture.Apply();
        return rgba32Texture;
    }

    //Loads all the needed data in the GUI;
    public static void LoadLimbInGUI(Limb limb, float rectSize, bool showMetadata) {
        int boxHeight = 20;
        string name = limb.GetName();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(name, EditorStyles.boldLabel);

        if(showMetadata){
            //Coordinates;
            Vector4 coordinates = limb.GetCoordinates();
            //Sprite boxes;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sprite Box Start", GUILayout.Width(100));
            EditorGUILayout.SelectableLabel(coordinates.x.ToString(), EditorStyles.textField, GUILayout.Height(boxHeight));

            EditorGUILayout.LabelField("Sprite Box End", GUILayout.Width(100));
            EditorGUILayout.SelectableLabel(coordinates.y.ToString(), EditorStyles.textField, GUILayout.Height(boxHeight));
            EditorGUILayout.EndHorizontal();

            //Width / Height;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Width", GUILayout.Width(100));
            EditorGUILayout.SelectableLabel(coordinates.z.ToString(), EditorStyles.textField, GUILayout.Height(boxHeight));

            EditorGUILayout.LabelField("Height", GUILayout.Width(100));
            EditorGUILayout.SelectableLabel(coordinates.w.ToString(), EditorStyles.textField, GUILayout.Height(boxHeight));
            EditorGUILayout.EndHorizontal();

            //Coordinates;
            Vector2 pivot = limb.GetPivot();
            //Pivot;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pivot X", GUILayout.Width(100));
            EditorGUILayout.SelectableLabel(pivot.x.ToString(), EditorStyles.textField, GUILayout.Height(boxHeight));

            EditorGUILayout.LabelField("Pivot Y", GUILayout.Width(100));
            EditorGUILayout.SelectableLabel(pivot.y.ToString(), EditorStyles.textField, GUILayout.Height(boxHeight));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.Space();

        // Display the label and read-only texture field
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Texture");
        Rect objectFieldRect = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth * rectSize, EditorGUIUtility.fieldWidth * rectSize);

        EditorGUI.BeginDisabledGroup(true);
        EditorGUI.ObjectField(objectFieldRect, GUIContent.none, limb.GetTexture(), typeof(Texture2D), false);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }
}