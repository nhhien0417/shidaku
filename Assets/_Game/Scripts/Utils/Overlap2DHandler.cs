using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Overlap2DHandler
{
    public Collider2D Cld2D => collider2D;

    [SerializeField] private int maxOverlapCollider = 30;
    [SerializeField] private Collider2D collider2D;
    [SerializeField] private ContactFilter2D contactFilter;
    
    private Collider2D[] listOverlap;
    private LayerMask originalLayer;
    private bool isOriginalLayerSet = false;

    public void SetCollider(Collider2D cld)
    {
        collider2D = cld;
    }

    public int GetOverlapColliders(Collider2D[] listOverlap)
    {
        if (collider2D == null)
            return 0;

        if (listOverlap == null)
            listOverlap = new Collider2D[maxOverlapCollider];

        return collider2D.Overlap(contactFilter, listOverlap);
    }

    public int GetOverlapColliders(Collider2D[] listOverlap, ContactFilter2D cf)
    {
        ContactFilter2D cache = contactFilter;
        contactFilter = cf;
        int result = GetOverlapColliders(listOverlap);
        contactFilter = cache;
        return result;
    }

    public int GetOverlapColliders(Collider2D[] listOverlap, params string[] layersFilter)
    {
        return GetOverlapColliders(listOverlap, CreateContactFilter2D(layersFilter));
    }

    public Collider2D[] GetOverlapColliders(out int numOfColliders)
    {
        if (collider2D == null)
        {
            numOfColliders = 0;
            Debug.LogError("Base collider not found!");
        }
        else
        {
            if (listOverlap == null)
                listOverlap = new Collider2D[maxOverlapCollider];

            numOfColliders = collider2D.Overlap(contactFilter, listOverlap);
        }

        return listOverlap;
    }

    public Collider2D[] GetOverlapColliders(out int numOfColliders, ContactFilter2D cf)
    {
        ContactFilter2D cache = contactFilter;
        contactFilter = cf;
        Collider2D[] result = GetOverlapColliders(out numOfColliders);
        contactFilter = cache;
        return result;
    }

    public Collider2D[] GetOverlapColliders(out int numOfColliders, params string[] layersFilter)
    {
        return GetOverlapColliders(out numOfColliders, CreateContactFilter2D(layersFilter));
    }

    public bool IsInsideACollider()
    {
        Collider2D[] result = GetOverlapColliders(out int numOfCollider);

        for (int i = 0; i < numOfCollider; i++)
        {
            if (result[i].bounds.Contains(Cld2D.bounds.min) && result[i].bounds.Contains(Cld2D.bounds.max))
                return true;
        }

        return false;
    }

    public bool IsInsideACollider(ContactFilter2D cf)
    {
        ContactFilter2D cache = contactFilter;
        contactFilter = cf;
        bool result = IsInsideACollider();
        contactFilter = cache;
        return result;
    }

    public bool IsInsideACollider(params string[] layersFilter)
    {
        return IsInsideACollider(CreateContactFilter2D(layersFilter));
    }

    public bool IsOverlapCollider(params string[] layersFilter)
    {
        return GetOverlapColliders(null, layersFilter) > 0;
    }

    public LayerMask GetLayerFilter()
    {
        return contactFilter.layerMask;
    }
    
    public void SetLayerFilter(LayerMask layerAreaBlockedLayer)
    {
        if (isOriginalLayerSet == false)
        {
            originalLayer = contactFilter.layerMask;
            isOriginalLayerSet = true;
        }
        
        contactFilter.SetLayerMask(layerAreaBlockedLayer);
    }

    public void SetLayerFilter(int layer)
    {
        if (isOriginalLayerSet == false)
        {
            originalLayer = contactFilter.layerMask;
            isOriginalLayerSet = true;
        }
        
        contactFilter.SetLayerMask(1 << layer);
    }
    
    public void ResetLayerFilterToOriginal()
    {
        if (isOriginalLayerSet)
        {
            contactFilter.SetLayerMask(originalLayer);
        }
    }

    private ContactFilter2D CreateContactFilter2D(params string[] layersFilter)
    {
        LayerMask layer = LayerMask.GetMask(layersFilter);
        ContactFilter2D cf = default(ContactFilter2D);
        cf.useTriggers = true;
        cf.useLayerMask = true;
        cf.SetLayerMask(layer);
        return cf;
    }
}
