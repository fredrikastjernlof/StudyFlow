
"use strict";

// Add and remove checklist fields using the shared form validation.
const newChecklistContainer = document.querySelector(".new-checklist-items");

if (newChecklistContainer) {
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

    document.querySelector(".add-new-checklist-item")?.addEventListener("click", () => {
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
        row.innerHTML = `<input type="text" name="checklistItems" placeholder="New checklist item" maxlength="100" />
            <button type="button" class="remove-new-checklist-item">Remove</button>`;

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

    if (!title) errors.title = "Please enter a task name.";
    else if (title.length > 100) errors.title = "Task name must not exceed 100 characters.";

    if (description.length > 1000) errors.description = "Description must not exceed 1000 characters.";

    if (!["1", "2", "3"].includes(priority)) errors.priority = "Please select a valid priority.";

    // An empty effort field is treated as zero.
    if (hours !== "" && (!Number.isFinite(Number(hours)) || Number(hours) < 0 || Number(hours) > 1000))
        errors.estimatedHours = "Estimated hours must be between 0 and 1000.";

    if (deadline) {
        const parsed = new Date(`${deadline}T00:00:00`);
        const today = new Date();
        today.setHours(0, 0, 0, 0);

        if (!Number.isFinite(parsed.getTime())) errors.deadline = "Please enter a valid date.";
        else if (parsed < today) errors.deadline = "Deadline cannot be in the past.";
    }

    if (edit) {
        if ([...form.querySelectorAll('input[name="itemTitles"], input[name="newItemTitles"]')]
            .some(input => !input.value.trim() || input.value.trim().length > 100)) {
            errors.checklist = "Checklist items must contain 1–100 characters.";
        }
    } else {
        const checklistInputs = [...form.querySelectorAll('input[name="checklistItems"]')];

        if (checklistInputs.some(input => input.value.trim().length > 100)) {
            errors.checklistItems = "Checklist items must not exceed 100 characters.";
        } else if (requireNamedChecklist && checklistInputs.some(input => !input.value.trim())) {
            errors.checklistItems = "Complete the checklist item before adding another.";
        }
    }

    // Mark invalid fields and remove the error state when corrected.
    const fieldErrors = ["title", "description", "priority", "estimatedHours", "deadline"];

    for (const name of fieldErrors) {
        const input = field(name);
        if (!input) continue;

        const invalid = Boolean(errors[name]);
        input.classList.toggle("is-invalid", invalid);

        if (invalid) {
            input.setAttribute("aria-invalid", "true");
        } else {
            input.removeAttribute("aria-invalid");
        }
    }

    const checklistNames = edit
        ? ['input[name="itemTitles"], input[name="newItemTitles"]']
        : ['input[name="checklistItems"]'];

    form.querySelectorAll(checklistNames.join(",")).forEach(input => {
        const invalid = edit
            ? (!input.value.trim() || input.value.trim().length > 100)
            : Boolean(errors.checklistItems) &&
              (!input.value.trim() || input.value.trim().length > 100);

        input.classList.toggle("is-invalid", invalid);

        if (invalid) {
            input.setAttribute("aria-invalid", "true");
        } else {
            input.removeAttribute("aria-invalid");
        }
    });

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
        heading.textContent = "Please check the following:";

        const list = document.createElement("ul");

        for (const message of messages) {
            const item = document.createElement("li");
            item.textContent = message;
            list.appendChild(item);
        }

        summary.append(heading, list);
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
