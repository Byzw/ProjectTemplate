export interface MenuItem {
    itemLabel: string; // 選單項目名稱
    itemIcon?: string; // 選單對應的 icon（可選）
    itemUrlPath?: string; // 路由導航（可選）
    ModuleName?: string; // 權限檢查對應的 Module 名稱（可選）
    ModuleSubType?: string; // 權限檢查對應的 Module 子類型（可選）
    enable?: boolean; // 是否啟用該選單項目
}

export interface MenuFolder {
    folderLabel: string; // 資料夾名稱
    folderIcon?: string; // 資料夾對應的 icon（可選）
    ModuleName?: string; // 權限檢查對應的 Module 名稱（可選）
    ModuleSubType?: string; // 權限檢查對應的 Module 子類型（可選）
    folderItems: (MenuFolder | MenuItem)[]; // 子項目可包含 MenuFolder 或 MenuItem，因此支援無限層級(理論，請勿超過三層)下去
    enable?: boolean; // 是否啟用該選單資料夾
}
