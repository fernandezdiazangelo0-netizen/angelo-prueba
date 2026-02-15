$baseUrl = "http://localhost:5248" # Default standard port for dotnet new webapi

# 1. Register Guest (Standard register)
Write-Host "Registering Guest..."
$guestBody = @{
    Username = "guestUser"
    Email = "guest@test.com"
    Password = "GuestPassword123!"
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$baseUrl/api/Auth/register" -Method Post -Body $guestBody -ContentType "application/json"
    Write-Host "Guest Registered."
} catch {
    Write-Host "Guest Register Error: $($_.Exception.Message)"
}

# 2. Login Guest
Write-Host "Logging in Guest..."
$guestLoginBody = @{
    Username = "guestUser"
    Password = "GuestPassword123!"
} | ConvertTo-Json

$guestLoginResponse = Invoke-RestMethod -Uri "$baseUrl/api/Auth/login" -Method Post -Body $guestLoginBody -ContentType "application/json"
$guestToken = $guestLoginResponse.token
Write-Host "Guest Token received."

# 3. Create Product as Guest (Should Fail)
Write-Host "Attempting to create product as Guest (Expected: 403 Forbidden)..."
$productBody = @{
    Name = "Test Product"
    Description = "A test product"
    Price = 99.99
    Category = "Toys"
} | ConvertTo-Json

$guestHeaders = @{
    Authorization = "Bearer $guestToken"
}

try {
    Invoke-RestMethod -Uri "$baseUrl/api/Products" -Method Post -Headers $guestHeaders -Body $productBody -ContentType "application/json"
    Write-Host "ERROR: Guest was able to create product!"
} catch {
    Write-Host "Success: Guest was denied access ($($_.Exception.Response.StatusCode))"
}

# 4. Register Admin (Special endpoint)
Write-Host "Registering Admin..."
$adminBody = @{
    Username = "adminUser"
    Email = "admin@test.com"
    Password = "AdminPassword123!"
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$baseUrl/api/Auth/register-admin" -Method Post -Body $adminBody -ContentType "application/json"
    Write-Host "Admin Registered."
} catch {
    Write-Host "Admin Register Error: $($_.Exception.Message)"
}

# 5. Login Admin
Write-Host "Logging in Admin..."
$loginBody = @{
    Username = "adminUser"
    Password = "AdminPassword123!"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/Auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token
Write-Host "Admin Token received."

# 6. Create Product (As Admin)
Write-Host "Creating Product as Admin..."
$headers = @{
    Authorization = "Bearer $token"
}

try {
    $createProductResponse = Invoke-RestMethod -Uri "$baseUrl/api/Products" -Method Post -Headers $headers -Body $productBody -ContentType "application/json"
    Write-Host "Product Created: $($createProductResponse.name)"
} catch {
    Write-Host "Failed to create product: $($_.Exception.Message)"
}

# 7. Get Products (Public)
Write-Host "Getting Products..."
$products = Invoke-RestMethod -Uri "$baseUrl/api/Products" -Method Get
Write-Host "Products found: $($products.Count)"

# 8. Check Frontend Availability
$frontendUrl = "http://localhost:5148" # Port from launchSettings.json
Write-Host "Checking Frontend at $frontendUrl..."
try {
    $frontendResponse = Invoke-WebRequest -Uri $frontendUrl -Method Head -TimeoutSec 5
    Write-Host "Frontend is reachable (Status: $($frontendResponse.StatusCode))"
} catch {
    Write-Host "Frontend check failed (Might not be running yet): $($_.Exception.Message)"
}
