using Sirenix.OdinInspector; 
using UnityEngine;
using UnityEngine.Animations;  
using UnityEngine.Playables;
#if DRILL_EXPORT_EDITOR
[ExecuteInEditMode]
#endif
[SelectionBase]
public class CharComponent : MonoBehaviour {
    [System.Serializable]
    public class CharSkin {
        public Animator Animator;
        public SkinnedMeshRenderer ShirtRenderer;
        public Transform HeadTf;
        [Range(0f, 100f)] public float Weight = 100f;
    } 

    static readonly int AV_MirrorHash = Animator.StringToHash("is_mirror");
    static readonly int AV_CycleOffset = Animator.StringToHash("cycle_offset");
    static readonly Color _EnemyColor = Color.softRed;
    static readonly Color _AllyColor = Color.white;
    static bool IsExportEditor =>
#if DRILL_EXPORT_EDITOR
        !Application.isPlaying;
#else
        false;
#endif
    [ShowInInspector, SerializeField] CharData _data;
    [SerializeField] CharSkin[] _skins;

    Animator _anim => _skins[_usedAnimatorSkinIdx].Animator;
    public CharSkin ActiveSkin => _skins[_usedAnimatorSkinIdx];

    int _usedAnimatorSkinIdx = 0;

    public CharData Data => _data; 
    [ShowInInspector]
    public AnimationClip Clip => _overrideAnimation ? _overrideAnimation.animationClips[0] : null;  
    public void SetData(CharData data, bool mirror) {
        _data = data;

        if(IsExportEditor && !_graph.IsValid())
            RecreateGraph();

        if(data != null) { 
            var pos = new Vector3(data.FieldStandardPosition.y,0,data.FieldStandardPosition.x);
            var rot = Quaternion.Euler(0, data.yRotation, 0);
            transform.SetLocalPositionAndRotation(pos, rot);
            SetAnim(data.Animation);
            if(_controllerPlayable.IsValid())
                _controllerPlayable.SetBool(AV_MirrorHash, mirror);
                
            _skins[_usedAnimatorSkinIdx].ShirtRenderer.sharedMaterial.color = data.IsFriendly ? _AllyColor : _EnemyColor;
        }
    } 

    public void RandomizeSkin() {
        if (_skins.Length > 0) {
            float totalWeight = 0f;
            foreach (var skin in _skins)
                totalWeight += skin.Weight;
            
            float roll = Random.Range(0f, totalWeight);
            float accumulated = 0f;
            _usedAnimatorSkinIdx = _skins.Length - 1;
            for (int i = 0; i < _skins.Length; i++) {
                accumulated += _skins[i].Weight;
                if (roll < accumulated) {
                    _usedAnimatorSkinIdx = i;
                    break;
                }
            }

            for (int i = 0; i < _skins.Length; i++) {
                _skins[i].Animator.gameObject.SetActive(i == _usedAnimatorSkinIdx);
            }
            RecreateGraph();
        } else {
            Debug.LogError("No animator skins assigned to CharComponent.");
        }
    }

    PlayableGraph _graph;
    AnimationPlayableOutput _output; 
    private void Start() {
        foreach (var skin in _skins) {
            skin.ShirtRenderer.sharedMaterial = new Material(skin.ShirtRenderer.sharedMaterial);
        }
        RandomizeSkin();
         
        foreach (var s in _skins) {
            s.Animator.keepAnimatorStateOnDisable = true;
            s.Animator.fireEvents = false;
            s.Animator.enabled = true;
            s.Animator.speed = 1f;
        }
        RecreateGraph();
    } 
    AnimatorControllerPlayable _controllerPlayable;
    AnimatorOverrideController _overrideAnimation; 
    void RecreateGraph() { 
        _overrideAnimation = new AnimatorOverrideController(_anim.runtimeAnimatorController);
        _anim.runtimeAnimatorController = _overrideAnimation;
        SetAnim(Data?.Animation);

        if (IsExportEditor) {
            if (_graph.IsValid())
                _graph.Destroy();

            _graph = PlayableGraph.Create("SingleAnimationGraph");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _controllerPlayable = AnimatorControllerPlayable.Create(_graph, _overrideAnimation);
            _output = AnimationPlayableOutput.Create(_graph, "AnimationOutput", _anim);
            _output.SetSourcePlayable(_controllerPlayable);
            _graph.Play();
        }
    }

    private void OnDestroy() {
        if (IsExportEditor && _graph.IsValid())
            _graph.Destroy();
    } 

    public void SetAnimationTime(float time) {
        if (IsExportEditor) {
            if (!_graph.IsValid() || !_controllerPlayable.IsValid())
                return;
        }

        if (!_overrideAnimation)
            RecreateGraph();

        var actualTime = time + (_data?.AnimationTimeOffset ?? 0f);
        var clip = _overrideAnimation.animationClips[0];
        var duration = clip ? clip.length : 0f;
        var normalized = (duration == 0f || actualTime < 0f) ? 0f : actualTime / duration;

        if (IsExportEditor) {
            _controllerPlayable.Play("Clip", 0, normalized);
            _controllerPlayable.SetFloat(AV_CycleOffset, normalized);
            _graph.Evaluate();
        } else {
            _anim.SetFloat(AV_CycleOffset, normalized);
            _anim.Update(0f);
        }
    } 
    void SetAnim(AnimationClip clip) {
        if (clip == Clip)
            return;
        _overrideAnimation[_overrideAnimation.animationClips[0]] = clip;
    } 
}