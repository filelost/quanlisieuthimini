param([string]$BaseUrl = 'http://localhost:5233')
$ErrorActionPreference = 'Stop'
if (!$env:BootstrapAdmin__Username -or !$env:BootstrapAdmin__Password) {
    throw 'Set BootstrapAdmin__Username and BootstrapAdmin__Password for the QA database.'
}
function Check($condition, $name) {
    if (!$condition) { throw "FAIL $name" }
    Write-Host "PASS $name"
}
function Request($path, $session, $body) {
    $options = @{ Uri = "$BaseUrl$path"; SkipHttpErrorCheck = $true; MaximumRedirection = 0; ErrorAction = "SilentlyContinue" }
    if ($session) { $options.WebSession = $session }
    if ($null -ne $body) { $options.Method = 'Post'; $options.Body = $body }
    Invoke-WebRequest @options
}
function Token($response) {
    $match = [regex]::Match($response.Content, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (!$match.Success) { throw "CSRF token missing (HTTP $($response.StatusCode))" }
    $match.Groups[1].Value
}
function Login($username, $password) {
    $session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
    $form = Request '/Auth/Login' $session
    $result = Request '/Auth/Login' $session @{
        Username = $username; Password = $password; __RequestVerificationToken = (Token $form)
    }
    Check ($result.StatusCode -eq 302) "Login $username"
    $session
}
function Json($path, $session) {
    $response = Request $path $session
    if ($response.StatusCode -ne 200) { throw "$path returned $($response.StatusCode)" }
    $response.Content | ConvertFrom-Json
}
Check ((Request '/api/products').StatusCode -eq 401) 'TC01 anonymous API'
$admin = Login $env:BootstrapAdmin__Username $env:BootstrapAdmin__Password
$products = @(Json '/api/products?search=DEMO-001' $admin)
Check ($products.Count -eq 1 -and $products[0].salePrice -eq 7000) 'TC02 seeded catalog'
$id = $products[0].productId
function Quantity {
    (Json '/api/inventory' $admin | Where-Object productId -eq $id).quantityOnHand
}
$before = Quantity
$sale = Request '/Sales' $admin
Check ($sale.StatusCode -eq 200 -and $sale.Content.Contains('DEMO-001')) 'TC03 POS renders SQL product'
$body = @{
    __RequestVerificationToken = (Token $sale); RequestId = [guid]::NewGuid().ToString()
    PaymentMethod = 'Cash'; 'Lines[0].ProductId' = $id; 'Lines[0].Quantity' = '2'
}
$first = Request '/Sales/Checkout' $admin $body
Check ($first.StatusCode -eq 302 -and "$($first.Headers.Location)" -match '/Sales/Details/') 'TC04 checkout creates invoice'
$repeat = Request '/Sales/Checkout' $admin $body
Check ("$($repeat.Headers.Location)" -eq "$($first.Headers.Location)" -and (Quantity) -eq ($before - 2)) 'TC05 replay deducts once'
$body.RequestId = [guid]::NewGuid().ToString()
$body['Lines[0].Quantity'] = '1000000'
$invalid = Request '/Sales/Checkout' $admin $body
Check ("$($invalid.Headers.Location)" -notmatch '/Sales/Details/' -and (Quantity) -eq ($before - 2)) 'TC06 insufficient stock'
$utc = [DateTime]::UtcNow.ToString('yyyy-MM-dd')
$report = Json "/api/reports/sales?from=$utc&to=$utc" $admin
Check ($report.days[0].revenue -ge 14000 -and (Request '/Screens/Reports' $admin).StatusCode -eq 200) 'TC07 sales report'
Check ((Request '/api/reports/sales?from=2026-12-31&to=2026-01-01' $admin).StatusCode -eq 400) 'TC08 reversed report date'
$employee = Request '/Admin/EmployeeCreate' $admin
$user = 'qa-' + [guid]::NewGuid().ToString('N').Substring(0, 10)
$password = [guid]::NewGuid().ToString('N')
$created = Request '/Admin/EmployeeCreate' $admin @{
    __RequestVerificationToken = (Token $employee); Username = $user; Password = $password
    EmployeeCode = $user; FullName = 'QA Cashier'; RoleName = 'Cashier'
}
Check ($created.StatusCode -eq 302) 'TC09 create Cashier'
$cashier = Login $user $password
Check ((Request '/api/inventory' $cashier).StatusCode -eq 403 -and (Request '/api/products' $cashier).StatusCode -eq 200) 'TC10 Cashier permission'
Check ((Request '/Sales/Checkout' $admin @{ RequestId = [guid]::NewGuid() }).StatusCode -eq 400) 'TC11 missing CSRF'

$purchase = Request '/Purchases' $admin
$supplier = [regex]::Match($purchase.Content, '<option value="(\d+)">DEMO-NCC').Groups[1].Value
if (!$supplier) { $supplier = [regex]::Match($purchase.Content, '<option value="(\d+)">').Groups[1].Value }
$receipt = Request '/Purchases/Create' $admin @{
    __RequestVerificationToken = (Token $purchase); supplierId = $supplier
}
$location = "$($receipt.Headers.Location)"
Check ($location -match '/Purchases/Details/(\d+)') 'TC12 create purchase'
$receiptId = $Matches[1]
$details = Request $location $admin
$csrf = Token $details
$null = Request '/Purchases/AddLine' $admin @{
    __RequestVerificationToken = $csrf; id = $receiptId; productId = $id; quantity = 5; unitCost = 4000
}
$details = Request $location $admin
$lineId = [regex]::Match($details.Content, 'name="lineId" value="(\d+)"').Groups[1].Value
Check ([bool]$lineId) 'TC13 add purchase line'
$receive = @{
    __RequestVerificationToken = (Token $details); id = $receiptId; lineId = $lineId
    quantity = 2; expectedReceived = 0; reason = 'QA partial delivery'
}
$null = Request '/Purchases/Receive' $admin $receive
Check ((Quantity) -eq $before) 'TC14 partial delivery increases stock by two'
$receive.expectedReceived = 2
$receive.quantity = 4
$null = Request '/Purchases/Receive' $admin $receive
Check ((Quantity) -eq $before) 'TC15 overdelivery keeps stock'
$receive.quantity = 3
$null = Request '/Purchases/Receive' $admin $receive
Check ((Quantity) -eq ($before + 3)) 'TC16 final delivery increases stock by three'
$token = Json '/api/sales/checkout-token' $cashier
$jsonBody = @{
    requestId = [guid]::NewGuid().ToString(); paymentMethod = 'Cash'
    lines = @(@{ productId = $id; quantity = 1 })
} | ConvertTo-Json -Depth 4
$options = @{
    Uri = "$BaseUrl/api/sales/checkout"; Method = 'Post'; Body = $jsonBody
    ContentType = 'application/json'; WebSession = $cashier; SkipHttpErrorCheck = $true
}
Check ((Invoke-WebRequest @options).StatusCode -eq 400) 'TC21 JSON checkout rejects missing CSRF'
$options.Headers = @{ $token.headerName = $token.requestToken }
$jsonSale = Invoke-WebRequest @options
$jsonRepeat = Invoke-WebRequest @options
Check ($jsonSale.StatusCode -eq 200 -and $jsonRepeat.StatusCode -eq 200 -and
    ($jsonSale.Content | ConvertFrom-Json).totalAmount -eq 7000 -and
    $jsonSale.Content -eq $jsonRepeat.Content -and (Quantity) -eq ($before + 2)) 'TC22 JSON checkout and replay'
$change = Request '/Auth/ChangePassword' $cashier
Check ($change.StatusCode -eq 200) 'TC23 password form'
$newPassword = [guid]::NewGuid().ToString('N')
$changeBody = @{
    __RequestVerificationToken = (Token $change)
    CurrentPassword = 'incorrect-password'
    NewPassword = $newPassword
    ConfirmPassword = $newPassword
}
Check ((Request '/Auth/ChangePassword' $cashier $changeBody).StatusCode -eq 200) 'TC24 wrong current password rejected'
$changeBody.CurrentPassword = $password
$changeBody.ConfirmPassword = 'different-password'
Check ((Request '/Auth/ChangePassword' $cashier $changeBody).StatusCode -eq 200) 'TC25 confirmation mismatch rejected'
$changeBody.ConfirmPassword = $newPassword
$changed = Request '/Auth/ChangePassword' $cashier $changeBody
Check ($changed.StatusCode -eq 302 -and "$($changed.Headers.Location)" -match '/Auth/Login' -and
    (Request '/api/products' $cashier).StatusCode -eq 401) 'TC26 password change signs out'
$loginSession = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
$loginPage = Request '/Auth/Login' $loginSession
$oldLogin = Request '/Auth/Login' $loginSession @{
    Username = $user; Password = $password; __RequestVerificationToken = (Token $loginPage)
}
Check ($oldLogin.StatusCode -eq 200 -and (Request '/api/products' $loginSession).StatusCode -eq 401) 'TC27 old password rejected'
$null = Login $user $newPassword
