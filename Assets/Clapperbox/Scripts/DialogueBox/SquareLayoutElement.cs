using UnityEngine;
using UnityEngine.UI;

// Keeps this element 1:1 inside a HorizontalLayoutGroup: its width always matches the height
// the parent group gives it, so siblings with flexible width take whatever space is left over.
[RequireComponent(typeof(RectTransform))]
public class SquareLayoutElement : MonoBehaviour, ILayoutElement
{
    [SerializeField] private int priority = 2;

    private float _side;

    public void CalculateLayoutInputHorizontal()
    {
        // Widths are calculated before heights, so derive the side from the parent's
        // height (which doesn't depend on this element) rather than from our own rect.
        var parent = transform.parent as RectTransform;
        if (parent == null) { _side = 0; return; }

        var group = parent.GetComponent<HorizontalLayoutGroup>();
        float padding = group != null ? group.padding.vertical : 0;
        _side = Mathf.Max(0, parent.rect.height - padding);
    }

    public void CalculateLayoutInputVertical() { }

    public float minWidth => _side;
    public float preferredWidth => _side;
    public float flexibleWidth => 0;
    public float minHeight => -1;
    public float preferredHeight => -1;
    public float flexibleHeight => 1;
    public int layoutPriority => priority;

    private void OnEnable() => SetDirty();
    private void OnDisable() => SetDirty();
#if UNITY_EDITOR
    private void OnValidate() => SetDirty();
#endif

    private void SetDirty()
    {
        if (!isActiveAndEnabled) return;
        LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
    }
}
