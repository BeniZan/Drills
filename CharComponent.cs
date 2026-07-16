using Sirenix.OdinInspector; 
using UnityEngine;
using UnityEngine.Animations;  
using UnityEngine.Playables;
#if DRILL_EXPORT_EDITOR
[ExecuteInEditMode]
#endif
[SelectionBase]
public class CharComponent : MonoBehaviour {
    static readonly int AV_MirrorHash = Animator.StringToHash("is_mirror");
    static readonly Color _EnemyColor = Color.softRed;
    static readonly Color _AllyColor = Color.white; 
    [ShowInInspector, SerializeField] CharData _data;
    [SerializeField] SkinnedMeshRenderer[] _shirtRenderers;
    [SerializeField] Animator[] _animatorSkins;

    Animator _anim => _animatorSkins[_usedAnimatorSkinIdx];

    int _usedAnimatorSkinIdx = 0;

    public CharData Data => _data;
    public Transform Head;
    [ShowInInspector]
    public AnimationClip Clip => _overrideAnimation ? _overrideAnimation.animationClips[0] : null; 
    bool _isMirror;
    public void SetData(CharData data, bool mirror) {
        _data = data;
        if(data != null) { 
            var pos = new Vector3(data.FieldStandardPosition.y,0,data.FieldStandardPosition.x);
            var rot = Quaternion.Euler(0, data.yRotation, 0);
            transform.SetLocalPositionAndRotation(pos, rot);
            _controllerPlayable.SetBool(AV_MirrorHash, mirror);
            SetAnim(data.Animation);
                
            _shirtRenderers[_usedAnimatorSkinIdx].sharedMaterial.color = data.IsFriendly ? _AllyColor : _EnemyColor;
        }
    }


    private void Awake() {
        foreach(var skin in _shirtRenderers) {
            skin.sharedMaterial = new Material(skin.sharedMaterial);      
        }
        RandomizeSkin();
    }

    public void RandomizeSkin() {
        //todo
        if (_animatorSkins.Length > 0) {
            _usedAnimatorSkinIdx = Random.Range(0, _animatorSkins.Length);
            for (int i = 0; i < _animatorSkins.Length; i++) {
                _animatorSkins[i].gameObject.SetActive(i == _usedAnimatorSkinIdx);
            }
            RecreateGraph();
        } else {
            Debug.LogError("No animator skins assigned to CharComponent.");
        }
    }

    PlayableGraph _graph;
    AnimationPlayableOutput _output;
    private void OnEnable() {
        _anim.fireEvents = true;
        _anim.enabled = true;
        RecreateGraph(); 
    }
    AnimatorControllerPlayable _controllerPlayable;
    AnimatorOverrideController _overrideAnimation;
    void RecreateGraph() {
        _overrideAnimation = new AnimatorOverrideController(_anim.runtimeAnimatorController);
        _anim.runtimeAnimatorController = _overrideAnimation;

        if(_graph.IsValid())
            _graph.Destroy();

        _graph = PlayableGraph.Create("SingleAnimationGraph");
        _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        _controllerPlayable = AnimatorControllerPlayable.Create(_graph, _overrideAnimation);
        _output = AnimationPlayableOutput.Create(_graph, "AnimationOutput", _anim);
        _output.SetSourcePlayable(_controllerPlayable);
        SetAnim(null); 
        _graph.Play();
    } 

    private void OnDisable() {
        if(_graph.IsValid())
            _graph.Destroy();
    } 

    public void SetAnimationTime(float time) {
        if (!_graph.IsValid() || !_controllerPlayable.IsValid())
            return;

        if (_controllerPlayable.IsValid()) {
            var actualTime = time + (_data?.AnimationTimeOffset ?? 0f);
            var duration = _overrideAnimation.animationClips[0].length;
            var normalized = (duration == 0f || actualTime < 0f) ? 0f : actualTime / duration;
            _controllerPlayable.Play("Clip", 0, normalized); 
        }
        if(_graph.IsValid())
            _graph.Evaluate(); 
    } 
    void SetAnim(AnimationClip clip) {
        if (clip == Clip)
            return;
        _overrideAnimation[_overrideAnimation.animationClips[0]] = clip;
    } 
}