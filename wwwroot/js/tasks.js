
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
    document.addEventListener("click", event => {
        const button = event.target.closest(".task-details-button");
        if (!button) return;

        document.getElementById(
            `taskDialog-${button.dataset.taskId}`
        )?.showModal();
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

        if (button.matches(".close-task-dialog, .close-edit-task-dialog, .cancel-delete-task")) {
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
        if (!form) return;

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

            const sections = [
                ".todays-focus-section",
                ".in-progress-section",
                ".coming-up-section",
                ".completed-section",
                ".dashboard-completed"
            ];

            sections.forEach(selector => {
                const currentSection = currentContent.querySelector(selector);
                const updatedSection = updatedContent.querySelector(selector);

                if (currentSection && updatedSection) {
                    currentSection.innerHTML = updatedSection.innerHTML;
                }
            });

            // Synchronize task dialogs after updating the task sections.
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

                    newDialog.querySelectorAll(
                        ".task-status-select, .edit-task-form, form"
                    ).forEach(element => {
                        const form = element.matches("form")
                            ? element
                            : element.closest("form");

                        if (!form) return;

                        const url = new URL(form.action, window.location.href);
                        url.pathname = window.location.pathname;
                        url.searchParams.delete("id");
                        form.action = url.toString();
                    });

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


    // Save checklist changes and synchronize progress.
    document.addEventListener("change", async event => {
        const checkbox = event.target;

        if (!checkbox.matches(".checklist-checkbox")) return;
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

    // Reopen the task details dialog after a status update.
    const reopenTaskId = sessionStorage.getItem("reopenTaskDialog");

    if (reopenTaskId) {
        sessionStorage.removeItem("reopenTaskDialog");

        const taskDialog = document.getElementById(`taskDialog-${reopenTaskId}`);
        taskDialog?.showModal();
    }
});
