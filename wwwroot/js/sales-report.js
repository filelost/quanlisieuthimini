const reportForm = document.querySelector("#sales-report");
const reportFields = reportForm.querySelector("fieldset");
const reportStatus = document.querySelector("#report-status");
const reportDays = document.querySelector("#report-days");
const reportHeading = document.querySelector("#report-date-heading");
const lowStockSection = document.querySelector("#report-low-stock");
const lowStockBody = document.querySelector("#report-low-stock-items");
const excelLink = document.querySelector("#report-excel");
const invoicesButton = document.querySelector("#report-invoices-button");
const invoicesPanel = document.querySelector("#report-invoices-panel");
const invoicesBody = document.querySelector("#report-invoices");
const invoicesStatus = document.querySelector("#report-invoices-status");
const previousPage = document.querySelector("#report-invoices-prev");
const nextPage = document.querySelector("#report-invoices-next");
let loadedRange;
let invoicePage = 1;
let invoiceRequest;
const { period, date, from, to } = reportForm.elements;
const reportCurrency = new Intl.NumberFormat("vi-VN", {
    style: "currency", currency: "VND", maximumFractionDigits: 2
});
const stockQuantity = new Intl.NumberFormat("vi-VN", { maximumFractionDigits: 3 });
const invoiceTime = new Intl.DateTimeFormat("vi-VN", {
    timeZone: "Asia/Ho_Chi_Minh", dateStyle: "short", timeStyle: "medium"
});

const isoDate = value => value.toISOString().slice(0, 10);
const displayDate = value => value.slice(0, 10).split("-").reverse().join("/");

function reportRange(kind, selectedDate) {
    const start = new Date(selectedDate + "T00:00:00Z");
    const end = new Date(start);
    if (kind === "week") {
        start.setUTCDate(start.getUTCDate() - (start.getUTCDay() + 6) % 7);
        end.setTime(start.getTime());
        end.setUTCDate(start.getUTCDate() + 6);
    } else if (kind === "month") {
        start.setUTCDate(1);
        end.setUTCMonth(end.getUTCMonth() + 1, 0);
    } else if (kind === "year") {
        start.setUTCMonth(0, 1);
        end.setUTCMonth(11, 31);
    }
    return { from: isoDate(start), to: isoDate(end) };
}

function monthlyRows(days, year) {
    const months = Array.from({ length: 12 }, (_, index) => ({
        label: `${String(index + 1).padStart(2, "0")}/${year}`, invoiceCount: 0, revenue: 0
    }));
    for (const day of days) {
        const month = months[Number(day.date.slice(5, 7)) - 1];
        month.invoiceCount += day.invoiceCount;
        month.revenue += day.revenue;
    }
    return months;
}

function updateReportFields() {
    const custom = period.value === "custom";
    document.querySelector("#report-reference").hidden = custom;
    document.querySelector("#report-from-field").hidden = !custom;
    document.querySelector("#report-to-field").hidden = !custom;
    date.disabled = custom;
    from.disabled = to.disabled = !custom;
    from.required = to.required = custom;
}

function showLowStock(items) {
    if (items.length === 0) {
        const cell = lowStockBody.insertRow().insertCell();
        cell.colSpan = 5;
        cell.textContent = "Hiện không có mặt hàng nào tồn thấp.";
    }
    for (const item of items) {
        const row = lowStockBody.insertRow();
        row.insertCell().textContent = item.sku;
        row.insertCell().textContent = item.name;
        row.insertCell().textContent = item.unit;
        row.insertCell().textContent = item.quantityOnHand === null
            ? "Chưa ghi nhận tồn" : stockQuantity.format(item.quantityOnHand);
        row.insertCell().textContent = stockQuantity.format(item.reorderLevel);
    }
    lowStockSection.hidden = false;
}

function hideInvoices() {
    invoiceRequest?.abort();
    invoicesPanel.hidden = true;
}

