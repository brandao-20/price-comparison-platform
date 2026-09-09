<#
.SYNOPSIS
Populate an empty local demo database through the running API.
.DESCRIPTION
Run only against a disposable, migrated database with an explicitly bootstrapped
administrator. All stores, products, and prices are fictional. No data is deleted.
#>
[CmdletBinding()]
param(
    [uri]$ApiBaseUrl = 'http://localhost:5000/',
    [string]$AdminUsername = 'DemoAdmin',
    [Parameter(Mandatory)][securestring]$AdminPassword
)

$ErrorActionPreference = 'Stop'
if (-not $ApiBaseUrl.IsLoopback -or $ApiBaseUrl.Scheme -notin @('http', 'https')) {
    throw 'Demo seeding is limited to a local API backed by a disposable database.'
}
$baseUrl = $ApiBaseUrl.AbsoluteUri.TrimEnd('/')
$login = Invoke-RestMethod -Method Post -Uri "$baseUrl/api/Auth/login" -ContentType 'application/json' -Body (@{
    Username = $AdminUsername
    Password = [Net.NetworkCredential]::new('', $AdminPassword).Password
} | ConvertTo-Json)
if (-not $login.data.token -or $login.data.role -ne 'Admin') { throw 'An administrator login is required.' }
$headers = @{ Authorization = 'Bearer ' + $login.data.token }

function Invoke-DemoApi([string]$Method, [string]$Path, $Body = $null) {
    $request = @{ Method = $Method; Uri = "$baseUrl/api/$Path"; Headers = $headers }
    if ($null -ne $Body) {
        $request.ContentType = 'application/json'
        $request.Body = $Body | ConvertTo-Json -Depth 8
    }
    Invoke-RestMethod @request
}

$existingProducts = Invoke-DemoApi Get 'Produtos'
$existingStores = Invoke-DemoApi Get 'Lojas'
$existingCategories = Invoke-DemoApi Get 'Categorias'
if (@($existingProducts.data).Count -gt 0 -or @($existingStores).Count -gt 0 -or @($existingCategories.data).Count -gt 0) {
    throw 'The catalogue is not empty. Use a fresh disposable database; existing data was preserved.'
}

$dairy = (Invoke-DemoApi Post 'Categorias' @{ Nome = 'Dairy' }).data.categoriaId
$pantry = (Invoke-DemoApi Post 'Categorias' @{ Nome = 'Pantry' }).data.categoriaId
$stores = @(
    Invoke-DemoApi Post 'Lojas' @{ Nome = 'Demo Market Central'; Supermercado = 'Demo Market'; Endereco = 'Fictional location A' }
    Invoke-DemoApi Post 'Lojas' @{ Nome = 'Demo Market Riverside'; Supermercado = 'Demo Market'; Endereco = 'Fictional location B' }
)
$action = (Invoke-DemoApi Post 'TipoAcaos' @{ Tipo = 'DEMO_PRICE' }).data.tipoAcaoId
$products = @(
    @{ Nome = 'Whole Milk 1 L'; Marca = 'Demo Dairy'; CategoriaId = $dairy; Prices = @(1.19, 1.25) }
    @{ Nome = 'Greek Yogurt 500 g'; Marca = 'Demo Dairy'; CategoriaId = $dairy; Prices = @(2.49, 2.29) }
    @{ Nome = 'Rolled Oats 500 g'; Marca = 'Demo Pantry'; CategoriaId = $pantry; Prices = @(1.39, 1.59) }
    @{ Nome = 'Basmati Rice 1 kg'; Marca = 'Demo Pantry'; CategoriaId = $pantry; Prices = @(2.19, 2.39) }
    @{ Nome = 'Olive Oil 750 ml'; Marca = 'Demo Pantry'; CategoriaId = $pantry; Prices = @(6.49, 6.19) }
)
$offsets = @(@(0.20, 0.10, 0.15, 0.05, 0.10, 0.00, 0.00), @(0.10, 0.15, 0.05, 0.10, 0.00, 0.05, 0.00))
$lastDate = [DateTime]::UtcNow.Date.AddDays(-1).AddHours(12)
$recordCount = 0
foreach ($item in $products) {
    $product = (Invoke-DemoApi Post 'Produtos' @{
        Nome = $item.Nome; Marca = $item.Marca; CategoriaId = $item.CategoriaId
        Descricao = 'Fictional sample product for portfolio demonstration. Prices are not retail offers.'
    }).data
    for ($storeIndex = 0; $storeIndex -lt $stores.Count; $storeIndex++) {
        for ($day = 0; $day -lt 7; $day++) {
            $null = Invoke-DemoApi Post 'RegistosPrecos' @{
                ProdutoId = $product.produtoId; LojaId = $stores[$storeIndex].lojaId; TipoAcaoId = $action
                Preco = [Math]::Round($item.Prices[$storeIndex] + $offsets[$storeIndex][$day], 2)
                DataRegisto = $lastDate.AddDays($day - 6).ToString('o')
            }
            $recordCount++
        }
    }
}
Write-Output "Created 5 fictional products, 2 stores, 2 categories and $recordCount price records."
