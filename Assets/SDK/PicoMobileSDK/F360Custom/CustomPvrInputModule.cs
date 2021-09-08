using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CustomPvrInputModule : Pvr_InputModule
{
    //  custom
    public enum PointerType
    {
        Head,
        LeftHand,
        RightHand
    }

    [SerializeField] private Pvr_UIPointer pointer_head;
    [SerializeField] private Pvr_UIPointer pointer_leftController;
    [SerializeField] private Pvr_UIPointer pointer_rightController;

    protected override void Awake()
    {
        base.Awake();
        if(pointer_head == null)
        {
            pointer_head = GameObject.FindObjectOfType<Pvr_UIPointer>();
        }
        if(pointer_head != null && !pointers.Contains(pointer_head))
        {
            pointers.Add(pointer_head);
        }
        if(pointer_leftController != null && !pointers.Contains(pointer_leftController))
        {
            pointers.Add(pointer_leftController);
        }
        if(pointer_rightController != null && !pointers.Contains(pointer_rightController))
        {
            pointers.Add(pointer_rightController);
        }
    }
    public bool GetEventData(PointerType type, out PointerEventData eventData)
    {
        Pvr_UIPointer p=null;
        switch(type)
        {
            case PointerType.Head:      p = pointer_head; break;
            case PointerType.LeftHand:  p = pointer_leftController; break;  
            case PointerType.RightHand: p = pointer_rightController; break;
        }
        eventData = p != null ? p.pointerEventData : null;
        return eventData != null;
    }
    //
}
