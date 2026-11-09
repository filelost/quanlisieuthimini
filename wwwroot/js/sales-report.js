const reportForm = document.querySelector("#sales-report");
const reportStatus = document.querySelector("#report-status");
const reportPeriods = document.querySelector("#report-periods");
const reportDetails = document.querySelector("#report-details");
const reportDetailsTitle = document.querySelector("#report-details-title");
const reportDetailsStatus = document.querySelector("#report-details-status");
const reportInvoices = document.querySelector("#report-invoices");
const reportDetailsPage = document.querySelector("#report-details-page");
const reportCurrency = new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" });
const reportDate = new Intl.DateTimeFormat("vi-VN", {
    timeZone: "UTC",
    day: "2-digit",
    month: "2-digit",
    year: "numeric"
});

function parseDate(value) {
    const [year, month, day] = value.split("-").map(Number);
    return new Date(Date.UTC(year, month - 1, day));
}

function addDays(date, count) {
    const result = new Date(date);
    result.setUTCDate(result.getUTCDate() + count);
    return result;
}

function dateValue(date) {
    return date.toISOString().slice(0, 10);
}

function parseUtcTimestamp(value) {
    const timestamp = /(?:Z|[+-]\d{2}:\d{2})$/i.test(value) ? value : `${value}Z`;
    return new Date(timestamp);
}

function getPeriods(from, to, groupBy, dailyTotals) {
    const lastDate = parseDate(to);
    const periods = [];

    for (let start = parseDate(from); start <= lastDate;) {
        let end = start;
        if (groupBy === "week") {
            end = addDays(start, (7 - start.getUTCDay()) % 7);
        } else if (groupBy === "month") {
            end = new Date(Date.UTC(start.getUTCFullYear(), start.getUTCMonth() + 1, 0));
        }
        if (end > lastDate) end = lastDate;

        let invoiceCount = 0;
        let revenue = 0;
        for (let day = start; day <= end; day = addDays(day, 1)) {
            const totals = dailyTotals.get(dateValue(day));
            if (totals) {
                invoiceCount += totals.invoiceCount;
                revenue += totals.revenue;
            }
        }

        periods.push({ start, end, invoiceCount, revenue });
        start = addDays(end, 1);
    }

    return periods;
}

function periodLabel(period, groupBy) {
    if (groupBy === "day") return `Ngày ${reportDate.format(period.start)}`;
    if (groupBy === "week") {
        return `Tuần ${reportDate.format(period.start)} – ${reportDate.format(period.end)}`;
    }
    return `Tháng ${String(period.start.getUTCMonth() + 1).padStart(2, "0")}/${period.start.getUTCFullYear()}`;
}

function addCell(row, value) {
    const cell = row.insertCell();
    cell.textContent = value;
    return cell;
}

async function loadInvoiceDetails(from, to, page) {
    reportDetails.hidden = false;
    reportDetailsStatus.textContent = "Đang tải chi tiết…";
    reportInvoices.replaceChildren();
    const parameters = new URLSearchParams({ from, to, page: String(page) });
    const response = await fetch("/api/reports/sales/details?" + parameters);
    if (!response.ok) {
        const error = await response.json().catch(() => ({}));
        throw new Error(error.message || "Không tải được chi tiết báo cáo.");
    }

    const result = await response.json();
    for (const invoice of result.invoices) {
        const row = reportInvoices.insertRow();
        const invoiceCell = row.insertCell();
        const link = document.createElement("a");
        link.href = `/Sales/Details/${invoice.salesInvoiceId}`;
        link.textContent = invoice.invoiceNo;
        invoiceCell.append(link);
        addCell(row, new Intl.DateTimeFormat("vi-VN", {
            timeZone: "UTC",
            dateStyle: "short",
            timeStyle: "short"
        }).format(parseUtcTimestamp(invoice.createdAt)));
        addCell(row, invoice.customerName);
        addCell(row, invoice.cashierName);
        addCell(row, invoice.paymentMethod === "Cash" ? "Tiền mặt" : invoice.paymentMethod);
        addCell(row, invoice.status === "Completed" ? "Hoàn tất" : invoice.status);
        addCell(row, reportCurrency.format(invoice.totalAmount));

        const detailsCell = row.insertCell();
        const disclosure = document.createElement("details");
        const summary = document.createElement("summary");
        summary.textContent = `${invoice.details.length} mặt hàng`;
        disclosure.append(summary);
        const list = document.createElement("ul");
        list.className = "mb-0 ps-3";
        for (const line of invoice.details) {
            const item = document.createElement("li");
            item.textContent = `${line.skuSnapshot} · ${line.productNameSnapshot}: ${line.quantity} × ${reportCurrency.format(line.unitPrice)} = ${reportCurrency.format(line.lineTotal)}`;
            list.append(item);
        }
        disclosure.append(list);
        detailsCell.append(disclosure);
    }

    const pageCount = Math.ceil(result.totalCount / result.pageSize);
    reportDetailsPage.textContent = pageCount
        ? `Trang ${result.page}/${pageCount} · ${result.totalCount} hóa đơn`
        : "Không có hóa đơn trong kỳ.";
    reportDetailsStatus.textContent = pageCount ? "" : "Không có hóa đơn hoàn tất trong kỳ này.";
    document.querySelector("#report-details-previous").disabled = result.page <= 1;
    document.querySelector("#report-details-next").disabled = result.page >= pageCount;
    reportDetails.dataset.from = from;
    reportDetails.dataset.to = to;
    reportDetails.dataset.page = String(result.page);
}

