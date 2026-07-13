using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;

#if UNITY_EDITOR
[ExecuteInEditMode]
public class EditorTeamAnimator : MonoBehaviour {
    [SerializeField] TeamManeuverPlacer _movePlacer;
    public float AnimationTime = 0;
    public bool IsRunning { get; private set; }
    public float MaxAnimationTime { get; private set; }
    [Button]
    public void RestartAnimation() {
        ToggleAnimation(true);
        AnimationTime = 0f;
    } 
    public void ToggleAnimation(bool isRunning) {
        if (IsRunning == isRunning)
            return;

        IsRunning = isRunning;
        enabled = true;
    } 

    private void OnEnable() { 
        RestartAnimation(); 
        ToggleAnimation(false);
    }
     
    private void Update() {
        if (IsRunning)  
            AnimationTime += Time.deltaTime;
        MaxAnimationTime = 0f;
        foreach (var character in _movePlacer.PlacedChars) {
            var clipLength = character.Clip == null ? 0f : character.Clip.length;
            clipLength += character.Data == null ? 0f : Mathf.Abs(character.Data.AnimationTimeOffset);
            MaxAnimationTime = Mathf.Max(MaxAnimationTime, clipLength); 
        }
        AnimationTime = Mathf.Min(AnimationTime, MaxAnimationTime); 
        foreach (var character in _movePlacer.PlacedChars) {  
            if(character && character.Data != null) {
                var anim = character.Data.Animation;
                character.SetAnimationTime(AnimationTime);
            }
        }  
    }
}
#endif
