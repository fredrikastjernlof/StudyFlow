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
document.querySelectorAll(".task-details-button").forEach(button => {
    button.addEventListener("click", () => {
        const taskId = button.dataset.taskId;
        const dialog = document.getElementById(`taskDialog-${taskId}`);

        dialog.showModal();
    });
});

// Close the task details dialog.
document.querySelectorAll(".close-task-dialog").forEach(button => {
    button.addEventListener("click", () => {
        button.closest("dialog").close();
    });
});

// Automatically save the task status when changed, without reloading the page.
document.querySelectorAll(".task-status-select").forEach(select => {
    let savedStatus = select.value;

    select.addEventListener("change", async () => {
        const form = select.closest("form");
        const formData = new FormData(form);

        select.disabled = true;

        try {
            const response = await fetch(form.action, {
                method: "POST",
                body: formData
            });

            if (!response.ok) {
                throw new Error("Failed to save status.");
            }

            savedStatus = select.value;
        } catch (error) {
            console.error(error);
            select.value = savedStatus;
            alert("Kunde inte spara statusen.");
        } finally {
            select.disabled = false;
        }
    });
});

// Save checklist changes and synchronize progress across the page.
document.querySelectorAll(".checklist-checkbox").forEach(checkbox => {
    checkbox.addEventListener("change", async () => {
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

            alert("Kunde inte spara delmomentet.");
        } finally {
            matchingCheckboxes.forEach(item => {
                item.disabled = false;
            });
        }
    });
});

