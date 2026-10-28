
"use strict";

document.addEventListener("DOMContentLoaded", () => {
    // Create task dialog.
    const dialog = document.getElementById("create-task-dialog");
    const openButton = document.getElementById("open-create-task-dialog");
    const closeButton = document.getElementById("close-create-task-dialog");

    openButton?.addEventListener("click", () => dialog?.showModal());
    closeButton?.addEventListener("click", () => dialog?.close());

    if (dialog?.dataset.openOnLoad === "true") {
        dialog.showModal();
    }

    // Route existing task forms to the Tasks page handlers.
    document.querySelectorAll(
        ".task-status-select, .edit-task-form, dialog[id^='deleteTaskDialog-'] form"
    ).forEach(element => {
        const form = element.matches("form") ? element : element.closest("form");
        if (!form) return;

        const url = new URL(form.action, window.location.href);
        url.pathname = window.location.pathname;
        url.searchParams.delete("id");
        form.action = url.toString();
    });

    // Open task details.
    document.querySelectorAll(".task-details-button").forEach(button => {
        button.addEventListener("click", () => {
            document.getElementById(
                `taskDialog-${button.dataset.taskId}`
            )?.showModal();
        });
    });

    // Close task details.
    document.querySelectorAll(".close-task-dialog").forEach(button => {
        button.addEventListener("click", () => {
            button.closest("dialog")?.close();
        });
    });

    // Open task editor.
    document.querySelectorAll(".open-edit-task-dialog").forEach(button => {
        button.addEventListener("click", () => {
            const taskId = button.dataset.taskId;
            document.getElementById(`taskDialog-${taskId}`)?.close();
            document.getElementById(`editTaskDialog-${taskId}`)?.showModal();
        });
    });

    document.querySelectorAll(".close-edit-task-dialog").forEach(button => {
        button.addEventListener("click", () => {
            button.closest("dialog")?.close();
        });
    });

    // Open delete confirmation.
    document.querySelectorAll(".open-delete-task-dialog").forEach(button => {
        button.addEventListener("click", () => {
            const taskId = button.dataset.taskId;
            document.getElementById(`taskDialog-${taskId}`)?.close();
            document.getElementById(`deleteTaskDialog-${taskId}`)?.showModal();
        });
    });

    document.querySelectorAll(".cancel-delete-task").forEach(button => {
        button.addEventListener("click", () => {
            button.closest("dialog")?.close();
        });
    });

    // Add checklist fields when editing.
    document.querySelectorAll(".add-checklist-row").forEach(button => {
        button.addEventListener("click", () => {
            const form = button.closest(".edit-task-form");
            const container = form?.querySelector(".edit-checklist-items");
            if (!container) return;

            const empty = [...container.querySelectorAll(
                'input[name="itemTitles"], input[name="newItemTitles"]'
            )].find(input => !input.value.trim());

            if (empty) {
                updateValidationSummary(form, {
                    checklist: "Fill in the checklist item before adding another."
                });
                empty.focus();
                return;
            }

            const row = document.createElement("div");
            row.className = "edit-checklist-row";
            row.innerHTML = `
                <input type="text" name="newItemTitles"
                maxlength="100" aria-label="New checklist item" />
                <button type="button" class="remove-checklist-row">Remove</button>`;

            container.appendChild(row);
        });
    });

    document.querySelectorAll(".edit-task-form").forEach(form => {
        form.addEventListener("click", event => {
            const button = event.target.closest(".remove-checklist-row");
            if (!button) return;

            const row = button.closest(".edit-checklist-row");
            if (row) {
                row.remove();
            } else {
                button.nextElementSibling?.remove();
                button.remove();
            }
        });
    });

    // Save status without a page reload.
    document.querySelectorAll(".task-status-select").forEach(select => {
        let savedStatus = select.value;

        select.addEventListener("change", async () => {
            const form = select.closest("form");
            if (!form) return;

            const formData = new FormData(form);
            select.disabled = true;

            try {
                const response = await fetch(form.action, {
                    method: "POST",
                    body: formData
                });

                if (!response.ok) throw new Error("Failed to save status.");

                savedStatus = select.value;

                const taskId = form.querySelector('input[name="taskId"]').value;
                sessionStorage.setItem("reopenTaskDialog", taskId);

                window.location.reload();

            } catch (error) {
                console.error(error);
                select.value = savedStatus;
                alert("Failed to save task status.");
            } finally {
                select.disabled = false;
            }
        });
    });

    // Save checklist changes and synchronize progress.
    document.querySelectorAll(".checklist-checkbox").forEach(checkbox => {
        checkbox.addEventListener("change", async () => {
            const itemId = checkbox.dataset.itemId;
            const isCompleted = checkbox.checked;

            const matchingCheckboxes = document.querySelectorAll(
                `.checklist-checkbox[data-item-id="${itemId}"]`
            );

            matchingCheckboxes.forEach(item => item.disabled = true);

            try {
                const token = document.querySelector(
                    'input[name="__RequestVerificationToken"]'
                )?.value;

                if (!token) throw new Error("Missing verification token.");

                const data = new FormData();
                data.append("itemId", itemId);
                data.append("isCompleted", isCompleted);
                data.append("__RequestVerificationToken", token);

                const response = await fetch(
                    `${window.location.pathname}?handler=UpdateChecklistItem`,
                    { method: "POST", body: data }
                );

                if (!response.ok) throw new Error("Failed to save checklist.");

                matchingCheckboxes.forEach(item => {
                    item.checked = isCompleted;

                    const list = item.closest("ul");
                    if (!list) return;

                    const sortedItems = [...list.children].sort((a, b) => {
                        const aCompleted = a.querySelector(".checklist-checkbox")?.checked ?? false;
                        const bCompleted = b.querySelector(".checklist-checkbox")?.checked ?? false;

                        if (aCompleted !== bCompleted) {
                            return Number(aCompleted) - Number(bCompleted);
                        }

                        return Number(a.dataset.order) - Number(b.dataset.order);
                    });

                    sortedItems.forEach(row => list.appendChild(row));
                });

                document.querySelectorAll(
                    ".task-card-checklist, dialog[id^='taskDialog-']"
                ).forEach(container => {
                    const boxes = [...container.querySelectorAll(".checklist-checkbox")];
                    if (!boxes.length) return;

                    const completed = boxes.filter(item => item.checked).length;
                    const count = container.querySelector(".checklist-count");
                    const progress = container.querySelector(".checklist-progress");

                    if (count) count.textContent = `${completed}/${boxes.length} done`;
                    if (progress) progress.value = completed;
                });
            } catch (error) {
                console.error(error);
                matchingCheckboxes.forEach(item => item.checked = !isCompleted);
                alert("Failed to save checklist item.");
            } finally {
                matchingCheckboxes.forEach(item => item.disabled = false);
            }
        });
    });

    // Reopen the task details dialog after a status update.
    const reopenTaskId = sessionStorage.getItem("reopenTaskDialog");

    if (reopenTaskId) {
        sessionStorage.removeItem("reopenTaskDialog");

        const taskDialog = document.getElementById(`taskDialog-${reopenTaskId}`);
        taskDialog?.showModal();
    }
});
