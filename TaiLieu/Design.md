# Thiết kế

## Use Case

```mermaid
flowchart LR
    A[Admin] --> L((Đăng nhập))
    C[Cashier] --> L
    K[StockStaff] --> L
    A --> M((Danh mục, nhân viên, báo cáo))
    A --> I((Nhập và tra tồn))
    K --> I
    A --> J((Điều chỉnh tồn))
    C --> S((Khách hàng và bán hàng))
    A --> S
    A --> X((Hủy hóa đơn))
```

## Activity bán hàng

```mermaid
flowchart TD
    A([Chọn sản phẩm]) --> B[Nhập số lượng và phương thức thanh toán]
    B --> C{Dữ liệu và tồn hợp lệ?}
    C -- Không --> B
    C -- Có --> D[Tính tiền theo giá DB]
    D --> E[Ghi hóa đơn, chi tiết và biến động kho]
    E --> F{Tiền mặt?}
    F -- Có --> G([Hoàn tất])
    F -- Không --> H([Chờ xác nhận MoMo])
```

## Sequence bán hàng

```mermaid
sequenceDiagram
    actor NV as Thu ngân
    participant UI as Razor và JavaScript
    participant S as SalesController
    participant DB as SQL Server
    NV->>UI: Xác nhận giỏ
    UI->>S: POST Checkout, cookie và CSRF token
    S->>DB: Mở giao dịch, tìm mã yêu cầu và đọc tồn
    alt Hợp lệ
        S->>DB: Ghi hóa đơn, chi tiết và tồn, commit
        S-->>UI: Chuyển tới chi tiết
    else Không đủ tồn
        S-->>UI: Quay về POS với lỗi
    end
```

## Class Diagram

```mermaid
classDiagram
    SalesController --> ApplicationDbContext
    SalesController --> MomoGateway
    SalesController --> StockMovement
    PurchasesController --> ApplicationDbContext
    PurchasesController --> StockMovement
    AuthController --> IAuthService
    IAuthService <|.. AuthService
    AuthService --> ApplicationDbContext
    StoreApiController --> ApplicationDbContext
    SalesInvoice "1" *-- "*" SalesDetail
    PurchaseReceipt "1" *-- "*" PurchaseDetail
    Product "1" -- "0..1" Inventory
```

## ERD

```mermaid
erDiagram
    Roles ||--o{ Users : assigns
    Employees o|--o| Users : links
    Categories ||--o{ Products : groups
    Products ||--o| Inventory : tracks
    Products ||--o{ StockTransactions : moves
    Users ||--o{ StockTransactions : records
    Suppliers ||--o{ PurchaseReceipts : supplies
    Users ||--o{ PurchaseReceipts : creates
    PurchaseReceipts ||--o{ PurchaseDetails : contains
    Products ||--o{ PurchaseDetails : purchased
    Customers o|--o{ SalesInvoices : buys
    Users ||--o{ SalesInvoices : sells
    SalesInvoices ||--o{ SalesDetails : contains
    Products ||--o{ SalesDetails : sold
```

Khóa, kiểu cột, chỉ mục và CHECK được định nghĩa trong CoSoDuLieu/01_CreateDatabase.sql. Khóa chính của Inventory đồng thời là khóa ngoại ProductId. StockTransactions lưu tham chiếu nghiệp vụ theo cặp loại và ID chứng từ.
