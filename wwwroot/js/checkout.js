(() => {
    const checkout = document.querySelector("#checkout");
    const search = document.querySelector("#catalog-search");
    const message = document.querySelector("#catalog-message");
    const rows = Array.from(checkout.querySelectorAll("[data-product]"));
    const payment = checkout.querySelector("#payment");
    const promotion = checkout.querySelector("#promotion");
    const cashReceived = checkout.querySelector("#cash-received");
    const cashChange = checkout.querySelector("#cash-change");
    const total = checkout.querySelector("#checkout-total");
    const money = new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 2 });

    function updateTotals() {
        const cash = payment.value === "Cash";
        const factor = cash ? 100 : 1;
        const subtotal = rows.reduce((sum, row) =>
            sum + Math.round(Number(row.dataset.price) * Number(row.querySelector("input").value) * factor), 0);
        const selected = promotion.selectedOptions[0];
        const amount = Number(selected.dataset.value) || 0;
        const discount = selected.dataset.type === "Percent" ? subtotal * amount / 100 : amount * factor;
        const due = (subtotal - Math.min(subtotal, Math.round(discount))) / factor;
        total.value = money.format(due);
        cashReceived.disabled = !cash;
        checkout.querySelector("#cash-received-field").hidden = !cash;
        checkout.querySelector("#cash-change-field").hidden = !cash;
        const received = Number(cashReceived.value);
        cashChange.value = cashReceived.value === "" ? "—"
            : received < due ? "Còn thiếu " + money.format(due - received) : money.format(received - due);
    }

    checkout.addEventListener("input", updateTotals);
    checkout.addEventListener("change", updateTotals);
    updateTotals();

    search.addEventListener("input", () => {
        const term = search.value.trim().toLowerCase();
        for (const row of rows) {
            const matches = [row.dataset.barcode, row.dataset.sku, row.dataset.name]
                .some(value => value.toLowerCase().includes(term));
            row.hidden = !matches && !Number(row.querySelector("input").value);
        }
        message.textContent = "";
    });

    search.addEventListener("keydown", event => {
        if (event.key !== "Enter") return;
        event.preventDefault();
        const term = search.value.trim().toLowerCase();
        if (!term) return;

        const row = rows.find(item => item.dataset.barcode?.toLowerCase() === term)
            ?? rows.find(item => item.dataset.sku.toLowerCase() === term);
        if (!row) {
            message.textContent = "Mã hàng không tồn tại hoặc hết tồn khả dụng.";
            return;
        }

        const quantity = row.querySelector("input");
        const next = Number(quantity.value) + 1;
        if (next > Number(quantity.max)) {
            message.textContent = "Không đủ tồn khả dụng.";
            return;
        }
        quantity.value = next.toFixed(3);
        updateTotals();
        search.value = "";
        search.dispatchEvent(new Event("input"));
        message.textContent = `Đã thêm ${row.dataset.name}.`;
    });

    checkout.addEventListener("submit", event => {
        let index = 0;
        for (const row of rows) {
            const quantity = row.querySelector("input");
            quantity.removeAttribute("name");
            row.querySelector("input[type=hidden]")?.remove();
            if (!Number(quantity.value)) continue;

            const productId = document.createElement("input");
            productId.type = "hidden";
            productId.name = `Lines[${index}].ProductId`;
            productId.value = row.dataset.product;
            quantity.name = `Lines[${index}].Quantity`;
            quantity.after(productId);
            index++;
        }
        if (!index) {
            event.preventDefault();
            message.textContent = "Chọn ít nhất một sản phẩm.";
            search.focus();
        }
    });
})();
