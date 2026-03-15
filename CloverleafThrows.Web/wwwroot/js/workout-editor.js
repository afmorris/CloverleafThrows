// workout-editor.js — handles AJAX CRUD + drag-and-drop reordering

async function postJson(url, data) {
    const res = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: new URLSearchParams(data)
    });
    return res.json();
}

async function postJsonBody(url, data) {
    const res = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
    });
    return res.json();
}

// ---- Exercise CRUD ----

async function addExercise(groupId) {
    const result = await postJson('/Admin/Workouts/AddExercise', {
        exerciseGroupId: groupId,
        exerciseName: 'New Exercise',
        reps: '3 x 10'
    });

    const list = document.querySelector(`.exercise-list[data-group-id="${groupId}"]`);
    if (!list) return;

    const row = document.createElement('div');
    row.className = 'exercise-edit-row';
    row.dataset.exerciseId = result.id;
    row.innerHTML = `
        <span class="drag-handle" title="Drag to reorder">&#x2630;</span>
        <input type="text" class="num-input" value="" placeholder="#" onchange="updateExercise(${result.id}, this.parentElement)" />
        <input type="text" class="name-input" value="New Exercise" placeholder="Exercise name" onchange="updateExercise(${result.id}, this.parentElement)" />
        <input type="text" class="reps-input" value="3 x 10" placeholder="Reps" onchange="updateExercise(${result.id}, this.parentElement)" />
        <button class="delete-btn" onclick="deleteExercise(${result.id}, this.parentElement)" title="Remove">&times;</button>
    `;
    list.appendChild(row);

    // Focus the name input
    row.querySelector('.name-input').focus();
    row.querySelector('.name-input').select();
    initDragDrop(list);
}

async function updateExercise(id, row) {
    const inputs = row.querySelectorAll('input');
    await postJson('/Admin/Workouts/UpdateExercise', {
        Id: id,
        Number: inputs[0].value,
        ExerciseName: inputs[1].value,
        Reps: inputs[2].value,
        SortOrder: Array.from(row.parentElement.children).indexOf(row)
    });
}

async function deleteExercise(id, row) {
    if (!confirm('Remove this exercise?')) return;
    await postJson('/Admin/Workouts/DeleteExercise', { id });
    row.parentElement.removeChild(row);
}

// ---- Groups ----

async function addGroup(sectionId) {
    const label = prompt('Group label (optional, e.g. "Superset 1"):', '');
    const result = await postJson('/Admin/Workouts/AddGroup', {
        workoutSectionId: sectionId,
        label: label || ''
    });
    location.reload();
}

// ---- Default sections ----

async function addDefaultSections(workoutDayId) {
    const sections = [
        { name: 'Throws Warmup', headerColor: '#1F4E79' },
        { name: 'Throwing', headerColor: '#C55A11' },
        { name: 'Lifting', headerColor: '#1F4E79' },
        { name: 'Mobility', headerColor: '#375623' },
        { name: 'Core', headerColor: '#4A3670' }
    ];

    for (let i = 0; i < sections.length; i++) {
        const s = sections[i];
        const result = await postJson('/Admin/Workouts/AddSection', {
            workoutDayId: workoutDayId,
            name: s.name,
            headerColor: s.headerColor
        });

        // Add a default group to each section
        await postJson('/Admin/Workouts/AddGroup', {
            workoutSectionId: result.id,
            label: ''
        });
    }

    location.reload();
}

// ---- Drag and drop reordering ----

function initDragDrop(container) {
    const rows = container.querySelectorAll('.exercise-edit-row');
    rows.forEach(row => {
        const handle = row.querySelector('.drag-handle');
        if (!handle) return;

        handle.addEventListener('mousedown', (e) => {
            e.preventDefault();
            row.classList.add('dragging');
            row.style.opacity = '0.5';

            const onMove = (e) => {
                const siblings = [...container.querySelectorAll('.exercise-edit-row:not(.dragging)')];
                const next = siblings.find(s => {
                    const box = s.getBoundingClientRect();
                    return e.clientY < box.top + box.height / 2;
                });
                container.insertBefore(row, next || null);
            };

            const onUp = async () => {
                row.classList.remove('dragging');
                row.style.opacity = '1';
                document.removeEventListener('mousemove', onMove);
                document.removeEventListener('mouseup', onUp);

                // Save new order
                const items = [...container.querySelectorAll('.exercise-edit-row')].map((r, i) => ({
                    id: parseInt(r.dataset.exerciseId),
                    sortOrder: i
                }));
                await postJsonBody('/Admin/Workouts/ReorderExercises', items);
            };

            document.addEventListener('mousemove', onMove);
            document.addEventListener('mouseup', onUp);
        });
    });
}

// ---- Exercise library autocomplete ----

function initAutocomplete() {
    let library = [];
    try {
        const el = document.getElementById('exercise-library');
        if (el) library = JSON.parse(el.textContent);
    } catch (e) { return; }

    document.addEventListener('focus', (e) => {
        if (!e.target.classList?.contains('name-input')) return;
        // Could add dropdown autocomplete here
    }, true);
}

// ---- Init ----

document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.exercise-list').forEach(initDragDrop);
    initAutocomplete();
});
