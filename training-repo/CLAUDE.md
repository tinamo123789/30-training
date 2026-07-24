# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 專案簡介

OrderHub 是一套公司內部訂單管理系統（訓練用專案）：業務人員可建立/查詢訂單、管理商品與客戶。單一 SQL Server 資料庫、內部使用，不需要考慮多租戶或高併發架構。

## 技術棧

- .NET 8 / ASP.NET Core MVC（Razor Views + Bootstrap 5，前端資源皆為本地檔案，不依賴 CDN）
- EF Core 8.0.11 + SQL Server（本機安裝，不使用 Docker）
- 測試：xUnit 2.5.3 + EF Core InMemory（不需要 SQL Server）

## 常用指令

```powershell
dotnet build                                    # 建置整個 solution
dotnet run --project src/OrderHub.Web           # 啟動網站（第一次啟動會自動 migrate + 植入種子資料）
dotnet test                                     # 跑全部測試（EF Core InMemory，不會動到實際資料庫）
dotnet test --filter "FullyQualifiedName~OrderServiceCancelTests"   # 只跑單一測試類別
dotnet test --filter "DisplayName~CancelOrder_NotFound_Fails"       # 只跑單一測試方法
```

重置資料庫（回到初始種子資料）：

```powershell
dotnet ef database drop -f -p src/OrderHub.Infrastructure -s src/OrderHub.Web
dotnet run --project src/OrderHub.Web
```

## 架構

三層式，依賴方向 `OrderHub.Web` → `OrderHub.Core` → `OrderHub.Infrastructure`（Core 定義 interface，Infrastructure 實作）：

- **`OrderHub.Web`**：Controllers、ViewModels、Views。只做接線與顯示，不含商業邏輯。
- **`OrderHub.Core`**：Domain models（`Customer`/`Product`/`Order`/`OrderItem`）、service 介面與實作（`OrderService`/`ProductService`/`CustomerService`）、repository 介面。所有商業邏輯（折扣、庫存增減、狀態轉移、驗證）都在這裡。
- **`OrderHub.Infrastructure`**：`OrderHubDbContext`、repository 實作、EF Core migrations、`DbSeeder`（開發種子資料，固定 random seed 確保每次重建內容一致）。

慣例（新增功能時請遵循，可參考 `ProductsController`/`ProductService` 的寫法）：

- Controller 保持薄，只轉接 service 結果；商業邏輯一律放 Core 的 service
- 只有 repository 碰 `DbContext`；Controller / Service 不可直接使用 EF Core
- Service 回傳 `ServiceResult<T>`（`Common/ServiceResult.cs`）表達預期內的失敗（累積多筆錯誤訊息），不要用例外表達業務規則失敗
- 分頁查詢回傳 `PagedResult<T>`（`Common/PagedResult.cs`）
- View 一律綁 ViewModel（`ViewModels/`，手寫 mapping），不要把 domain model 直接丟給 View
- 使用者輸入用 DataAnnotations + ModelState 驗證；輸入錯誤絕不能變成 500
- 金額一律用 `decimal`；折扣邏輯集中在 `OrderService`（`GetDiscountRate`/`CalculateSubtotal`/`CalculateTotal`），不要在別處重算
- 操作結果訊息用 `TempData["Success"] / TempData["Error"]`（`Views/Shared/_Layout.cshtml` 有共用 alert 區塊）

### 領域規則

- `CustomerTier`：Standard（不打折）/ Silver（95 折）/ Gold（9 折），折扣率定義在 `OrderService.GetDiscountRate`
- `OrderStatus` 生命週期：`Pending` → `Confirmed` → `Shipped`，或 `Pending`/`Confirmed` → `Cancelled`；只有 `Pending`/`Confirmed` 的訂單可取消
- 建立訂單時會鎖定單價快照（`OrderItem.UnitPriceSnapshot`）並扣減 `Product.StockQuantity`；取消訂單需要把庫存加回去

## 測試慣例

- 測試都在 `tests/OrderHub.Tests`，用 `TestSetup` 這個共用工具（`CreateContext`/`CreateOrderService`/`AddCustomer`/`AddProduct`）搭建 InMemory DbContext 與 service，不需要 mock repository
- 依 service 方法分類命名測試檔（如 `OrderServiceCancelTests`、`OrderServicePricingTests`），新測試比照既有檔案歸類

## 重要 / 危險檔案

- `src/OrderHub.Infrastructure/Migrations/**`：EF migration 是歷史紀錄，不要手改
- `src/OrderHub.Web/appsettings*.json`：連線字串等設定，改動前先問

## 不要做的事

- 不要未經同意就加新的 NuGet 套件
- 不要在 Controller / Service 直接使用 `DbContext`
- 不要為了「順手」重構與當前任務無關的程式碼
- 不要讀取或寫入任何機密檔（`*.pfx`、`appsettings.Production.json`、user-secrets）