async function showInvoices(page) {
    if (!loadedRange) return;
    invoiceRequest?.abort();
    const request = new AbortController();
    invoiceRequest = request;
    invoicesPanel.hidden = false;
    invoicesStatus.textContent = "Đang tải hóa đơn…";
    previousPage.disabled = nextPage.disabled = true;
    invoicesBody.replaceChildren();
    try {
        const parameters = new URLSearchParams({ ...loadedRange, page: String(page) });
        const response = await fetch("/api/reports/sales/details?" + parameters, { signal: request.signal });
        if (!response.ok) throw new Error("Không tải được hóa đơn trong kỳ.");
        const result = await response.json();
        if (request.signal.aborted) return;

        for (const invoice of result.invoices) {
            const row = invoicesBody.insertRow();
            const link = document.createElement("a");
            link.href = "/Sales/Details/" + encodeURIComponent(invoice.salesInvoiceId);
            link.textContent = invoice.invoiceNo;
            row.insertCell().append(link);
            const timestamp = /(?:Z|[+-]\d{2}:\d{2})$/i.test(invoice.createdAt)
                ? invoice.createdAt : invoice.createdAt + "Z";
            row.insertCell().textContent = invoiceTime.format(new Date(timestamp));
            row.insertCell().textContent = invoice.customerName;
            row.insertCell().textContent = invoice.cashierName;
            row.insertCell().textContent = invoice.paymentMethod === "Cash" ? "Tiền mặt" : invoice.paymentMethod;
            row.insertCell().textContent = reportCurrency.format(invoice.totalAmount);

            const disclosure = document.createElement("details");
            const summary = document.createElement("summary");
            summary.textContent = invoice.details.length + " mặt hàng";
            const list = document.createElement("ul");
            list.className = "mb-0 ps-3 text-wrap";
            for (const line of invoice.details) {
                const item = document.createElement("li");
                item.textContent = `${line.skuSnapshot} · ${line.productNameSnapshot} · ${stockQuantity.format(line.quantity)} · ${reportCurrency.format(line.lineTotal)}`;
                list.append(item);
            }
            disclosure.append(summary, list);
            row.insertCell().append(disclosure);
        }

        invoicePage = result.page;
        const pageCount = Math.ceil(result.totalCount / result.pageSize);
        invoicesStatus.textContent = pageCount === 0 ? "Không có hóa đơn trong kỳ."
            : `Trang ${invoicePage}/${pageCount} · ${result.totalCount} hóa đơn`;
        previousPage.disabled = invoicePage <= 1;
        nextPage.disabled = invoicePage >= pageCount;
    } catch (error) {
        if (!request.signal.aborted) invoicesStatus.textContent = error.message;
    }
}

invoicesButton.addEventListener("click", () => showInvoices(1));
previousPage.addEventListener("click", () => showInvoices(invoicePage - 1));
nextPage.addEventListener("click", () => showInvoices(invoicePage + 1));
document.querySelector("#report-invoices-close").addEventListener("click", hideInvoices);

period.addEventListener("change", updateReportFields);
reportForm.addEventListener("submit", async event => {
    event.preventDefault();
    const kind = period.value;
    reportFields.disabled = true;
    reportStatus.textContent = "Đang tải…";
    reportDays.replaceChildren();
    lowStockSection.hidden = true;
    lowStockBody.replaceChildren();
    loadedRange = undefined;
    invoicesButton.disabled = true;
    excelLink.hidden = true;
    hideInvoices();
    reportHeading.textContent = kind === "year" ? "Tháng (UTC)" : "Ngày (UTC)";

    try {
        const range = kind === "custom" ? { from: from.value, to: to.value } : reportRange(kind, date.value);
        const response = await fetch("/api/reports/sales?" + new URLSearchParams(range));
        if (!response.ok) {
            const error = await response.json().catch(() => ({}));
            throw new Error(error.message || "Không tải được báo cáo. Kiểm tra ngày hoặc đăng nhập lại.");
        }

        const report = await response.json();
        loadedRange = { from: report.from, to: report.to };
        invoicesButton.disabled = false;
        excelLink.href = excelLink.dataset.url + "?" + new URLSearchParams(loadedRange);
        excelLink.hidden = false;
        const revenue = report.days.reduce((total, day) => total + day.revenue, 0);
        const count = report.days.reduce((total, day) => total + day.invoiceCount, 0);
        reportStatus.textContent = `${displayDate(report.from)} – ${displayDate(report.to)} · Doanh thu ${reportCurrency.format(revenue)} · ${count} hóa đơn · ${report.lowStockCount} mặt hàng tồn thấp hiện tại`;
        showLowStock(report.lowStockItems);

        if (report.days.length === 0) {
            const cell = reportDays.insertRow().insertCell();
            cell.colSpan = 3;
            cell.textContent = "Không có hóa đơn đã hoàn tất trong kỳ này.";
            return;
        }

        const rows = kind === "year"
            ? monthlyRows(report.days, report.from.slice(0, 4))
            : report.days.map(day => ({ ...day, label: displayDate(day.date) }));
        for (const item of rows) {
            const row = reportDays.insertRow();
            row.insertCell().textContent = item.label;
            row.insertCell().textContent = item.invoiceCount;
            row.insertCell().textContent = reportCurrency.format(item.revenue);
        }
    } catch (error) {
        reportStatus.textContent = error.message;
    } finally {
        reportFields.disabled = false;
    }
});

date.value = isoDate(new Date());
from.value = reportRange("month", date.value).from;
to.value = date.value;
updateReportFields();
reportForm.requestSubmit();
