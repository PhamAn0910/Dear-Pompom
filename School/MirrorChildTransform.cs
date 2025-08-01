using UnityEngine;

public class MirrorChildTransform : MonoBehaviour
{
    [Header("Mirror Settings")]
    [Tooltip("Drag the source child here - this will be copied")]
    public Transform sourceChild;
    
    [Tooltip("Drag the target child here - this will be adjusted")]
    public Transform targetChild;
    
    [Tooltip("Should position be mirrored?")]
    public bool mirrorPosition = true;
    
    [Tooltip("Should rotation be mirrored?")]
    public bool mirrorRotation = true;
    
    [Tooltip("Should scale be mirrored?")]
    public bool mirrorScale = true;
    
    [Header("Offset Options")]
    [Tooltip("Position offset from source")]
    public Vector3 positionOffset = Vector3.zero;
    
    [Tooltip("Rotation offset from source")]
    public Vector3 rotationOffset = Vector3.zero;
    
    [Tooltip("Scale multiplier (1 = same size)")]
    public Vector3 scaleMultiplier = Vector3.one;

    void Update()
    {
        if (sourceChild == null || targetChild == null)
            return;
            
        MirrorTransform();
    }
    
    void OnValidate()
    {
        if (sourceChild != null && targetChild != null)
        {
            MirrorTransform();
        }
    }
    
    public void MirrorTransform()
    {
        if (mirrorPosition)
        {
            targetChild.position = sourceChild.position + positionOffset;
        }
        
        if (mirrorRotation)
        {
            targetChild.rotation = sourceChild.rotation * Quaternion.Euler(rotationOffset);
        }
        
        if (mirrorScale)
        {
            targetChild.localScale = Vector3.Scale(sourceChild.localScale, scaleMultiplier);
        }
    }
}