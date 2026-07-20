let _lines = [];

// pairs: [[predecessorTaskId, successorTaskId], ...]
window.updateLeaderLines = (containerId, pairs) => {
    _lines.forEach(l => l.remove());
    _lines = [];

    const container = document.getElementById(containerId);
    if (!container) return;

    pairs.forEach(([fromId, toId]) => {
        const start = document.getElementById(`task-bar-${fromId}`);
        const end = document.getElementById(`task-bar-${toId}`);
        if (!start || !end) return;
        _lines.push(new LeaderLine(start, end, {
            startSocket: 'right',
            endSocket: 'left',
            color: '#9333ea',
            size: 2,
            path: 'arc',
            startSocketGravity: 4,
            endSocketGravity: 0,
            startPlugSize: 2,
            endPlug: 'arrow1',
            endPlugSize: 1.5,
        }));
    });

    window.repositionLeaderLines(containerId);
};

window.repositionLeaderLines = (containerId) => {
    const container = document.getElementById(containerId);
    if (!container || _lines.length === 0) return;

    const rect = container.getBoundingClientRect();
    const intersects = (el) => {
        const r = el.getBoundingClientRect();
        return r.right > rect.left && r.left < rect.right && r.bottom > rect.top && r.top < rect.bottom;
    };

    _lines.forEach(line => {
        if (intersects(line.start) || intersects(line.end)) {
            line.show('none');
            line.position();
        } else {
            line.hide('none');
        }
    });
};

window.initLeaderLineSync = (chartPaneId, taskPaneRowsId) => {
    const reposition = () => window.repositionLeaderLines(chartPaneId);
    document.getElementById(chartPaneId)?.addEventListener('scroll', reposition);
    document.getElementById(taskPaneRowsId)?.addEventListener('scroll', reposition);
    window.addEventListener('resize', reposition);
};

window.disposeLeaderLines = () => {
    _lines.forEach(l => l.remove());
    _lines = [];
};
