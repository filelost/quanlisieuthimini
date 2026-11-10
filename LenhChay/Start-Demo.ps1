param(
    [string]$Dotnet = 'dotnet',
    [string]$SqlInstance = '.\SQLEXPRESS',
    [ValidatePattern('^QuanLySieuThi_Demo_[A-Za-z0-9_]{1,60}$')]
    [string]$Database = 'QuanLySieuThi_Demo_Local'
)
$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    $exists = sqlcmd -S $SqlInstance -E -C -b -h -1 -W -Q "SET NOCOUNT ON; SELECT IIF(DB_ID(N'$Database') IS NULL, 0, 1);"
    if ($LASTEXITCODE) { throw 'Không kết nối được SQL Server.' }
    if (($exists | Out-String).Trim() -eq '0') {
        $schemaFile = [IO.Path]::GetTempFileName()
        try {
            (Get-Content CoSoDuLieu/01_CreateDatabase.sql -Raw).Replace('QuanLySieuThiDb', $Database) |
                Set-Content -Encoding utf8 $schemaFile
            sqlcmd -S $SqlInstance -E -C -b -f 65001 -i $schemaFile
            if ($LASTEXITCODE) { throw 'Không tạo được schema demo.' }
        } finally { Remove-Item -LiteralPath $schemaFile }
    }
    sqlcmd -S $SqlInstance -E -C -b -d $Database -f 65001 -i CoSoDuLieu/05_AddProductImages.sql
    if ($LASTEXITCODE) { throw 'Không cập nhật được bảng ảnh sản phẩm.' }
    sqlcmd -S $SqlInstance -E -C -b -d $Database -f 65001 -i CoSoDuLieu/06_AddPurchaseShortageActions.sql
    if ($LASTEXITCODE) { throw 'Không cập nhật được bảng xử lý hàng nhập thiếu.' }
    sqlcmd -S $SqlInstance -E -C -b -d $Database -f 65001 -i CoSoDuLieu/07_AddWarehouseAudit.sql
    if ($LASTEXITCODE) { throw 'Không cập nhật được thông tin đối soát kho.' }
    sqlcmd -S $SqlInstance -E -C -b -d $Database -f 65001 -i CoSoDuLieu/08_AddAuditLogs.sql
    if ($LASTEXITCODE) { throw 'Không cập nhật được bảng nhật ký thao tác.' }
    sqlcmd -S $SqlInstance -E -C -b -d $Database -f 65001 -i CoSoDuLieu/09_AddCashPayment.sql
    if ($LASTEXITCODE) { throw 'Không cập nhật được thông tin tiền mặt trên hóa đơn.' }
    sqlcmd -S $SqlInstance -E -C -b -d $Database -f 65001 -i CoSoDuLieu/03_DemoData.sql
    if ($LASTEXITCODE) { throw 'Không nạp được dữ liệu mẫu.' }

    if (!$env:BootstrapAdmin__Username) { $env:BootstrapAdmin__Username = 'admin' }
    if (!$env:BootstrapAdmin__Password) {
        $env:BootstrapAdmin__Password = Read-Host 'Mật khẩu Admin demo cho lần tạo DB đầu tiên' -MaskInput
    }
    $env:ConnectionStrings__DefaultConnection = "Server=$SqlInstance;Database=$Database;Integrated Security=true;TrustServerCertificate=true"
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    & $Dotnet run --project QuanLySieuThi.csproj -c Release --no-launch-profile --urls http://localhost:5233
    if ($LASTEXITCODE) { throw 'Ứng dụng không khởi động được.' }
} finally { Pop-Location }
