using Sirenix.OdinInspector; 
using UnityEngine;
using UnityEngine.Animations;  
using UnityEngine.Playables;  

[ExecuteInEditMode]
[SelectionBase]
public class CharComponent : MonoBehaviour {
    static readonly int AV_MirrorHash = Animator.StringToHash("is_mirror");
    [SerializeField] Animator _anim;
    [ShowInInspector, SerializeField] CharData _data;
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
        }
    } 

    void UpdateData() {
        if(_data == null)
            return;
        _data.FieldStandardPosition = transform.position;
        _data.yRotation = transform.rotation.eulerAngles.y; 
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

        //transform.localPosition += _anim.velocity; 
        //transform.localRotation *= _anim.deltaRotation;
    } 
    void SetAnim(AnimationClip clip) {
        if (clip == Clip)
            return;
        _overrideAnimation[_overrideAnimation.animationClips[0]] = clip;
    } 
}