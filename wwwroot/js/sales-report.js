const reportForm = document.querySelector("#sales-report");
const reportStatus = document.querySelector("#report-status");
const reportDays = document.querySelector("#report-days");
const reportCurrency = new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" });

reportForm.addEventListener("submit", async event => {
    event.preventDefault();
    const button = reportForm.querySelector("button");
    button.disabled = true;
    reportStatus.textContent = "Đang tải…";
    reportDays.replaceChildren();
    try {
        const parameters = new URLSearchParams();
        for (const [key, value] of new FormData(reportForm)) {
            if (value) parameters.set(key, value);
        }
        const response = await fetch("/api/reports/sales?" + parameters);
        if (!response.ok) {
            const error = await response.json().catch(() => ({}));
            throw new Error(error.message || "Không tải được báo cáo. Kiểm tra ngày hoặc đăng nhập lại.");
        }
        const report = await response.json();
        reportForm.elements.from.value = report.from;
        reportForm.elements.to.value = report.to;
        const revenue = report.days.reduce((total, day) => total + day.revenue, 0);
        const count = report.days.reduce((total, day) => total + day.invoiceCount, 0);
        reportStatus.textContent = `Doanh thu: ${reportCurrency.format(revenue)} · Hóa đơn: ${count} · Mặt hàng dưới ngưỡng: ${report.lowStockCount}`;
        for (const day of report.days) {
            const row = reportDays.insertRow();
            row.insertCell().textContent = day.date.slice(0, 10);
            row.insertCell().textContent = day.invoiceCount;
            row.insertCell().textContent = reportCurrency.format(day.revenue);
        }
    } catch (error) {
        reportStatus.textContent = error.message;
    } finally {
        button.disabled = false;
    }
});

reportForm.requestSubmit();