reportPeriods.addEventListener("click", async event => {
    const button = event.target.closest("button[data-from][data-to]");
    if (!button) return;

    reportDetailsTitle.textContent = `Chi tiết báo cáo · ${button.dataset.label}`;
    try {
        await loadInvoiceDetails(button.dataset.from, button.dataset.to, 1);
        reportDetails.scrollIntoView({ behavior: "smooth", block: "start" });
    } catch (error) {
        reportDetails.hidden = false;
        reportDetailsStatus.textContent = error.message;
    }
});

document.querySelector("#report-details-close").addEventListener("click", () => {
    reportDetails.hidden = true;
});

document.querySelector("#report-details-previous").addEventListener("click", async () => {
    try {
        await loadInvoiceDetails(reportDetails.dataset.from, reportDetails.dataset.to, Number(reportDetails.dataset.page) - 1);
    } catch (error) {
        reportDetailsStatus.textContent = error.message;
    }
});

document.querySelector("#report-details-next").addEventListener("click", async () => {
    try {
        await loadInvoiceDetails(reportDetails.dataset.from, reportDetails.dataset.to, Number(reportDetails.dataset.page) + 1);
    } catch (error) {
        reportDetailsStatus.textContent = error.message;
    }
});

reportForm.addEventListener("submit", async event => {
    event.preventDefault();
    const button = reportForm.querySelector("button");
    button.disabled = true;
    reportStatus.textContent = "Đang tải…";
    reportPeriods.replaceChildren();
    reportDetails.hidden = true;
    try {
        const from = reportForm.elements.from.value;
        const to = reportForm.elements.to.value;
        const groupBy = reportForm.elements.groupBy.value;
        const parameters = new URLSearchParams({ from, to });
        const response = await fetch("/api/reports/sales?" + parameters);
        if (!response.ok) {
            const error = await response.json().catch(() => ({}));
            throw new Error(error.message || "Không tải được báo cáo. Kiểm tra ngày hoặc đăng nhập lại.");
        }
        const report = await response.json();
        reportForm.elements.from.value = report.from;
        reportForm.elements.to.value = report.to;
        const dailyTotals = new Map(report.days.map(day => [day.date.slice(0, 10), day]));
        const periods = getPeriods(report.from, report.to, groupBy, dailyTotals);
        const revenue = periods.reduce((total, period) => total + period.revenue, 0);
        const count = periods.reduce((total, period) => total + period.invoiceCount, 0);
        reportStatus.textContent = `Doanh thu: ${reportCurrency.format(revenue)} · Hóa đơn: ${count} · Mặt hàng dưới ngưỡng: ${report.lowStockCount}`;
        for (const period of periods) {
            const row = reportPeriods.insertRow();
            row.insertCell().textContent = periodLabel(period, groupBy);
            row.insertCell().textContent = `${reportDate.format(period.start)} – ${reportDate.format(period.end)}`;
            row.insertCell().textContent = period.invoiceCount;
            row.insertCell().textContent = reportCurrency.format(period.revenue);
            const detailsButton = document.createElement("button");
            detailsButton.type = "button";
            detailsButton.className = "btn btn-sm btn-outline-primary";
            detailsButton.textContent = "Xem chi tiết";
            detailsButton.dataset.from = dateValue(period.start);
            detailsButton.dataset.to = dateValue(period.end);
            detailsButton.dataset.label = periodLabel(period, groupBy);
            row.insertCell().append(detailsButton);
        }
    } catch (error) {
        reportStatus.textContent = error.message;
    } finally {
        button.disabled = false;
    }
});

const utcToday = new Date().toISOString().slice(0, 10);
const currentMonth = utcToday.slice(0, 7);
const [year, month] = currentMonth.split("-").map(Number);
reportForm.elements.from.value = `${currentMonth}-01`;
reportForm.elements.to.value = dateValue(new Date(Date.UTC(year, month, 0)));
reportForm.requestSubmit();
