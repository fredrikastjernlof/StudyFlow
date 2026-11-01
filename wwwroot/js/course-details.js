"use strict";

// Handle opening and closing the course edit and delete dialogs.
const editCourseDialog = document.getElementById("editCourseDialog");

document.getElementById("openEditCourseDialog").addEventListener("click", () => {
    editCourseDialog.showModal();
});

document.getElementById("closeEditCourseDialog").addEventListener("click", () => {
    editCourseDialog.close();
});

const deleteCourseDialog = document.getElementById("deleteCourseDialog");

document.getElementById("openDeleteCourseDialog").addEventListener("click", () => {
    editCourseDialog.close();
    deleteCourseDialog.showModal();
});

document.getElementById("closeDeleteCourseDialog").addEventListener("click", () => {
    deleteCourseDialog.close();
    editCourseDialog.showModal();
});

// Open the selected task's details dialog.
document.addEventListener("click", event => {
    const button = event.target.closest(".task-details-button");
    if (!button) return;

    const taskId = button.dataset.taskId;
    document.getElementById(`taskDialog-${taskId}`)?.showModal();
});

// Handle task dialog actions.
document.addEventListener("click", event => {
    const button = event.target.closest(
        ".close-task-dialog, " +
        ".open-edit-task-dialog, " +
        ".close-edit-task-dialog, " +
        ".open-delete-task-dialog, " +
        ".cancel-delete-task"
    );

    if (!button) return;

    if (button.matches(
        ".close-task-dialog, .close-edit-task-dialog, .cancel-delete-task"
    )) {
        button.closest("dialog")?.close();
        return;
    }

    const taskId = button.dataset.taskId;

    if (button.matches(".open-edit-task-dialog")) {
        document.getElementById(`taskDialog-${taskId}`)?.close();
        document.getElementById(`editTaskDialog-${taskId}`)?.showModal();
    }

    if (button.matches(".open-delete-task-dialog")) {
        document.getElementById(`taskDialog-${taskId}`)?.close();
        document.getElementById(`deleteTaskDialog-${taskId}`)?.showModal();
    }
});

// Remember the current status before it changes.
document.addEventListener("focusin", event => {
    const select = event.target;

    if (select.matches(".task-status-select")) {
        select.dataset.savedStatus = select.value;
    }
});

// Save task status.
document.addEventListener("change", async event => {
    const select = event.target;

    if (!select.matches(".task-status-select")) return;

    const savedStatus = select.dataset.savedStatus ?? select.value;
    const form = select.closest("form");
    const formData = new FormData(form);

    select.disabled = true;

    try {
        const response = await fetch(form.action, {
            method: "POST",
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            body: formData
        });

        if (!response.ok) {
            throw new Error("Failed to save status.");
        }

        const result = await response.json();

        if (!result.success) {
            throw new Error("Failed to save status.");
        }

        select.dataset.savedStatus = select.value;

        // Fetch the updated course page.
        const pageResponse = await fetch(window.location.href);

        if (!pageResponse.ok) {
            throw new Error("Failed to refresh tasks.");
        }

        const html = await pageResponse.text();
        const updatedDocument = new DOMParser().parseFromString(html, "text/html");

        const currentContent = document.getElementById("task-status-content");
        const updatedContent = updatedDocument.getElementById("task-status-content");

        if (!currentContent || !updatedContent) {
            throw new Error("Task content not found.");
        }

        // Update only the task sections.
        const currentPage = currentContent.querySelector(
            ".dashboard-book-page:first-child"
        );

        const updatedPage = updatedContent.querySelector(
            ".dashboard-book-page:first-child"
        );

        if (!currentPage || !updatedPage) {
            throw new Error("Course task section not found.");
        }

        currentPage.innerHTML = updatedPage.innerHTML;

        // Synchronize task dialogs after updating the course tasks.
        const dialogPrefixes = [
            "taskDialog-",
            "editTaskDialog-",
            "deleteTaskDialog-"
        ];

        dialogPrefixes.forEach(prefix => {
            const updatedDialogs = updatedDocument.querySelectorAll(
                `dialog[id^="${prefix}"]`
            );

            const updatedIds = new Set(
                [...updatedDialogs].map(dialog => dialog.id)
            );

            document.querySelectorAll(`dialog[id^="${prefix}"]`).forEach(dialog => {
                if (!updatedIds.has(dialog.id) && !dialog.open) {
                    dialog.remove();
                }
            });

            updatedDialogs.forEach(updatedDialog => {
                const existingDialog = document.getElementById(updatedDialog.id);

                if (existingDialog?.open) return;

                const newDialog = updatedDialog.cloneNode(true);

                if (existingDialog) {
                    existingDialog.replaceWith(newDialog);
                } else {
                    document.body.appendChild(newDialog);
                }
            });
        });

    } catch (error) {
        console.error(error);
        select.value = savedStatus;
        alert("Failed to save task status.");
    } finally {
        select.disabled = false;
    }
});


