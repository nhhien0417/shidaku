using UnityEngine;
using UnityEngine.EventSystems;

namespace _Game.Scripts.Utils
{
    [RequireComponent(typeof(RectTransform))]
    public class DragableUI : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _target;
        [SerializeField] private bool _canDrag = true;
        [SerializeField] private bool _bringToFrontOnDrag = true;
        [SerializeField] private bool _clampToParent;
        [SerializeField] private bool _lockX;
        [SerializeField] private bool _lockY;

        private RectTransform _rectTransform;
        private RectTransform _dragParent;
        private Canvas _canvas;
        private Vector2 _dragOffset;
        private Vector2 _originalAnchoredPosition;
        private Vector2 _startAnchoredPosition;
        private bool _hasOriginalPosition;
        private bool _isPointerDown;
        private bool _isDragging;

        public bool CanDrag
        {
            get => _canDrag;
            set => _canDrag = value;
        }

        public bool IsPointerDown => _isPointerDown;
        public bool IsDragging => _isDragging;
        public RectTransform Target => _target;

        private RectTransform DragTarget
        {
            get
            {
                if (_target == null)
                    _target = _rectTransform != null ? _rectTransform : transform as RectTransform;

                return _target;
            }
        }

        private void Awake()
        {
            _rectTransform = transform as RectTransform;

            if (_target == null)
                _target = _rectTransform;

            _canvas = GetComponentInParent<Canvas>();
        }

        private void Start()
        {
            RefreshOriginalPosition();
        }

        private void OnDisable()
        {
            _isPointerDown = false;
            _isDragging = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanDrag)
                return;

            _isPointerDown = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPointerDown = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanDrag)
                return;

            RectTransform dragTarget = DragTarget;
            _dragParent = dragTarget != null ? dragTarget.parent as RectTransform : null;

            if (dragTarget == null || _dragParent == null)
                return;

            if (!TryGetPointerLocalPosition(eventData, out Vector2 pointerLocalPosition))
                return;

            _isDragging = true;
            _startAnchoredPosition = dragTarget.anchoredPosition;
            _dragOffset = dragTarget.anchoredPosition - pointerLocalPosition;

            if (_bringToFrontOnDrag)
                dragTarget.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || !CanDrag)
                return;

            if (!TryGetPointerLocalPosition(eventData, out Vector2 pointerLocalPosition))
                return;

            Vector2 targetPosition = pointerLocalPosition + _dragOffset;

            if (_lockX)
                targetPosition.x = _startAnchoredPosition.x;

            if (_lockY)
                targetPosition.y = _startAnchoredPosition.y;

            if (_clampToParent)
                targetPosition = ClampToParent(targetPosition);

            DragTarget.anchoredPosition = targetPosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isPointerDown = false;
            _isDragging = false;
        }

        public void SetPosition(Vector2 anchoredPosition)
        {
            DragTarget.anchoredPosition = anchoredPosition;
        }

        public void RefreshOriginalPosition()
        {
            Canvas.ForceUpdateCanvases();

            _originalAnchoredPosition = DragTarget.anchoredPosition;
            _hasOriginalPosition = true;
        }

        public void ResetToOriginalPosition()
        {
            if (!_hasOriginalPosition)
                RefreshOriginalPosition();

            DragTarget.anchoredPosition = _originalAnchoredPosition;
        }

        public void ResetToDragStartPosition()
        {
            DragTarget.anchoredPosition = _startAnchoredPosition;
        }

        private bool TryGetPointerLocalPosition(PointerEventData eventData, out Vector2 localPosition)
        {
            Camera eventCamera = GetEventCamera(eventData);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _dragParent,
                eventData.position,
                eventCamera,
                out localPosition);
        }

        private Camera GetEventCamera(PointerEventData eventData)
        {
            if (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            if (eventData.pressEventCamera != null)
                return eventData.pressEventCamera;

            return _canvas != null ? _canvas.worldCamera : null;
        }

        private Vector2 ClampToParent(Vector2 targetPosition)
        {
            RectTransform dragTarget = DragTarget;
            if (dragTarget == null || _dragParent == null)
                return targetPosition;

            Rect parentRect = _dragParent.rect;
            Rect targetRect = dragTarget.rect;

            float minX = targetPosition.x - targetRect.width * dragTarget.pivot.x;
            float maxX = targetPosition.x + targetRect.width * (1f - dragTarget.pivot.x);
            float minY = targetPosition.y - targetRect.height * dragTarget.pivot.y;
            float maxY = targetPosition.y + targetRect.height * (1f - dragTarget.pivot.y);

            if (minX < parentRect.xMin)
                targetPosition.x += parentRect.xMin - minX;
            else if (maxX > parentRect.xMax)
                targetPosition.x -= maxX - parentRect.xMax;

            if (minY < parentRect.yMin)
                targetPosition.y += parentRect.yMin - minY;
            else if (maxY > parentRect.yMax)
                targetPosition.y -= maxY - parentRect.yMax;

            return targetPosition;
        }
    }
}
