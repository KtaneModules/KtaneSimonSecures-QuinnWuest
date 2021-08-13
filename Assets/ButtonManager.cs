using System;
using UnityEngine;

public class ButtonManager : MonoBehaviour {
    public KMSelectable Button;
    public Animator Animator;
    public string Property;

    public event Action OnPress = () => { };
    public event Action OnRelease = () => { };

    private void Start()
    {
        Button.OnInteract += () => { Animator.SetBool(Property, true); OnPress(); return false; };
        Button.OnInteractEnded += () => { Animator.SetBool(Property, false); OnRelease(); };
    }
}
