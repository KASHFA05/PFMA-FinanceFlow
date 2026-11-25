// Small helpers for the colorful pages: open/close modals and fill edit forms from data-* attributes.
document.addEventListener("DOMContentLoaded", function () {
    // data-open="modalId"  -> opens that modal
    document.querySelectorAll("[data-open]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            var modal = document.getElementById(btn.getAttribute("data-open"));
            if (!modal) return;

            // clear old values first (a missing data-fill-xxx attribute means "empty")
            modal.querySelectorAll("[data-field]").forEach(function (f) {
                if (!btn.hasAttribute("data-fill-" + f.getAttribute("data-field"))) f.value = "";
            });

            // data-fill-xxx="value" copies the value into the field with name="xxx" inside the modal
            Array.prototype.forEach.call(btn.attributes, function (attr) {
                if (attr.name.indexOf("data-fill-") === 0) {
                    var fieldName = attr.name.substring("data-fill-".length);
                    var field = modal.querySelector('[data-field="' + fieldName + '"]');
                    if (field) field.value = attr.value;
                }
            });

            // data-text-xxx="value" writes text into elements with data-text="xxx"
            Array.prototype.forEach.call(btn.attributes, function (attr) {
                if (attr.name.indexOf("data-text-") === 0) {
                    var key = attr.name.substring("data-text-".length);
                    var el = modal.querySelector('[data-text="' + key + '"]');
                    if (el) el.textContent = attr.value;
                }
            });

            modal.classList.add("active");
        });
    });

    // data-close -> closes the modal it is inside
    document.querySelectorAll("[data-close]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            var modal = btn.closest(".modal-overlay");
            if (modal) modal.classList.remove("active");
        });
    });

    // click on the dark background closes the modal
    document.querySelectorAll(".modal-overlay").forEach(function (overlay) {
        overlay.addEventListener("click", function (e) {
            if (e.target === overlay) overlay.classList.remove("active");
        });
    });
});