// Save checklist changes and synchronize progress across the page.
document.addEventListener("change", async event => {
    const checkbox = event.target;

    if (!checkbox.matches(".checklist-checkbox")) return;
    const itemId = checkbox.dataset.itemId;
    const isCompleted = checkbox.checked;

    const matchingCheckboxes = document.querySelectorAll(
        `.checklist-checkbox[data-item-id="${itemId}"]`
    );

    matchingCheckboxes.forEach(item => {
        item.disabled = true;
    });

    try {
        const token = document.querySelector(
            'input[name="__RequestVerificationToken"]'
        ).value;

        const data = new FormData();
        data.append("itemId", itemId);
        data.append("isCompleted", isCompleted);
        data.append("__RequestVerificationToken", token);

        const response = await fetch(
            `${window.location.pathname}?handler=UpdateChecklistItem`,
            {
                method: "POST",
                body: data
            }
        );

        if (!response.ok) {
            throw new Error("Failed to update checklist item.");
        }

        // Synchronize matching checkboxes.
        matchingCheckboxes.forEach(item => {
            item.checked = isCompleted;
        });

        // Keep incomplete items in their original order and completed items last.
        matchingCheckboxes.forEach(item => {
            const list = item.closest("ul");

            const sortedItems = [...list.children].sort((a, b) => {
                const aCompleted = a.querySelector(".checklist-checkbox").checked;
                const bCompleted = b.querySelector(".checklist-checkbox").checked;

                if (aCompleted !== bCompleted) {
                    return Number(aCompleted) - Number(bCompleted);
                }

                return Number(a.dataset.order) - Number(b.dataset.order);
            });

            sortedItems.forEach(row => list.appendChild(row));
        });

        // Update progress in both task cards and dialogs.
        document.querySelectorAll(".task-card-checklist, dialog[id^='taskDialog-']")
            .forEach(container => {
                const checkboxes = container.querySelectorAll(".checklist-checkbox");

                if (checkboxes.length === 0) return;

                const completed = [...checkboxes].filter(item => item.checked).length;
                const total = checkboxes.length;

                const count = container.querySelector(".checklist-count");
                const progress = container.querySelector(".checklist-progress");

                if (count) count.textContent = `${completed}/${total} done`;
                if (progress) progress.value = completed;
            });

    } catch (error) {
        console.error(error);

        matchingCheckboxes.forEach(item => {
            item.checked = !isCompleted;
        });

        alert("Failed to save checklist item.");
    } finally {
        matchingCheckboxes.forEach(item => {
            item.disabled = false;
        });
    }
});

// Add new checklist items without submitting the form.
document.querySelectorAll(".add-checklist-row").forEach(button => {
    button.addEventListener("click", () => {
        const container = button
            .closest(".edit-task-form")
            .querySelector(".edit-checklist-items");

        const empty = [...container.querySelectorAll('input[name="itemTitles"], input[name="newItemTitles"]')]
            .find(input => !input.value.trim());
        if (empty) {
            const form = button.closest("form");
            updateValidationSummary(form, { checklist: "Complete the checklist item before adding another." });
            empty.focus();
            return;
        }
        const row = document.createElement("div");
        row.className = "edit-checklist-row";

        row.innerHTML = `
            <input type="text" name="newItemTitles"
                   maxlength="100" aria-label="New checklist item" />
            <button type="button" class="remove-checklist-row">
                Remove
            </button>
        `;

        container.appendChild(row);
    });
});

// Remove checklist rows without submitting the form.
document.querySelectorAll(".edit-task-form").forEach(form => {
    form.addEventListener("click", event => {
        const removeButton = event.target.closest(".remove-checklist-row");

        if (removeButton) {
            const row = removeButton.closest(".edit-checklist-row");
            if (row) row.remove();
            else {
                removeButton.nextElementSibling?.remove();
                removeButton.remove();
            }
        }
    });
});

// Open the course dialog if server-side validation failed.
if (document.querySelector("main").dataset.courseNameError === "True") {
    editCourseDialog.showModal();
}

// Scroll to the task form if server-side validation failed.
if (document.querySelector("main").dataset.taskInputError === "True") {
    document.querySelector(".create-task-form")?.scrollIntoView({
        block: "start"
    });
}

// Validate the course editor without browser popups.
const courseForm = editCourseDialog.querySelector('form');
courseForm.addEventListener("submit", event => {
    const name = courseForm.elements.namedItem("CourseName");
    const color = courseForm.elements.namedItem("CourseColor");
    const nameError = courseForm.querySelector('[data-valmsg-for="CourseName"]');
    const colorError = courseForm.querySelector('[data-valmsg-for="CourseColor"]');
    nameError.textContent = !name.value.trim() ? "You must enter a course name."
        : name.value.trim().length > 100 ? "The course name cannot exceed 100 characters." : "";
    colorError.textContent = /^#[0-9a-fA-F]{6}$/.test(color.value) ? "" : "Invalid color code.";
    if (nameError.textContent || colorError.textContent) event.preventDefault();
});
courseForm.addEventListener("input", () => {
    courseForm.querySelectorAll('[data-valmsg-for]').forEach(x => x.textContent = "");
});