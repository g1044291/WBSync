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
