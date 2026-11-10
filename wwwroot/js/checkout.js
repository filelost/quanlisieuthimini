(() => {
    const checkout = document.querySelector("#checkout");
    const search = document.querySelector("#catalog-search");
    const message = document.querySelector("#catalog-message");
    const rows = Array.from(checkout.querySelectorAll("[data-product]"));

    search.addEventListener("input", () => {
        const term = search.value.trim().toLowerCase();
        for (const row of rows) {
            const matches = Object.values(row.dataset).some(value => value.toLowerCase().includes(term));
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
