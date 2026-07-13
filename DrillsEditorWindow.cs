#if UNITY_EDITOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;
using static System.Collections.Specialized.BitVector32;

public class DrillsEditorWindow : OdinMenuEditorWindow {
    readonly public string MovesDir = "Assets/Data/Drills";
    [MenuItem("Window/Drills Editor")]
    private static void OpenWindow() {
        var win = GetWindow<DrillsEditorWindow>();
        if (!win) win = CreateWindow<DrillsEditorWindow>();
        win.Show();
    } 
     
    protected override OdinMenuTree BuildMenuTree() {
        var tree = new OdinMenuTree();
        tree.AddAllAssetsAtPath("", MovesDir, typeof(Drill), true, true);
        
        tree.Config.DrawSearchToolbar = true;
        tree.Selection.SelectionChanged += Selection_SelectionChanged;
        tree.Selection.SupportsMultiSelect = false;

        var createNewName = " Create New";
        tree.Add(createNewName, new() , SdfIconType.PlusCircle);
        tree.GetMenuItem(createNewName).OnDrawItem += OnDrawCreateNewItem;
         
        return tree;
    }

    private void OnProjectChange() {
        ForceMenuTreeRebuild();
        var placer = TeamManeuverPlacer.Instance;
        if (placer && placer.CurrentActive) {
            var activeItem =
                MenuTree.EnumerateTree().First(i => (i.Value as Drill) == placer.CurrentActive);
            activeItem.Select();
        }
    }

    void OnDrawCreateNewItem(OdinMenuItem item) {
        var lbl = item.LabelRect;
        lbl.width -= 4f;
        if(GUI.Button(lbl, " Create New")) {
            CreateNewMove();
        }
    }
    void ValidateDir() {
        if (!Directory.Exists(MovesDir))
            Directory.CreateDirectory(MovesDir);
    } 
    OdinMenuItem CreateNewMove() {
        var teamMenuver = CreateInstance<Drill>();
        teamMenuver.CharsData.Add(new CharData());
        ValidateDir();
        var assetPath = AssetDatabase.GenerateUniqueAssetPath(MovesDir + "/NewTeamManeuver.asset");
        AssetDatabase.CreateAsset(teamMenuver, assetPath);
        AssetDatabase.SaveAssets();
        return AddMoveToTree(assetPath);
    }
    OdinMenuItem AddMoveToTree(string assetPath) {
        var assetName = Path.GetFileNameWithoutExtension(assetPath);
        MenuTree.AddAssetAtPath(assetPath, assetPath, typeof(Drill));
        var menuItem = MenuTree.GetMenuItem(assetPath);
        menuItem.Name = assetName; 
        return MenuTree.GetMenuItem(assetPath);
    }
    private void Selection_SelectionChanged(SelectionChangedType obj) { 
        MenuTree.Selection.SelectionChanged -= Selection_SelectionChanged;
        try {
            OnSelection();
        }catch(System.Exception ex) { Debug.LogException(ex); }  
        finally {
            MenuTree.Selection.SelectionChanged += Selection_SelectionChanged;
        }
    }

    protected override void OnBeginDrawEditors() {
        DrawRename(); 
        base.OnBeginDrawEditors();
    }

    protected override void OnEndDrawEditors() { 
        base.OnEndDrawEditors();
        DrawAnimationTimeSlider();
        var r = EditorGUILayout.GetControlRect(GUILayout.MaxHeight(25));
        if (SirenixEditorGUI.SDFIconButton(rect: r, "Export Package", SdfIconType.Send))
            ExportPackage();
        EditorGUILayout.Space(10);
    }

    void DrawRename() {
        var currentMove = TeamManeuverPlacer.Instance.CurrentActive;
        if (currentMove == null)
            return;

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();
        var rename = SirenixEditorFields.DelayedTextField("Name: ", currentMove.name);
        if (EditorGUI.EndChangeCheck() && rename != currentMove.name) {
            var path = AssetDatabase.GetAssetPath(currentMove);
            AssetDatabase.RenameAsset(path, rename);
        }

        if (TeamManeuverPlacer.Instance && MenuTree.Selection.SelectedValue is Drill move) {
            var r = EditorGUILayout.GetControlRect(GUILayout.MaxHeight(17), GUILayout.MaxWidth(30));
            if (SirenixEditorGUI.SDFIconButton(rect: r, icon: SdfIconType.Trash, iconAlignment: IconAlignment.RightOfText)) {
                var userConfirm =
                EditorUtility.DisplayDialog("Delete move", $"Are you sure you want to delete {move.name}?", "Delete", "cancel");
                if (userConfirm && AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(move))) {
                    AssetDatabase.Refresh();
                    return;
                }
            }
        }
         
