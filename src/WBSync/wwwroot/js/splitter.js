window.initSortable = (containerId, dotNetRef) => {
    const container = document.getElementById(containerId);
    if (!container) return;

    new Sortable(container, {
        animation: 150,
        // WebView2 では HTML5 DnD が不安定なため SortableJS 独自実装を使用
        forceFallback: true,
        fallbackClass: 'task-row-dragging',
        onStart: () => { dotNetRef.invokeMethodAsync('SetDragging', true); },
        // 同一階層のみ許可：dragged と related の data-parent-id が一致しなければ移動不可
        onMove: (evt) => {
            if (!evt.related) return true;
            const draggedParent = evt.dragged.dataset.parentId ?? '';
            const relatedParent = evt.related.dataset.parentId ?? '';
            return draggedParent === relatedParent;
        },
        onEnd: (evt) => {
            dotNetRef.invokeMethodAsync('SetDragging', false);
            // ドロップ後、同一 parent-id のすべての兄弟を順番に列挙してBlazorへ通知
            const draggedParentId = evt.item.dataset.parentId ?? '';
            const selector = draggedParentId === ''
                ? '[data-parent-id=""]'
                : `[data-parent-id="${draggedParentId}"]`;
            const siblings = Array.from(container.querySelectorAll(selector));
            const taskIds = siblings.map(el => parseInt(el.dataset.taskId, 10));
            dotNetRef.invokeMethodAsync('OnSortOrderChanged', taskIds);
        }
    });
};

window.initScrollSync = (leftId, rightId) => {
    const left = document.getElementById(leftId);
    const right = document.getElementById(rightId);
    if (!left || !right) return;

    let syncing = false;

    left.addEventListener('scroll', () => {
        if (syncing) return;
        syncing = true;
        right.scrollTop = left.scrollTop;
        syncing = false;
    });

    right.addEventListener('scroll', () => {
        if (syncing) return;
        syncing = true;
        left.scrollTop = right.scrollTop;
        syncing = false;
    });
};

window.initSplitter = (splitterId, leftPaneId, minWidth, maxWidth) => {
    const splitter = document.getElementById(splitterId);
    const leftPane = document.getElementById(leftPaneId);
    if (!splitter || !leftPane) return;

    let startX = 0;
    let startWidth = 0;

    splitter.addEventListener('pointerdown', (e) => {
        startX = e.clientX;
        startWidth = leftPane.offsetWidth;
        splitter.setPointerCapture(e.pointerId);
        document.body.style.cursor = 'col-resize';
        document.body.style.userSelect = 'none';
    });

    splitter.addEventListener('pointermove', (e) => {
        if (!splitter.hasPointerCapture(e.pointerId)) return;
        const newWidth = Math.min(maxWidth, Math.max(minWidth, startWidth + (e.clientX - startX)));
        leftPane.style.width = newWidth + 'px';
        leftPane.style.minWidth = newWidth + 'px';
    });

    splitter.addEventListener('pointerup', (e) => {
        if (!splitter.hasPointerCapture(e.pointerId)) return;
        splitter.releasePointerCapture(e.pointerId);
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
    });
};
