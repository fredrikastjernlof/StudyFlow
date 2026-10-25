"use strict";

// Handle opening and closing the course creation dialog.
const courseDialog = document.getElementById("courseDialog");

document.getElementById("openCourseDialog").addEventListener("click", () => {
    courseDialog.showModal();
});

document.getElementById("closeCourseDialog").addEventListener("click", () => {
    courseDialog.close();
});

// Close the dialog when clicking outside its content.
courseDialog.addEventListener("click", (event) => {
    if (event.target === courseDialog) {
        courseDialog.close();
    }
});

// Reopen the dialog if server-side validation fails.
if (courseDialog.dataset.validationError === "True") {
    courseDialog.showModal();
}