        EditorGUILayout.EndHorizontal();
        SirenixEditorGUI.HorizontalLineSeparator(Color.gray);

    }

    bool _loopAnimation;

    void DrawAnimationTimeSlider() {
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Time: ", GUILayout.MaxWidth(100)); 

        var editorAnimator = TeamManeuverPlacer.Instance.EditorAnimator;
        var nicefyAnimationTime = editorAnimator.AnimationTime;
        nicefyAnimationTime = float.Parse($"{nicefyAnimationTime:F2}");
        editorAnimator.AnimationTime =
        EditorGUILayout.Slider(nicefyAnimationTime, 0f, editorAnimator.MaxAnimationTime);
        var btnsRect = EditorGUILayout.GetControlRect(GUILayout.Width(80));
        var playOrPauseIcon = editorAnimator.IsRunning ? SdfIconType.Pause : SdfIconType.Play;
        if (SirenixEditorGUI.SDFIconButton(btnsRect.Split(0, 3), playOrPauseIcon, null))
            editorAnimator.ToggleAnimation(!editorAnimator.IsRunning);
        if (SirenixEditorGUI.SDFIconButton(btnsRect.Split(1, 3), SdfIconType.ArrowCounterclockwise, null))
            editorAnimator.RestartAnimation();

        bool _wasPushed = false;
        if (_loopAnimation) {
            _wasPushed = true;
            GUIHelper.PushColor(Color.softGreen);
        }

        if (SirenixEditorGUI.SDFIconButton(btnsRect.Split(2, 3), SdfIconType.ArrowRepeat, null))
            _loopAnimation = !_loopAnimation;

        if (_wasPushed)
            GUIHelper.PopColor();

        if (_loopAnimation && editorAnimator.IsRunning && editorAnimator.AnimationTime >= editorAnimator.MaxAnimationTime) {
            editorAnimator.AnimationTime = 0;
        }


        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    public void ExportPackage() {
        try { 
            var prefKey = "TEAM_MEN_SAVE_title";
            var lastSaveLoc =
                EditorPrefs.GetString(prefKey, Path.Combine(Application.dataPath , "package"));

            var dir = Path.GetDirectoryName(lastSaveLoc);
            var fileName = Path.GetFileNameWithoutExtension(lastSaveLoc);
            var saveLocation = 
                EditorUtility.SaveFilePanel("Export moves", dir, fileName , "unitypackage");
            if (string.IsNullOrEmpty(saveLocation))
                return;

            EditorPrefs.SetString(prefKey, saveLocation);
            var moves =  MenuTree.MenuItems.Select(i => i.Value as Drill)
                                            .Where(i => i);
            var paths = moves.Select(m => AssetDatabase.GetAssetPath(m)).ToArray();
            AssetDatabase.ExportPackage(paths, saveLocation, ExportPackageOptions.IncludeDependencies | ExportPackageOptions.Interactive);
            EditorApplication.delayCall += () => {
                Application.OpenURL($"file://[{saveLocation}]");
            };  
        } catch(System.Exception ex) {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Error exporting", ex.Message, "close");
        } 
    } 

    private void OnInspectorUpdate() {
        Repaint();

    } 

    void OnSelection() {
        if (MenuTree.Selection.Count == 0 && MenuTree.MenuItems.Count > 0)
            MenuTree.Selection.Add(MenuTree.MenuItems[0]);
        if (MenuTree.Selection.Count == 1 && MenuTree.Selection[0].Value == null) {
            var addedItem = CreateNewMove();
            MenuTree.Selection.Add(addedItem);
            for (int i = 0; i < MenuTree.MenuItems.Count; i++) {
                var item = MenuTree.MenuItems[i];
                if (item != addedItem) {
                    MenuTree.Selection.RemoveAt(i);
                }
            } 
        }

        var placer = TeamManeuverPlacer.Instance;
        if (!placer)
           Debug.LogError("placer not found");

        if (MenuTree.Selection.SelectedValue is Drill move) {
            placer.Activate(move);
        }
        else placer.Deactivate();
    }   
}

#endif