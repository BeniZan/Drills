using Sirenix.OdinInspector;
#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[ExecuteInEditMode]
public class TeamManeuverPlacer : SingletonBehaviors.SingletonMono<TeamManeuverPlacer> {
    public Drill CurrentActive { get; private set; }
    [SerializeField] CharComponent _template;
    [SerializeField] Transform _courtTf;
    [SerializeField] List<CharComponent> _placedChars = new List<CharComponent>();
#if UNITY_EDITOR
    [field: SerializeField, Get] public EditorTeamAnimator EditorAnimator { get; private set; }
#endif
    public IReadOnlyList<CharComponent> PlacedChars => _placedChars;
    public void Activate(Drill move) {
        if (CurrentActive)
            Deactivate();
        if (move) 
            CurrentActive = move;
        UpdateChars();
#if UNITY_EDITOR
        SceneView.duringSceneGui -= SceneView_duringSceneGui;
        SceneView.duringSceneGui += SceneView_duringSceneGui;
#endif
    }

    public void UpdateChars() {
        if (!CurrentActive)
            return;

        var originPos = CurrentActive.OriginPoint;
        var originRot = Quaternion.Euler(0f, CurrentActive.OriginYRotation, 0f);
        if (CurrentActive.MirrorLeftRight)
            originRot *= Quaternion.Euler(0f, 180f, 0f);
        transform.SetPositionAndRotation(originPos, originRot);

        int i = 0;
        for (; i < CurrentActive.CharsData.Count; i++) {
            if (_placedChars.Count <= i) {
                var spawned = Instantiate(_template, transform);
                spawned.gameObject.SetActive(true);
                _placedChars.Add(spawned);
            }
            _placedChars[i].SetData(CurrentActive.CharsData[i], CurrentActive.MirrorLeftRight);
        }
        while(i < _placedChars.Count) {
            if (_placedChars[i])
                _placedChars[i].gameObject.SafeDestroy();
            _placedChars.RemoveAt(i);
        }
    }

    private void Update() {
        UpdateChars();
        _courtTf.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
    } 

    public void Deactivate() {
        foreach (var placedChar in _placedChars)
            if(placedChar)
                placedChar.gameObject.SafeDestroy();
        _placedChars.Clear();
        CurrentActive = null;
#if UNITY_EDITOR
        SceneView.duringSceneGui -= SceneView_duringSceneGui;
#endif
    }

    private void OnEnable() {
        Activate(CurrentActive);
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.1f);
        GizmosU.GizmosArrow(transform.position, transform.rotation.EulerSeperateY() * Vector3.forward);
    } 


#if UNITY_EDITOR 

    protected override void OnDestroy() {
        base.OnDestroy();
        SceneView.duringSceneGui -= SceneView_duringSceneGui;
    }
    GUIStyle _style;
    private void SceneView_duringSceneGui(SceneView obj) {
        if (!CurrentActive)
            return;

        if(_style == null) {
            _style = new GUIStyle(SirenixGUIStyles.WhiteLabel); 
            _style.richText = true;
            _style.fontSize = 35;
        }

        Handles.BeginGUI();  
        for (int i = 0; i < _placedChars.Count; i++) {
            var head = _placedChars[i].Head;
            var pos = head.position + new Vector3(0, 0.1f, 0);
            var lbl = $"<u>{i}</u>";
            Handles.Label(pos, lbl, _style);
        }
        Handles.EndGUI();
    } 
#endif
}