// Open the task editor from the details dialog.
document.querySelectorAll(".open-edit-task-dialog").forEach(button => {
    button.addEventListener("click", () => {
        const taskId = button.dataset.taskId;

        document.getElementById(`taskDialog-${taskId}`).close();
        document.getElementById(`editTaskDialog-${taskId}`).showModal();
    });
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
            updateValidationSummary(form, { checklist: "Fyll i delmomentet innan du lägger till ett nytt." });
            empty.focus();
            return;
        }
        const row = document.createElement("div");
        row.className = "edit-checklist-row";

        row.innerHTML = `
            <input type="text" name="newItemTitles"
                   maxlength="100" aria-label="Nytt delmoment" />
            <button type="button" class="remove-checklist-row">
                Ta bort
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

// Add and remove checklist fields using the shared form validation.
const newChecklistContainer = document.querySelector(".new-checklist-items");
const createTaskForm = newChecklistContainer.closest("form");
let checklistAddAttempted = false;

const clearNewChecklistError = () => {
    const inputs = [...newChecklistContainer.querySelectorAll('input[name="checklistItems"]')];

    if (inputs.every(input => input.value.trim())) {
        checklistAddAttempted = false;
    }

    if (!createTaskForm.querySelector(".validation-summary").hidden) {
        validateTaskForm(createTaskForm, false, checklistAddAttempted);
    }
};

document.querySelector(".add-new-checklist-item").addEventListener("click", () => {
    const inputs = [...newChecklistContainer.querySelectorAll('input[name="checklistItems"]')];
    const empty = inputs.find(input => !input.value.trim());

    if (empty) {
        checklistAddAttempted = true;
        validateTaskForm(createTaskForm, false, true);
        empty.focus();
        return;
    }

    checklistAddAttempted = false;

    const row = document.createElement("div");
    row.className = "new-checklist-row";
    row.innerHTML = `<input type="text" name="checklistItems" placeholder="Nytt delmoment" maxlength="100" />
        <button type="button" class="remove-new-checklist-item">Ta bort</button>`;

    newChecklistContainer.appendChild(row);
    row.querySelector("input").focus();
});

newChecklistContainer.addEventListener("input", clearNewChecklistError);

newChecklistContainer.addEventListener("click", event => {
    const button = event.target.closest(".remove-new-checklist-item");
    if (!button) return;

    button.closest(".new-checklist-row").remove();
    clearNewChecklistError();
});

// Close the task editor.
document.querySelectorAll(".close-edit-task-dialog").forEach(button => {
    button.addEventListener("click", () => {
        button.closest("dialog").close();
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

// Validate task forms before posting. Native POST remains unchanged.
function validateTaskForm(form, edit, requireNamedChecklist = false) {
    const field = name => form.elements.namedItem(edit ? name : `TaskInput.${name.charAt(0).toUpperCase()}${name.slice(1)}`);
    const errors = {};
    const title = field("title")?.value.trim() ?? "";
    const description = field("description")?.value ?? "";
    const priority = field("priority")?.value ?? "";
    const hours = field("estimatedHours")?.value ?? "";
    const deadline = field("deadline")?.value ?? "";
    if (!title) errors.title = "Du måste ange ett namn på uppgiften.";
    else if (title.length > 100) errors.title = "Uppgiftens namn får innehålla högst 100 tecken.";
    if (description.length > 1000) errors.description = "Beskrivningen får innehålla högst 1000 tecken.";
    if (!["1", "2", "3"].includes(priority)) errors.priority = "Välj en giltig prioritet.";
    // An empty effort field is treated as zero (the model uses a non-nullable number).
    if (hours !== "" && (!Number.isFinite(Number(hours)) || Number(hours) < 0 || Number(hours) > 1000))
        errors.estimatedHours = "Arbetsinsatsen måste vara mellan 0 och 1000 timmar.";
    if (deadline) {
        const parsed = new Date(`${deadline}T00:00:00`);
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        if (!Number.isFinite(parsed.getTime())) errors.deadline = "Ange ett giltigt datum.";
        else if (parsed < today) errors.deadline = "Deadline får inte ligga bakåt i tiden.";
    }
    if (edit) {
        if ([...form.querySelectorAll('input[name="itemTitles"], input[name="newItemTitles"]')]
            .some(input => !input.value.trim() || input.value.trim().length > 100)) {
            errors.checklist = "Delmoment måste innehålla 1–100 tecken.";
        }
    } else {
        const checklistInputs = [...form.querySelectorAll('input[name="checklistItems"]')];

        if (checklistInputs.some(input => input.value.trim().length > 100)) {
            errors.checklistItems = "Delmoment får innehålla högst 100 tecken.";
        } else if (requireNamedChecklist && checklistInputs.some(input => !input.value.trim())) {
            errors.checklistItems = "Fyll i delmomentet innan du lägger till ett nytt.";
        }
    }
    updateValidationSummary(form, errors);
    return Object.keys(errors).length === 0;
}

function updateValidationSummary(form, errors) {
    const summary = form.querySelector(".validation-summary");
    if (!summary) return;
    const messages = [...new Set(Object.values(errors).filter(Boolean))];
    summary.hidden = messages.length === 0;
    summary.replaceChildren();
    if (messages.length) {
        const heading = document.createElement("strong");
        heading.textContent = "Kontrollera följande:";
        const list = document.createElement("ul");
        for (const message of messages) {
            const item = document.createElement("li");
            item.textContent = message;
            list.appendChild(item);
        }
        summary.append(heading, list);
        summary.style.color = "#b42318";
    }
}

document.querySelectorAll(".create-task-form, .edit-task-form").forEach(form => {
    const edit = form.classList.contains("edit-task-form");
    let attemptedSave = false;
    form.addEventListener("submit", event => {
        attemptedSave = true;
        if (!validateTaskForm(form, edit)) {
            event.preventDefault();
            form.querySelector(".validation-summary")?.scrollIntoView({ block: "nearest" });
        }
    });
    form.addEventListener("input", () => {
        if (attemptedSave) validateTaskForm(form, edit);
    });
    form.addEventListener("change", () => {
        if (attemptedSave) validateTaskForm(form, edit);
    });
});

// Custom confirmation dialog, matching the course deletion dialog.
document.querySelectorAll(".open-delete-task-dialog").forEach(button => {
    button.addEventListener("click", () => {
        const taskId = button.dataset.taskId;
        document.getElementById(`taskDialog-${taskId}`)?.close();
        document.getElementById(`deleteTaskDialog-${taskId}`)?.showModal();
    });
});
document.querySelectorAll(".cancel-delete-task").forEach(button => {
    button.addEventListener("click", () => button.closest("dialog")?.close());
});

// Validate the course editor without browser popups.
const courseForm = editCourseDialog.querySelector('form');
courseForm.addEventListener("submit", event => {
    const name = courseForm.elements.namedItem("CourseName");
    const color = courseForm.elements.namedItem("CourseColor");
    const nameError = courseForm.querySelector('[data-valmsg-for="CourseName"]');
    const colorError = courseForm.querySelector('[data-valmsg-for="CourseColor"]');
    nameError.textContent = !name.value.trim() ? "Du måste ange ett kursnamn."
        : name.value.trim().length > 100 ? "Kursnamnet får innehålla högst 100 tecken." : "";
    colorError.textContent = /^#[0-9a-fA-F]{6}$/.test(color.value) ? "" : "Ogiltig färgkod.";
    if (nameError.textContent || colorError.textContent) event.preventDefault();
});
courseForm.addEventListener("input", () => {
    courseForm.querySelectorAll('[data-valmsg-for]').forEach(x => x.textContent = "");
});